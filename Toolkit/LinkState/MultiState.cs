using System.Collections.Generic;

namespace PowerCellStudio
{
    public class MultiState
    {
        private readonly List<bool> bits;

        public MultiState()
        {
            bits = new List<bool>();
        }

        public void SetState(int stateIndex, bool state)
        {
            if (stateIndex < 0)
            {
                LinkLogger.LogError($"State index must be non-negative: {stateIndex}");
                return;
            }

            EnsureSize(stateIndex);
            bits[stateIndex] = state;
        }

        public bool GetState(int stateIndex)
        {
            if (stateIndex < 0)
            {
                LinkLogger.LogError($"State index must be non-negative: {stateIndex}");
                return false;
            }

            if (stateIndex >= bits.Count)
            {
                return false;
            }

            return bits[stateIndex];
        }

        public void Clear()
        {
            bits.Clear();
        }

        public void SetBatchState(bool states, params int[] stateIndex)
        {
            foreach (var index in stateIndex)
            {
                SetState(index, states);
            }
        }

        public bool IsMatch(params int[] stateIndices)
        {
            foreach (var stateIndex in stateIndices)
            {
                if (!GetState(stateIndex))
                    return false;
            }
            return true;
        }

        private void EnsureSize(int stateIndex)
        {
            while (bits.Count <= stateIndex)
            {
                bits.Add(false);
            }
        }
    }
}