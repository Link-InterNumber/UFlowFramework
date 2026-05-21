using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class RandomizerDeterminismTest : RunTestMono
    {
        private void OnEnable()
        {
            RunAllTests();
        }

        [ContextMenu("Run Randomizer Determinism Tests")]
        public void RunAllTests()
        {
            RunTest("Randomizer same seed same sequence", TestSameSeedProducesSameSequence);
            RunTest("Randomizer reset seed replays sequence", TestResetSeedReplaysSequence);
        }

        private void TestSameSeedProducesSameSequence()
        {
            var left = new Randomizer();
            var right = new Randomizer();
            left.SetSeed(123456789);
            right.SetSeed(123456789);

            for (int i = 0; i < 32; i++)
            {
                Assert(left.RandomInt() == right.RandomInt(), $"RandomInt mismatch at index {i}");
                Assert(left.Range(-1000, 1000) == right.Range(-1000, 1000), $"Range(int) mismatch at index {i}");
                Assert(left.RandomUInt() == right.RandomUInt(), $"RandomUInt mismatch at index {i}");
                Assert(Mathf.Approximately(left.Value01(), right.Value01()), $"Value01 mismatch at index {i}");
            }
        }

        private void TestResetSeedReplaysSequence()
        {
            var randomizer = new Randomizer();
            randomizer.SetSeed(20240521);

            var expected = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                expected.Add(randomizer.Range(-5000, 5000));
            }

            randomizer.SetSeed(20240521);
            for (int i = 0; i < expected.Count; i++)
            {
                int actual = randomizer.Range(-5000, 5000);
                Assert(actual == expected[i], $"Replay mismatch at index {i}");
            }
        }
    }
}