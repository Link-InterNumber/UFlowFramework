using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public static class ResolversTypeBuffer
    {
        private static List<TypeRef> _buffer;
        
        public static List<TypeRef> buffer => _buffer ?? InitBuffer();
            
        public static List<TypeRef> InitBuffer()
        {
            if (_buffer != null)
            {
                return _buffer;
            }
            _buffer = ReflectionUtils.GetInstantiableSubtypeInstance<TypeRef>();
            return _buffer;
        }
        
        public static void ClearBuffer()
        {
            _buffer?.Clear();
            _buffer = null;
        }
    }
}