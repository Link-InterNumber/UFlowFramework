using UnityEngine;

namespace PowerCellStudio
{
    public interface IHideNumber<T>
    {
        public T value { get; set; }
    }
    
    public class HideInt : IHideNumber<int>
    {
        private int encryptKey;
        private int encryptNum;
        
        public HideInt(int num = 0)
        {
            encryptKey = Randomizer.Range(1000, 100000);
            encryptNum = num ^ encryptKey;
        }

        public int value
        {
            get => encryptNum ^ encryptKey;
            set => encryptNum = value ^ encryptKey;
        }
    }
    
    public class HideFloat : IHideNumber<float>
    {
        private int encryptKey;
        private int encryptNum;
        
        public HideFloat(float num = 0)
        {
            encryptKey = Randomizer.Range(1000, 100000);
            encryptNum = Mathf.RoundToInt(num * 10000) ^ encryptKey;
        }

        public float value
        {
            get => (encryptNum ^ encryptKey) / 10000f;
            set => encryptNum = Mathf.RoundToInt(value * 10000) ^ encryptKey;
        }
    }
    
    public class HideLong : IHideNumber<long>
    {
        private long encryptKey;
        private long encryptNum;
        
        public HideLong(long num = 0)
        {
            encryptKey = Randomizer.Range(1000L, 100000L);
            encryptNum = num ^ encryptKey;
        }

        public long value
        {
            get => encryptNum ^ encryptKey;
            set => encryptNum = value ^ encryptKey;
        }
    }
}