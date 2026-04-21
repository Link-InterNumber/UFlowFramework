using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerCellStudio
{
    public class BinarySerializeTypeBuffer
    {
        // 类型缓存
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();

        // 特定类型自定义写入方式
        private static readonly Dictionary<Type, IBinarySerializerTypeSelector> _customSelectorCache = new Dictionary<Type, IBinarySerializerTypeSelector>();

        public static FieldInfo[] GetSerializableFields(Type type)
        {
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => !f.IsNotSerialized 
# if UNITY_5_3_OR_NEWER
                                && (f.IsPublic || f.IsDefined(typeof(UnityEngine.SerializeField), false))
# endif
                                )
                    .OrderBy(f => f.MetadataToken)
                    .ToArray();
                _fieldCache[type] = fields;
            }
            return fields;
        }

        public static void RegisterCustomSelector(IBinarySerializerTypeSelector selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            _customSelectorCache[selector.TargetType] = selector;
        }

        public static IBinarySerializerTypeSelector GetCustomSelector(Type type)
        {
            _customSelectorCache.TryGetValue(type, out var selector);
            return selector;
        }

    }
}