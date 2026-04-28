using System;
using System.IO;
using System.Text;
using K4os.Compression.LZ4;

namespace PowerCellStudio
{
    public class BinarySerializer
    {
        // 编码使用 UTF-8，可替换为其他编码
        public static Encoding Encoding = Encoding.UTF8;

        #region 序列化入口

        private static bool IsSupportedType(Type type)
        {
            if (type.IsInterface || type.IsAbstract || type.ContainsGenericParameters)
                return false;
#if UNITY_5_3_OR_NEWER
            // 处理 UnityEngine.Object 类型（不支持直接序列化）
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }
#endif
            return true;
        }
        
        
        /// <summary>
        /// 将对象序列化为字节数组。
        /// </summary>
        public static byte[] Serialize<T>(T obj)
        {
            var type = typeof(T);
            if (!IsSupportedType(type))
                throw new NotSupportedException($"[BinarySerializer] 不支持的类型，无法序列化。类型: {obj?.GetType()}");
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding))
            {
                BinarySerializeHandler.WriteValue(writer, obj, type, Encoding);
                return LZ4Pickler.Pickle(ms.ToArray());
            }
        }

        /// <summary>
        /// 从字节数组反序列化为指定类型的实例。
        /// </summary>
        public static T Deserialize<T>(byte[] data)
        {
            if (!IsSupportedType(typeof(T)))
                throw new NotSupportedException($"[BinarySerializer] 不支持的类型，无法反序列化。类型: {typeof(T)}");
            var unpickledData = LZ4Pickler.Unpickle(data);
            using (var ms = new MemoryStream(unpickledData))
            using (var reader = new BinaryReader(ms, Encoding))
            {
                var type = typeof(T);
                return (T)BinaryDeserializeHandler.ReadValue(reader, type, Encoding);
            }
        }

        #endregion

        #region 自定义类型注册

        public static void RegisterCustomSelector(IBinarySerializerTypeSelector selector)
        {
            BinarySerializeTypeBuffer.RegisterCustomSelector(selector);
        }

        #endregion
    }
}