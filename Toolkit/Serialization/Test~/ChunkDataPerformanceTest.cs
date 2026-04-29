using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class ChunkDataPerformanceTest : RunTestMono
    {
        private const string PerfFileName = "ChunkDataPerformance";
        private const int RecordCount = 4096;
        private const int ChunkSize = 128;
        private const int RandomLookupIterations = 2048;
        private const int BatchFilterIterations = 128;
        private const int FullScanIterations = 64;

        private List<ChunkPerfRecord> _records;
        private string _directory;
        private string _dataFilePath;
        private string _indexFilePath;

        private void OnEnable()
        {
            Debug.Log($"========== DataChunk Performance Test Started (Records: {RecordCount}, ChunkSize: {ChunkSize}) ==========");

            PrepareDataset();
            WarmUp();
            ValidateQueryerRoundTrip();
            RunWriteBenchmark();
            RunPrepareBenchmark();
            RunRandomLookupBenchmark();
            RunBatchFilterBenchmark();
            RunFullScanBenchmark();
            Cleanup();

            Debug.Log("========== DataChunk Performance Test Finished ==========");
        }

        private void PrepareDataset()
        {
            _directory = Path.Combine(Application.temporaryCachePath, "DataChunkPerf", Guid.NewGuid().ToString("N"));
            _dataFilePath = Path.Combine(_directory, PerfFileName + "Data.bytes");
            _indexFilePath = Path.Combine(_directory, PerfFileName + "Index.bytes");
            _records = Enumerable.Range(0, RecordCount)
                .Select(index => new ChunkPerfRecord
                {
                    Id = index,
                    Name = "perf_" + index,
                    Group = index % 32,
                    Score = index * 1.5f
                })
                .ToList();

            ChunkMaker.StreamWriteSync<int, ChunkPerfRecord>(_directory, PerfFileName, _records, item => item.Id, ChunkSize);
        }

        private void WarmUp()
        {
            ChunkReader.ReadIndexFile<int>(_indexFilePath).FirstOrDefault();
            ChunkReader.ReadChunkData<ChunkPerfRecord>(_dataFilePath, 0).FirstOrDefault();
            CreateQueryer().Get(0, null);
        }

        private void ValidateQueryerRoundTrip()
        {
            RunTest("ChunkDataQueryer validation", () =>
            {
                var queryer = CreateQueryer();
                ChunkPerfRecord hit = queryer.Get(255, null);
                List<int> filteredIds = queryer.GetByKey(id => id >= 512 && id < 520, null).Select(item => item.Id).ToList();
                int fullScanCount = queryer.GetAll().Count();

                Assert(hit != null && hit.Id == 255, "Get should return the requested performance record.");
                Assert(filteredIds.Count == 8 && filteredIds[0] == 512 && filteredIds[7] == 519, "GetByKey validation mismatch.");
                Assert(fullScanCount == RecordCount, "GetAll validation count mismatch.");
            });
        }

        private void RunWriteBenchmark()
        {
            RunPerformanceTest("ChunkMaker StreamWriteSync x16", () =>
            {
                for (int i = 0; i < 16; i++)
                {
                    string directory = Path.Combine(Application.temporaryCachePath, "DataChunkPerfWrite", Guid.NewGuid().ToString("N"));
                    ChunkMaker.StreamWriteSync<int, ChunkPerfRecord>(directory, PerfFileName, _records, item => item.Id, ChunkSize);
                    if (System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.Delete(directory, true);
                    }
                }
            });
        }

        private void RunPrepareBenchmark()
        {
            RunPerformanceTest("ChunkDataQueryer Prepare x256", () =>
            {
                for (int i = 0; i < 256; i++)
                {
                    CreateQueryer();
                }
            });
        }

        private void RunRandomLookupBenchmark()
        {
            RunPerformanceTest($"ChunkDataQueryer Get x{RandomLookupIterations}", () =>
            {
                var queryer = CreateQueryer();
                for (int i = 0; i < RandomLookupIterations; i++)
                {
                    int key = (i * 37) % RecordCount;
                    queryer.Get(key, null);
                }
            });
        }

        private void RunBatchFilterBenchmark()
        {
            RunPerformanceTest($"ChunkDataQueryer GetByKey x{BatchFilterIterations}", () =>
            {
                for (int i = 0; i < BatchFilterIterations; i++)
                {
                    int min = (i * 17) % (RecordCount - 64);
                    int max = min + 64;
                    var queryer = CreateQueryer();
                    int count = 0;
                    foreach (ChunkPerfRecord _ in queryer.GetByKey(id => id >= min && id < max, null))
                    {
                        count++;
                    }

                    Assert(count == 64, "Each batch key query should return a 64-item window.");
                }
            });
        }

        private void RunFullScanBenchmark()
        {
            RunPerformanceTest($"ChunkDataQueryer GetAll x{FullScanIterations}", () =>
            {
                for (int i = 0; i < FullScanIterations; i++)
                {
                    var queryer = CreateQueryer();
                    int count = 0;
                    foreach (ChunkPerfRecord _ in queryer.GetAll())
                    {
                        count++;
                    }

                    Assert(count == RecordCount, "Full scan should enumerate the whole dataset.");
                }
            });
        }

        private ChunkDataQueryer<int, ChunkPerfRecord> CreateQueryer()
        {
            var queryer = new ChunkDataQueryer<int, ChunkPerfRecord>();
            queryer.Prepare(_indexFilePath, _dataFilePath, item => item.Id);
            return queryer;
        }

        private void Cleanup()
        {
            if (!string.IsNullOrEmpty(_directory) && System.IO.Directory.Exists(_directory))
            {
                System.IO.Directory.Delete(_directory, true);
            }
        }

        [Serializable]
        private class ChunkPerfRecord
        {
            public int Id;
            public string Name;
            public int Group;
            public float Score;
        }
    }
}