using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerCellStudio
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
            _buffer = new List<TypeRef>();
            var types = Assembly.GetAssembly(typeof(TypeRef)).GetTypes().Where(t => 
                !t.IsAbstract &&
                t.IsClass &&
                t.IsSubclassOf(typeof(TypeRef)));

            foreach (var type in types)
            {
                var resolver = (TypeRef)Activator.CreateInstance(type);
                _buffer.Add(resolver);
            }
            return _buffer;
        }
        
        public static void ClearBuffer()
        {
            _buffer?.Clear();
            _buffer = null;
        }
    }
}