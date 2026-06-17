using System;
using System.Collections.Generic;
using System.Reflection;

namespace PowerCellStudio
{
    public static class PersistenceVersionRouter
    {
        private static bool _initialized;

        private static readonly Dictionary<(Type targetType, int fromVersion), List<(int toVersion, MethodInfo method)>>
            _migrationMethods = new Dictionary<(Type targetType, int fromVersion), List<(int toVersion, MethodInfo method)>>();

        public static int GetCurrentVersion(Type dataType)
        {
            if (dataType == null)
            {
                return 1;
            }

            var attribute = dataType.GetCustomAttribute<PersistenceDataVersionAttribute>();
            return attribute?.version ?? 1;
        }

        public static string SerializeString<T>(PlayerDataType dataType, T data)
        {
            return SerializeUtils.SerializeToJson(data);
        }

        public static byte[] SerializeBinary<T>(PlayerDataType dataType, T data)
        {
            return SerializeUtils.SerializeToBinary(data, BinaryObjectSerializationMode.Safe);
        }

        public static T DeserializeString<T>(PlayerDataType dataType, int version, string payload)
        {
            return SerializeUtils.DeserializeFromJson<T>(payload);
        }

        public static T DeserializeBinary<T>(PlayerDataType dataType, int version, byte[] payload)
        {
            return SerializeUtils.DeserializeFromBinary<T>(payload, 0, -1, BinaryObjectSerializationMode.Safe);
        }

        public static bool TryUpgrade<T>(int sourceVersion, T sourceData, out T result, out bool upgraded)
        {
            EnsureInitialized();
            result = sourceData;
            upgraded = false;

            if (sourceData == null || !typeof(IPersistenceData).IsAssignableFrom(typeof(T)))
            {
                return false;
            }

            var currentVersion = GetCurrentVersion(typeof(T));
            var version = Math.Max(0, sourceVersion);
            if (version >= currentVersion)
            {
                return true;
            }

            upgraded = true;
            object currentData = sourceData;
            while (version < currentVersion)
            {
                if (_migrationMethods.TryGetValue((typeof(T), version), out var methods) && methods.Count > 0)
                {
                    var selected = methods[0];
                    var migrationResult = selected.method.Invoke(currentData, null);
                    if (selected.method.ReturnType != typeof(void) && migrationResult != null && migrationResult is T)
                    {
                        currentData = migrationResult;
                    }

                    if (currentData is not T typedData)
                    {
                        LinkLogger.LogError($"[PlayerDataUtils] Invalid migration result for {typeof(T).Name}: {selected.method.Name}");
                        return false;
                    }

                    result = typedData;
                    version = selected.toVersion;
                    continue;
                }

                version++;
            }

            return true;
        }

        public static List<string> GetConfigurationWarnings()
        {
            EnsureInitialized();

            var warnings = new List<string>();
            var allTypes = ReflectionUtils.GetInstantiableSubtype(typeof(IPersistenceData));
            foreach (var type in allTypes)
            {
                if (type == null)
                {
                    continue;
                }

                var currentVersion = GetCurrentVersion(type);
                if (currentVersion <= 1)
                {
                    continue;
                }

                var missingVersions = new List<int>();
                for (var version = 1; version < currentVersion; version++)
                {
                    if (!_migrationMethods.ContainsKey((type, version)))
                    {
                        missingVersions.Add(version);
                    }
                }

                if (missingVersions.Count == 0)
                {
                    continue;
                }

                warnings.Add($"{type.FullName} 当前版本为 {currentVersion}，但缺少起始版本 {string.Join(", ", missingVersions)} 的 PersistenceMigrationMethodAttribute 迁移方法。");
            }

            return warnings;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            var types = ReflectionUtils.GetInstantiableSubtype(typeof(IPersistenceData));
            for (var j = 0; j < types.Count; j++)
            {
                var type = types[j];
                if (type == null)
                {
                    continue;
                }

                RegisterMethods(type);
            }

            foreach (var pair in _migrationMethods)
            {
                pair.Value.Sort((left, right) => left.toVersion.CompareTo(right.toVersion));
            }
        }

        private static void RegisterMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                RegisterMigrationMethod(method);
            }
        }

        private static void RegisterMigrationMethod(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes<PersistenceMigrationMethodAttribute>(false);
            foreach (var attribute in attributes)
            {
                var parameters = method.GetParameters();
                var declaringType = method.DeclaringType;
                if (declaringType == null || !typeof(IPersistenceData).IsAssignableFrom(declaringType) || method.IsStatic || parameters.Length != 0)
                {
                    continue;
                }

                if (method.ReturnType != typeof(void) && !declaringType.IsAssignableFrom(method.ReturnType))
                {
                    continue;
                }

                var key = (declaringType, attribute.fromVersion);
                if (!_migrationMethods.TryGetValue(key, out var list))
                {
                    list = new List<(int toVersion, MethodInfo method)>();
                    _migrationMethods[key] = list;
                }

                list.Add((attribute.toVersion, method));
            }
        }
    }
}