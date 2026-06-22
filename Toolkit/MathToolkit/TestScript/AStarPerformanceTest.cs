using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PowerCellStudio
{
    public class AStarPerformanceTest : RunTestMono
    {
        [SerializeField]
        private int _mapWidth = 256;

        [SerializeField]
        private int _mapHeight = 256;

        [SerializeField]
        private int _requestCount = 64;

        [SerializeField]
        private int _repeatCount = 10;

        private readonly List<Vector2Int> _grounds = new List<Vector2Int>();
        private readonly List<PathRequest> _requests = new List<PathRequest>();
        private int _lastChecksum;

        public void Trigger()
        {
            Debug.Log("========== AStar Performance Test Started ==========");

            BuildTestMap();
            BuildPathRequests();
            WarmUp();
            ValidatePathFinding();
            RunBenchmarks();

            Debug.Log($"========== AStar Performance Test Finished. checksum: {_lastChecksum} ==========");
        }

        private void BuildTestMap()
        {
            _grounds.Clear();

            for (var y = 0; y < _mapHeight; y++)
            {
                for (var x = 0; x < _mapWidth; x++)
                {
                    if (IsBlockedByTestObstacle(x, y))
                    {
                        continue;
                    }

                    _grounds.Add(new Vector2Int(x, y));
                }
            }
        }

        private bool IsBlockedByTestObstacle(int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= _mapWidth - 1 || y >= _mapHeight - 1)
            {
                return false;
            }

            // 构造带缺口的竖向障碍，保证地图连通，同时让寻路有绕行成本。
            // Build vertical walls with gaps so the map stays connected while paths still need detours.
            return x % 11 == 0 && y % 9 != 4;
        }

        private void BuildPathRequests()
        {
            _requests.Clear();
            if (_grounds.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _requestCount; i++)
            {
                var startIndex = i * 17 % _grounds.Count;
                var endIndex = (_grounds.Count - 1 - i * 31) % _grounds.Count;
                if (endIndex < 0)
                {
                    endIndex += _grounds.Count;
                }

                var from = _grounds[startIndex];
                var to = _grounds[endIndex];
                if (from == to)
                {
                    to = _grounds[(endIndex + _grounds.Count / 2) % _grounds.Count];
                }

                _requests.Add(new PathRequest(from, to));
            }
        }

        private void WarmUp()
        {
            RunAStarOnce();
            RunFastAStarOnce();
            RunJPSOnce();
            RunFastJPSOnce();
        }

        private void ValidatePathFinding()
        {
            RunTest("AStar FastAStar JPS FastJPS path finding result", () =>
            {
                var astar = new AStar(_grounds);
                using var fastAStar = new FastAStar(_grounds, Vector2Int.one, false);
                var jps = new JPS();
                jps.initMap(_grounds);
                using var fastJPS = new FastJPS(_grounds);

                foreach (var request in _requests)
                {
                    var astarPath = astar.Path(request.From, request.To);
                    var fastAStarPath = fastAStar.Path(request.From, request.To);
                    var jpsPath = jps.GetPath(request.From, request.To);
                    var fastJPSPath = fastJPS.GetPath(request.From, request.To);

                    Assert(astarPath != null && astarPath.Count > 0, $"AStar path is null or empty. from:{request.From}, to:{request.To}");
                    Assert(fastAStarPath != null && fastAStarPath.Length > 0, $"FastAStar path is null or empty. from:{request.From}, to:{request.To}");
                    Assert(jpsPath != null && jpsPath.Count > 0, $"JPS path is null or empty. from:{request.From}, to:{request.To}");
                    Assert(fastJPSPath != null && fastJPSPath.Length > 0, $"FastJPS path is null or empty. from:{request.From}, to:{request.To}");
                }
            });
        }

        private void RunBenchmarks()
        {
            RunPerformanceTest($"AStar path x{_requestCount * _repeatCount} map {_mapWidth}x{_mapHeight}", () =>
            {
                for (var i = 0; i < _repeatCount; i++)
                {
                    RunAStarOnce();
                }
            });

            RunPerformanceTest($"FastAStar path x{_requestCount * _repeatCount} map {_mapWidth}x{_mapHeight}", () =>
            {
                for (var i = 0; i < _repeatCount; i++)
                {
                    RunFastAStarOnce();
                }
            });

            // RunPerformanceTest($"JPS path x{_requestCount * _repeatCount} map {_mapWidth}x{_mapHeight}", () =>
            // {
            //     for (var i = 0; i < _repeatCount; i++)
            //     {
            //         RunJPSOnce();
            //     }
            // });

            // RunPerformanceTest($"FastJPS path x{_requestCount * _repeatCount} map {_mapWidth}x{_mapHeight}", () =>
            // {
            //     for (var i = 0; i < _repeatCount; i++)
            //     {
            //         RunFastJPSOnce();
            //     }
            // });
        }

        // private async Task RunAsyncBenchmarks()
        // {
        //     var stopwatch = new Stopwatch();
        //     try
        //     {
        //         stopwatch.Start();
        //         for (var i = 0; i < _repeatCount; i++)
        //         {
        //             await RunFastAStarAsyncOnce();
        //         }
        //         stopwatch.Stop();
        //         Debug.Log($"[PASS] FastAStar async path x{_requestCount * _repeatCount} map {_mapWidth}x{_mapHeight}: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
        //     }
        //     catch (System.Exception e)
        //     {
        //         stopwatch.Stop();
        //         Debug.LogError($"[FAIL] FastAStar async path crashed after {stopwatch.Elapsed.TotalMilliseconds:F2} ms. \nException: {e.Message} \n{e.StackTrace}");
        //     }
        // }

        private void RunAStarOnce()
        {
            var astar = new AStar(_grounds);
            foreach (var request in _requests)
            {
                var path = astar.Path(request.From, request.To);
                _lastChecksum += path?.Count ?? 0;
            }
        }

        private void RunFastAStarOnce()
        {
            using var fastAStar = new FastAStar(_grounds, Vector2Int.one, false);
            foreach (var request in _requests)
            {
                var path = fastAStar.Path(request.From, request.To);
                _lastChecksum += path?.Length ?? 0;
            }
        }

        private void RunJPSOnce()
        {
            var jps = new JPS();
            jps.initMap(_grounds);
            foreach (var request in _requests)
            {
                var path = jps.GetPath(request.From, request.To);
                _lastChecksum += path?.Count ?? 0;
            }
        }

        private void RunFastJPSOnce()
        {
            using var fastJPS = new FastJPS(_grounds);
            foreach (var request in _requests)
            {
                var path = fastJPS.GetPath(request.From, request.To);
                _lastChecksum += path?.Length ?? 0;
            }
        }

        // private async Task RunFastAStarAsyncOnce()
        // {
        //     using var fastAStar = new FastAStar(_grounds, Vector2Int.one, false);
        //     var tasks = new Task<Vector2Int[]>[_requests.Count];
        //     for (var i = 0; i < _requests.Count; i++)
        //     {
        //         tasks[i] = RunFastAStarPathAsync(fastAStar, _requests[i]);
        //     }
        //
        //     var results = await Task.WhenAll(tasks);
        //     foreach (var path in results)
        //     {
        //         _lastChecksum += path?.Length ?? 0;
        //     }
        // }

        // private static Task<Vector2Int[]> RunFastAStarPathAsync(FastAStar fastAStar, PathRequest request)
        // {
        //     var taskCompletionSource = new TaskCompletionSource<Vector2Int[]>();
        //     fastAStar.PathAsync(request.From, request.To, result => taskCompletionSource.SetResult(result));
        //     return taskCompletionSource.Task;
        // }

        private readonly struct PathRequest
        {
            public readonly Vector2Int From;
            public readonly Vector2Int To;

            public PathRequest(Vector2Int from, Vector2Int to)
            {
                From = from;
                To = to;
            }
        }
    }
}
