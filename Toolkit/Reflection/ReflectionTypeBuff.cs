using System;
using System.Collections.Generic;
using System.Reflection;

namespace PowerCellStudio
{
    internal static class ReflectionTypeBuff
    {
        private static readonly Dictionary<Assembly, Type[]> _assemblyTypesCache = new Dictionary<Assembly, Type[]>();
        private static readonly Dictionary<MemberLookupKey, PropertyInfo> _propertyCache = new Dictionary<MemberLookupKey, PropertyInfo>();
        private static readonly Dictionary<MemberLookupKey, FieldInfo> _fieldCache = new Dictionary<MemberLookupKey, FieldInfo>();
        private static readonly Dictionary<MemberLookupKey, MethodInfo> _methodCache = new Dictionary<MemberLookupKey, MethodInfo>();
        private static readonly Dictionary<TypeMethodFlagsKey, MethodInfo[]> _methodsCache = new Dictionary<TypeMethodFlagsKey, MethodInfo[]>();
        private static readonly Dictionary<TypeMethodFlagsKey, ConstructorInfo[]> _constructorCache = new Dictionary<TypeMethodFlagsKey, ConstructorInfo[]>();
        private static readonly Dictionary<ConstructorLookupKey, ConstructorInfo> _exactConstructorCache = new Dictionary<ConstructorLookupKey, ConstructorInfo>();
        private static readonly Dictionary<Type, Type[]> _interfacesCache = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<SubtypeLookupKey, List<Type>> _subtypeCache = new Dictionary<SubtypeLookupKey, List<Type>>();

        public static bool NeedRefresh { get; set; } = true;

        public static PropertyInfo GetProperty(Type type, string name, BindingFlags bindingFlags)
        {
            var key = new MemberLookupKey(type, name, bindingFlags);
            if (_propertyCache.TryGetValue(key, out var property))
            {
                return property;
            }

            property = type.GetProperty(name, bindingFlags);
            _propertyCache[key] = property;
            return property;
        }

        public static FieldInfo GetField(Type type, string name, BindingFlags bindingFlags)
        {
            var key = new MemberLookupKey(type, name, bindingFlags);
            if (_fieldCache.TryGetValue(key, out var field))
            {
                return field;
            }

            field = type.GetField(name, bindingFlags);
            _fieldCache[key] = field;
            return field;
        }

        public static MethodInfo GetMethod(Type type, string name, BindingFlags bindingFlags)
        {
            var key = new MemberLookupKey(type, name, bindingFlags);
            if (_methodCache.TryGetValue(key, out var method))
            {
                return method;
            }

            method = type.GetMethod(name, bindingFlags);
            _methodCache[key] = method;
            return method;
        }

        public static MethodInfo[] GetMethods(Type type, BindingFlags bindingFlags)
        {
            var key = new TypeMethodFlagsKey(type, bindingFlags);
            if (_methodsCache.TryGetValue(key, out var methods))
            {
                return methods;
            }

            methods = type.GetMethods(bindingFlags);
            _methodsCache[key] = methods;
            return methods;
        }

        public static ConstructorInfo[] GetConstructors(Type type, BindingFlags bindingFlags)
        {
            var key = new TypeMethodFlagsKey(type, bindingFlags);
            if (_constructorCache.TryGetValue(key, out var constructors))
            {
                return constructors;
            }

            constructors = type.GetConstructors(bindingFlags);
            _constructorCache[key] = constructors;
            return constructors;
        }

        public static ConstructorInfo GetConstructor(Type type, BindingFlags bindingFlags, Type[] parameterTypes)
        {
            parameterTypes ??= Array.Empty<Type>();

            var key = new ConstructorLookupKey(type, bindingFlags, BuildTypeArrayKey(parameterTypes));
            if (_exactConstructorCache.TryGetValue(key, out var constructor))
            {
                return constructor;
            }

            constructor = type.GetConstructor(bindingFlags, null, parameterTypes, null);
            _exactConstructorCache[key] = constructor;
            return constructor;
        }

        public static Type[] GetInterfaces(Type type)
        {
            if (_interfacesCache.TryGetValue(type, out var interfaces))
            {
                return interfaces;
            }

            interfaces = type.GetInterfaces();
            _interfacesCache[type] = interfaces;
            return interfaces;
        }

        public static List<Type> GetInstantiableSubtype(Type baseType, Assembly[] assemblies, Func<Type, bool> canInstantiate, Func<Type, Type, bool> isSubTypeOf)
        {
            RefreshSubtypeCacheIfNeeded();

            assemblies ??= Array.Empty<Assembly>();
            if (assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            var key = new SubtypeLookupKey(baseType, BuildAssemblyArrayKey(assemblies));
            if (_subtypeCache.TryGetValue(key, out var types))
            {
                return types;
            }

            types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = GetAssemblyTypes(assembly);
                for (var index = 0; index < assemblyTypes.Length; index++)
                {
                    var type = assemblyTypes[index];
                    if (type == null)
                    {
                        continue;
                    }

                    if (!isSubTypeOf(type, baseType) || !canInstantiate(type))
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            _subtypeCache[key] = types;
            return types;
        }

        private static void RefreshSubtypeCacheIfNeeded()
        {
            if (!NeedRefresh)
            {
                return;
            }

            _subtypeCache.Clear();
            _assemblyTypesCache.Clear();
            NeedRefresh = false;
        }

        private static Type[] GetAssemblyTypes(Assembly assembly)
        {
            if (_assemblyTypesCache.TryGetValue(assembly, out var types))
            {
                return types;
            }

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loadedTypes = ex.Types;
                var typeCount = 0;
                for (var index = 0; index < loadedTypes.Length; index++)
                {
                    if (loadedTypes[index] != null)
                    {
                        typeCount++;
                    }
                }

                types = new Type[typeCount];
                var writeIndex = 0;
                for (var index = 0; index < loadedTypes.Length; index++)
                {
                    if (loadedTypes[index] == null)
                    {
                        continue;
                    }

                    types[writeIndex++] = loadedTypes[index];
                }
            }

            _assemblyTypesCache[assembly] = types;
            return types;
        }

        private static string BuildAssemblyArrayKey(Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
            {
                return string.Empty;
            }

            var names = new string[assemblies.Length];
            for (var index = 0; index < assemblies.Length; index++)
            {
                names[index] = assemblies[index]?.FullName ?? string.Empty;
            }

            return string.Join("|", names);
        }

        private static string BuildTypeArrayKey(Type[] types)
        {
            if (types.Length == 0)
            {
                return string.Empty;
            }

            var names = new string[types.Length];
            for (var index = 0; index < types.Length; index++)
            {
                names[index] = types[index]?.AssemblyQualifiedName ?? string.Empty;
            }

            return string.Join("|", names);
        }

        private readonly struct MemberLookupKey : IEquatable<MemberLookupKey>
        {
            private readonly Type _type;
            private readonly string _name;
            private readonly BindingFlags _bindingFlags;

            public MemberLookupKey(Type type, string name, BindingFlags bindingFlags)
            {
                _type = type;
                _name = name;
                _bindingFlags = bindingFlags;
            }

            public bool Equals(MemberLookupKey other)
            {
                return _type == other._type && _name == other._name && _bindingFlags == other._bindingFlags;
            }

            public override bool Equals(object obj)
            {
                return obj is MemberLookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _type != null ? _type.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)_bindingFlags;
                    return hashCode;
                }
            }
        }

        private readonly struct TypeMethodFlagsKey : IEquatable<TypeMethodFlagsKey>
        {
            private readonly Type _type;
            private readonly BindingFlags _bindingFlags;

            public TypeMethodFlagsKey(Type type, BindingFlags bindingFlags)
            {
                _type = type;
                _bindingFlags = bindingFlags;
            }

            public bool Equals(TypeMethodFlagsKey other)
            {
                return _type == other._type && _bindingFlags == other._bindingFlags;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeMethodFlagsKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_type != null ? _type.GetHashCode() : 0) * 397) ^ (int)_bindingFlags;
                }
            }
        }

        private readonly struct ConstructorLookupKey : IEquatable<ConstructorLookupKey>
        {
            private readonly Type _type;
            private readonly BindingFlags _bindingFlags;
            private readonly string _parameterKey;

            public ConstructorLookupKey(Type type, BindingFlags bindingFlags, string parameterKey)
            {
                _type = type;
                _bindingFlags = bindingFlags;
                _parameterKey = parameterKey;
            }

            public bool Equals(ConstructorLookupKey other)
            {
                return _type == other._type && _bindingFlags == other._bindingFlags && _parameterKey == other._parameterKey;
            }

            public override bool Equals(object obj)
            {
                return obj is ConstructorLookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _type != null ? _type.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (int)_bindingFlags;
                    hashCode = (hashCode * 397) ^ (_parameterKey != null ? _parameterKey.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private readonly struct SubtypeLookupKey : IEquatable<SubtypeLookupKey>
        {
            private readonly Type _baseType;
            private readonly string _assemblyKey;

            public SubtypeLookupKey(Type baseType, string assemblyKey)
            {
                _baseType = baseType;
                _assemblyKey = assemblyKey;
            }

            public bool Equals(SubtypeLookupKey other)
            {
                return _baseType == other._baseType && _assemblyKey == other._assemblyKey;
            }

            public override bool Equals(object obj)
            {
                return obj is SubtypeLookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _baseType != null ? _baseType.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (_assemblyKey != null ? _assemblyKey.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}