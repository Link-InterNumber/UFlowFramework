using System.Collections.Generic;

namespace PowerCellStudio
{
    public class BitsState
    {
        private readonly List<ulong> bits;

        public BitsState()
        {
            bits = new List<ulong>();
        }

        public void SetState(int stateIndex, bool state)
        {
            if (stateIndex < 0)
            {
                LinkLogger.LogError($"State index must be non-negative: {stateIndex}");
                return;
            }
            // 64位一个ulong
            var index = stateIndex / 64;
            var bitIndex = stateIndex % 64;
            if (index >= bits.Count)
                while (bits.Count <= index)
                {
                    bits.Add(0);
                }
            if (state)
            {
                bits[index] |= 1UL << bitIndex;
            }
            else
            {
                bits[index] &= ~(1UL << bitIndex);
            }
        }

        public bool GetState(int stateIndex)
        {
            if (stateIndex < 0)
            {
                LinkLogger.LogError($"State index must be non-negative: {stateIndex}");
                return false;
            }
            var index = stateIndex / 64;
            var bitIndex = stateIndex % 64;
            if (index >= bits.Count) return false;
            return (bits[index] & (1UL << bitIndex)) != 0;
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
    }
}