using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class ChunkDataToolChainTest : RunTestMono
    {
        private const string TestFileName = "ChunkDataToolChain";
        private const int RecordCount = 10;
        private const int ChunkSize = 3;

        private void OnEnable()
        {
            Debug.Log("========== DataChunk ToolChain Test Suite Started ==========");

            RunTest("ChunkMaker writes data and index files", TestChunkMakerWritesFilesAndIndexes);
            RunTest("ChunkDataQueryer prepares and loads by key", TestPrepareAndGet);
            RunTest("ChunkDataQueryer filters by key", TestGetByKey);
            RunTest("ChunkDataQueryer enumerates all data", TestGetAll);
            RunTest("ChunkDataQueryer clears cold chunks", TestTryClearUnused);
            RunTest("ChunkDataQueryer async prepare loads index", TestPrepareYieldInstruction);

            Debug.Log("========== DataChunk ToolChain Test Suite Finished ==========");
        }

        private void TestChunkMakerWritesFilesAndIndexes()
        {
            var context = CreateTestContext();
            try
            {
                Assert(File.Exists(context.DataFilePath), "Data file should exist after chunk generation.");
                Assert(File.Exists(context.IndexFilePath), "Index file should exist after chunk generation.");

                List<(int index, long offset, int[] keys)> chunks = ChunkReader.ReadIndexFile<int>(context.IndexFilePath).ToList();

                Assert(chunks.Count == 4, "Chunk count should match expected partition count.");
                Assert(chunks[0].index == 0 && chunks[0].offset == 0, "First chunk metadata mismatch.");
                Assert(chunks[0].keys.SequenceEqual(new[] { 0, 1, 2 }), "First chunk keys mismatch.");
                Assert(chunks[1].keys.SequenceEqual(new[] { 3, 4, 5 }), "Second chunk keys mismatch.");
                Assert(chunks[2].keys.SequenceEqual(new[] { 6, 7, 8 }), "Third chunk keys mismatch.");
                Assert(chunks[3].keys.SequenceEqual(new[] { 9 }), "Last chunk keys mismatch.");

                List<ChunkRecord> firstChunk = ChunkReader.ReadChunkData<ChunkRecord>(context.DataFilePath, chunks[0].offset).ToList();
                Assert(firstChunk.Count == 3, "First chunk should contain three records.");
                Assert(firstChunk[0].Id == 0 && firstChunk[2].Id == 2, "First chunk records mismatch.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private void TestPrepareAndGet()
        {
            var context = CreateTestContext();
            try
            {
                var queryer = CreatePreparedQueryer(context);
                List<int> loadedIds = new List<int>();

                ChunkRecord hit = queryer.Get(4, item => loadedIds.Add(item.Id));
                ChunkRecord sameChunkHit = queryer.Get(5, item => loadedIds.Add(item.Id));

                Assert(hit != null && hit.Id == 4, "Get should return the requested record.");
                Assert(sameChunkHit != null && sameChunkHit.Id == 5, "Get should return another record from same chunk.");
                Assert(loadedIds.SequenceEqual(new[] { 3, 4, 5 }), "First load should materialize only the matching chunk once.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private void TestGetByKey()
        {
            var context = CreateTestContext();
            try
            {
                var queryer = CreatePreparedQueryer(context);
                List<int> loadedIds = new List<int>();
                List<int> resultIds = queryer.GetByKey(id => id >= 7, item => loadedIds.Add(item.Id))
                    .Select(item => item.Id)
                    .OrderBy(id => id)
                    .ToList();

                Assert(resultIds.SequenceEqual(new[] { 7, 8, 9 }), "GetByKey should return all matching records.");
                Assert(loadedIds.SequenceEqual(new[] { 6, 7, 8, 9 }), "GetByKey should only load chunks that contain matching keys.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private void TestGetAll()
        {
            var context = CreateTestContext();
            try
            {
                var queryer = CreatePreparedQueryer(context);
                List<ChunkRecord> allRecords = queryer.GetAll().OrderBy(item => item.Id).ToList();

                Assert(allRecords.Count == RecordCount, "GetAll should enumerate every record.");
                Assert(allRecords[0].Id == 0 && allRecords[RecordCount - 1].Id == 9, "GetAll returned unexpected boundaries.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private void TestTryClearUnused()
        {
            var context = CreateTestContext();
            try
            {
                var queryer = CreatePreparedQueryer(context);

                queryer.Get(0, null);
                queryer.Get(0, null);
                queryer.Get(0, null);
                queryer.Get(3, null);
                queryer.Get(6, null);
                queryer.Get(9, null);

                queryer.TryClearUnused(null);

                List<int> keptChunkReloadIds = new List<int>();
                List<int> clearedChunkReloadIds = new List<int>();
                queryer.Get(0, item => keptChunkReloadIds.Add(item.Id));
                queryer.Get(3, item => clearedChunkReloadIds.Add(item.Id));

                Assert(keptChunkReloadIds.Count == 0, "Hot chunk should remain cached after cleanup.");
                Assert(clearedChunkReloadIds.SequenceEqual(new[] { 3, 4, 5 }), "Cold chunk should reload after cleanup.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private void TestPrepareYieldInstruction()
        {
            var context = CreateTestContext();
            try
            {
                var queryer = new ChunkDataQueryer<int, ChunkRecord>();
                IEnumerator routine = queryer.PrepareYieldInstruction(context.IndexFilePath, context.DataFilePath, item => item.Id, 2);
                ExecuteEnumerator(routine);

                List<int> loadedIds = new List<int>();
                ChunkRecord hit = queryer.Get(8, item => loadedIds.Add(item.Id));

                Assert(hit != null && hit.Id == 8, "Async prepare should initialize index and cache structures.");
                Assert(loadedIds.SequenceEqual(new[] { 6, 7, 8 }), "Async prepare should allow deferred chunk loading after initialization.");
            }
            finally
            {
                context.Dispose();
            }
        }

        private static ChunkDataQueryer<int, ChunkRecord> CreatePreparedQueryer(TestContext context)
        {
            var queryer = new ChunkDataQueryer<int, ChunkRecord>();
            queryer.Prepare(context.IndexFilePath, context.DataFilePath, item => item.Id);
            return queryer;
        }

        private static TestContext CreateTestContext()
        {
            string directory = Path.Combine(Application.temporaryCachePath, "DataChunkTests", Guid.NewGuid().ToString("N"));
            List<ChunkRecord> records = Enumerable.Range(0, RecordCount)
                .Select(index => new ChunkRecord
                {
                    Id = index,
                    Name = "record_" + index,
                    Value = index * 10
                })
                .ToList();

            ChunkMaker.StreamWriteSync<int, ChunkRecord>(directory, TestFileName, records, item => item.Id, ChunkSize);

            return new TestContext(directory, TestFileName);
        }

        [Serializable]
        private class ChunkRecord
        {
            public int Id;
            public string Name;
            public int Value;
        }

        private sealed class TestContext : IDisposable
        {
            public TestContext(string directory, string fileName)
            {
                Directory = directory;
                DataFilePath = Path.Combine(directory, fileName + "Data.bytes");
                IndexFilePath = Path.Combine(directory, fileName + "Index.bytes");
            }

            public string Directory { get; }

            public string DataFilePath { get; }

            public string IndexFilePath { get; }

            public void Dispose()
            {
                if (System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.Delete(Directory, true);
                }
            }
        }
    }
}