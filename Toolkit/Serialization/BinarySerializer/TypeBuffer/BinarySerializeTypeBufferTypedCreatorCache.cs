using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    internal static class BinarySerializeTypeBufferTypedCreatorCache<T>
    {
        private static Func<T> _instance;

        private static readonly Dictionary<Type, Func<T>> Cache = new Dictionary<Type, Func<T>>();

        internal static Func<T> Instance
        {
            get
            {
                if (_instance == null)
                    _instance = BinarySerializeTypeBuffer.BuildTypedCreator<T>();

                return _instance;
            }
        }

        internal static Func<T> GetOrCreate(Type concreteType)
        {
            if (concreteType == typeof(T))
                return Instance;

            if (Cache.TryGetValue(concreteType, out var creator))
                return creator;

            creator = BuildCreatorAdapter(concreteType);
            Cache[concreteType] = creator;
            return creator;
        }

        private static Func<T> BuildCreatorAdapter(Type concreteType)
        {
            Func<object> creator = BinarySerializeTypeBuffer.GetBoxedCreator(concreteType);
            return () => (T)creator();
        }
    }
}