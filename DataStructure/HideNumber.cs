using System;
using System.Runtime.InteropServices;

namespace PowerCellStudio
{
    public interface IHideNumber<T> 
    {
        public T value { get; set; }
    }
    
    public struct HideInt : IHideNumber<int>, IEquatable<HideInt>, IComparable<HideInt>
    {
        private int encryptKey;
        private int encryptNum;
        
        public HideInt(int num = 0)
        {
            encryptKey = Randomizer.Default.Range(1000, 100000);
            encryptNum = num ^ encryptKey;
        }

        public int value
        {
            get => encryptNum ^ encryptKey;
            set 
            {
                if (encryptKey == 0)
                {
                    encryptKey = Randomizer.Default.Range(1000, 100000);
                }
                encryptNum = value ^ encryptKey;
            }
        }

        public static implicit operator int(HideInt hideInt) => hideInt.value;
        public static implicit operator HideInt(int num) => new HideInt(num);
        
        public bool Equals(HideInt other)
        {
            return value == other.value;
        }

        public int CompareTo(HideInt other)
        {
            return value.CompareTo(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is HideInt other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
    
    public struct HideFloat : IHideNumber<float>, IEquatable<HideFloat>, IComparable<HideFloat>
    {
        private int encryptKey;
        private int encryptNum;
        
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)] public float FloatValue;
            [FieldOffset(0)] public int IntValue;
        }
        
        public HideFloat(float num = 0)
        {
            encryptKey = Randomizer.Default.Range(1000, 100000);
            encryptNum = FloatToIntBits(num) ^ encryptKey;
        }

        public float value
        {
            get => IntBitsToFloat(encryptNum ^ encryptKey);
            set
            {
                if (encryptKey == 0)
                {
                    encryptKey = Randomizer.Default.Range(1000, 100000);
                }

                encryptNum = FloatToIntBits(value) ^ encryptKey;
            }
        }
        
        private static int FloatToIntBits(float value)
        {
            return new FloatIntUnion { FloatValue = value }.IntValue;
        }

        private static float IntBitsToFloat(int value)
        {
            return new FloatIntUnion { IntValue = value }.FloatValue;
        }

        public static implicit operator float(HideFloat hideFloat) => hideFloat.value;
        public static implicit operator HideFloat(float num) => new HideFloat(num);

        public bool Equals(HideFloat other)
        {
            return value == other.value;
        }

        public int CompareTo(HideFloat other)
        {
            return value.CompareTo(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is HideFloat other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
    
    public struct HideLong : IHideNumber<long>, IEquatable<HideLong>, IComparable<HideLong>
    {
        private long encryptKey;
        private long encryptNum;
        
        public HideLong(long num = 0)
        {
            encryptKey = Randomizer.Default.Range(1000L, 100000L);
            encryptNum = num ^ encryptKey;
        }

        public long value
        {
            get => encryptNum ^ encryptKey;
            set 
            {
                if (encryptKey == 0)
                {
                    encryptKey = Randomizer.Default.Range(1000L, 100000L);
                }
                encryptNum = value ^ encryptKey;
            }
        }

        public static implicit operator long(HideLong hideLong) => hideLong.value;
        public static implicit operator HideLong(long num) => new HideLong(num);

        public bool Equals(HideLong other)
        {
            return value == other.value;
        }

        public int CompareTo(HideLong other)
        {
            return value.CompareTo(other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is HideLong other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}