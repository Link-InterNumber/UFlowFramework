using UnityEngine;

namespace PowerCellStudio
{
    public class YieldInstructionCompletionSource<T> : CustomYieldInstruction
    {
        public bool IsCompleted { get; private set; }
        public T Result { get; private set; }
            
        public YieldInstructionCompletionSource()
        {
            IsCompleted = false;
        }
            
        public void SetResult(T result)
        {
            IsCompleted = true;
            Result = result;
        }

        public override bool keepWaiting => !IsCompleted;
    }
    
    public class ResultPasser<T>
    {
        public T Result { get; private set; }
        
        public void SetResult(T result)
        {
            Result = result;
        }
    }
}