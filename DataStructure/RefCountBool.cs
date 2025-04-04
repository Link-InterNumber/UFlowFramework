
namespace PowerCellStudio
{
    public class RefCountBool
    {
        private int _refCount;
        public int refCount => _refCount;

        public void Clear()
        {
            _refCount = 0;
        }

        public void Add(int v)
        {
            _refCount += v;
            if (_refCount < 0)
            {
                _refCount = 0;
            }
        }
        
        public static implicit operator int(RefCountBool i)
        {
            return i.refCount;
        }
        
        public static implicit operator bool(RefCountBool i)
        {
            return i.refCount > 0;
        }

        public static RefCountBool operator +(RefCountBool i, int v)
        {
            i.Add(v);
            return i;
        }
        
        public static int operator +(int v, RefCountBool i)
        {
            return i.refCount + v;
        }
        
        public static RefCountBool operator ++(RefCountBool i)
        {
            i.Add(1);
            return i;
        }
        
        public static RefCountBool operator -(RefCountBool i, int v)
        {
            i.Add(-v);
            return i;
        }
        
        public static int operator -(int v, RefCountBool i)
        {
            return i.refCount - v;
        }
        
        public static RefCountBool operator --(RefCountBool i)
        {
            i.Add(-1);
            return i;
        }
    }
}