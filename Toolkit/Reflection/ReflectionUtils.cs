using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    /// <summary>
    /// Reflection utility class for common reflection operations.
    /// 反射工具类，提供常用的反射操作方法。
    /// </summary>
    public static class ReflectionUtils
    {
        private const BindingFlags AnyMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const BindingFlags StaticMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags PublicInstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        #region Create Instance
        
        /// <summary>
        /// Creates an instance of the specified type.
        /// 创建指定类型的实例。
        /// </summary>
        /// <param name="type">The type to instantiate. 要实例化的类型。</param>
        /// <param name="parameters">Constructor arguments. 构造函数参数。</param>
        /// <returns>The created instance. 创建的实例。</returns>
        public static object CreateInstance(Type type, params object[] parameters)
        {
            if (!CanInstantiate(type, parameters, out var reason))
                throw new InvalidOperationException($"Cannot create an instance of '{type.FullName}'. {reason}");
            return Activator.CreateInstance(type, parameters);
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// 创建指定类型的实例。
        /// </summary>
        /// <param name="parameters">Constructor arguments. 构造函数参数。</param>
        /// <typeparam name="T">The type to instantiate. 要实例化的类型。</typeparam>
        /// <returns>The created instance. 创建的实例。</returns>
        public static T CreateInstance<T>(params object[] parameters)
        {
            var type = typeof(T);
            return (T)CreateInstance(type, parameters);
        }

        /// <summary>
        /// Determines whether an instance of the specified type can be created with the given parameters.
        /// 判断是否可以使用给定参数创建指定类型的实例。
        /// </summary>
        /// <param name="type">The type to check. 要检查的类型。</param>
        /// <returns>True if the type can be instantiated; otherwise, false. 如果类型可以实例化，则为 true；否则为 false。</returns>
        public static bool CanInstantiate(Type type)
        {
            return CanInstantiate(type, Array.Empty<object>(), out _);
        }

        /// <summary>
        /// Determines whether an instance of the specified type can be created with the given parameters.
        /// 判断是否可以使用给定参数创建指定类型的实例。
        /// </summary>
        /// <param name="type">The type to check. 要检查的类型。</param>
        /// <param name="parameters">Constructor arguments. 构造函数参数。</param>
        /// <returns>True if the type can be instantiated; otherwise, false. 如果类型可以实例化，则为 true；否则为 false。</returns>
        public static bool CanInstantiate(Type type, params object[] parameters)
        {
            return CanInstantiate(type, parameters, out _);
        }

        /// <summary>
        /// Determines whether an instance of the specified type can be created with the given parameters, and
        /// provides a reason if it cannot be instantiated.
        /// 判断是否可以使用给定参数创建指定类型的实例，并在无法实例化时提供原因。
        /// </summary> <param name="type">The type to check. 要检查的类型。</param>
        /// <param name="parameters">Constructor arguments. 构造函数参数。</param>
        /// <param name="reason">The reason why the type cannot be instantiated, if applicable. 如果类型无法实例化，提供原因。</param>
        /// <returns>True if the type can be instantiated; otherwise, false. 如果类型可以实例化，则为 true；否则为 false。</returns> 
        public static bool CanInstantiate(Type type, object[] parameters, out string reason)
        {
            if (type == null)
            {
                reason = "Type is null.";
                return false;
            }

            if (type.IsAbstract)
            {
                reason = "Abstract types cannot be instantiated.";
                return false;
            }

            if (type.IsInterface)
            {
                reason = "Interfaces cannot be instantiated.";
                return false;
            }

            if (type == typeof(void))
            {
                reason = "System.Void cannot be instantiated.";
                return false;
            }

            if (type.ContainsGenericParameters)
            {
                reason = "Open generic types cannot be instantiated.";
                return false;
            }

            // if (typeof(MonoBehaviour).IsAssignableFrom(type))
            // {
            //     reason = "MonoBehaviour types must be created with GameObject.AddComponent.";
            //     return false;
            // }
            //
            // if (typeof(ScriptableObject).IsAssignableFrom(type))
            // {
            //     reason = "ScriptableObject types must be created with ScriptableObject.CreateInstance.";
            //     return false;
            // }

            if (!HasMatchingConstructor(type, parameters))
            {
                reason = "No matching instance constructor was found for the provided arguments.";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool HasMatchingConstructor(Type type, object[] parameters)
        {
            parameters ??= Array.Empty<object>();

            var parameterTypes = parameters.Select(parameter => parameter?.GetType() ?? typeof(object)).ToArray();

            return ReflectionTypeBuff.GetConstructor(type, PublicInstanceFlags, parameterTypes) != null
                   || ReflectionTypeBuff.GetConstructors(type, PublicInstanceFlags)
                       .Any(constructor => ParametersMatch(constructor.GetParameters(), parameters));
        }

        private static bool ParametersMatch(ParameterInfo[] ctorParameters, object[] providedParameters)
        {
            if (ctorParameters.Length != providedParameters.Length)
            {
                return false;
            }

            for (var index = 0; index < ctorParameters.Length; index++)
            {
                var providedParameter = providedParameters[index];
                var targetType = ctorParameters[index].ParameterType;

                if (providedParameter == null)
                {
                    if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    {
                        return false;
                    }

                    continue;
                }

                if (!targetType.IsInstanceOfType(providedParameter))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Instance

        /// <summary>
        /// Gets the value of a property from an object.
        /// 获取对象的属性值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <returns>The property value. 属性值。</returns>
        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = ReflectionTypeBuff.GetProperty(obj.GetType(), propertyName, AnyMemberFlags);
            return prop?.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a property on an object.
        /// 设置对象的属性值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = ReflectionTypeBuff.GetProperty(obj.GetType(), propertyName, AnyMemberFlags);
            prop?.SetValue(obj, value);
        }

        /// <summary>
        /// Gets the value of a field from an object.
        /// 获取对象的字段值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <returns>The field value. 字段值。</returns>
        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = ReflectionTypeBuff.GetField(obj.GetType(), fieldName, AnyMemberFlags);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a field on an object.
        /// 设置对象的字段值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = ReflectionTypeBuff.GetField(obj.GetType(), fieldName, AnyMemberFlags);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// Invokes a method on an object.
        /// 调用对象的方法。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <returns>The return value. 返回值。</returns>
        public static object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var method = ReflectionTypeBuff.GetMethod(obj.GetType(), methodName, AnyMemberFlags);
            return method?.Invoke(obj, parameters);
        }

        #endregion

        #region Generic

        /// <summary>
        /// Creates an instance of a generic type.
        /// 创建泛型类型的实例。
        /// </summary>
        /// <param name="genericType">The generic type definition. 泛型类型定义。</param>
        /// <param name="typeArgs">The type arguments. 类型参数。</param>
        /// <param name="ctorArgs">Constructor arguments. 构造函数参数。</param>
        /// <returns>The created instance. 创建的实例。</returns>
        public static object CreateGenericInstance(Type genericType, Type[] typeArgs, params object[] ctorArgs)
        {
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));
            if (typeArgs == null) throw new ArgumentNullException(nameof(typeArgs));
            if (!genericType.IsGenericTypeDefinition)
                throw new ArgumentException("The provided type must be a generic type definition.", nameof(genericType));
            if (typeArgs.Length != genericType.GetGenericArguments().Length)
                throw new ArgumentException("The number of type arguments does not match the generic type definition.", nameof(typeArgs));
            var constructedType = genericType.MakeGenericType(typeArgs);
            if (!CanInstantiate(constructedType, ctorArgs, out var reason))
                throw new InvalidOperationException($"Cannot create an instance of '{genericType.FullName}' with the specified type arguments. {reason}");
            return Activator.CreateInstance(constructedType, ctorArgs);
        }

        /// <summary>
        /// Invokes a generic method (supports static and instance methods).
        /// 调用泛型方法（支持静态和实例方法）。
        /// </summary>
        /// <param name="obj">The object instance or type (for static). 对象实例或类型（静态方法时）。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="genericTypes">The generic type arguments. 泛型类型参数。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <param name="isStatic">Is static method. 是否为静态方法。</param>
        /// <returns>The return value. 返回值。</returns>
        public static object InvokeGenericMethod(object obj, string methodName, Type[] genericTypes, object[] parameters, bool isStatic = false)
        {
            if (obj == null && !isStatic) throw new ArgumentNullException(nameof(obj));
            var type = isStatic ? (Type)obj : obj.GetType();
            var methods = ReflectionTypeBuff.GetMethods(type, AnyMemberFlags);
            foreach (var method in methods)
            {
                if (method.Name == methodName && method.IsGenericMethodDefinition)
                {
                    var genericMethod = method.MakeGenericMethod(genericTypes);
                    return genericMethod.Invoke(isStatic ? null : obj, parameters);
                }
            }
            return null;
        }

        #endregion

        #region Static

        /// <summary>
        /// Invokes a static method.
        /// 调用静态方法。
        /// </summary>
        /// <param name="type">The type containing the static method. 包含静态方法的类型。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <returns>The return value. 返回值。</returns>
        public static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var method = ReflectionTypeBuff.GetMethod(type, methodName, StaticMemberFlags);
            return method?.Invoke(null, parameters);
        }

        /// <summary>
        /// Gets the value of a static property.
        /// 获取静态属性值。
        /// </summary>
        /// <param name="type">The type containing the static property. 包含静态属性的类型。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <returns>The property value. 属性值。</returns>
        public static object GetStaticPropertyValue(Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = ReflectionTypeBuff.GetProperty(type, propertyName, StaticMemberFlags);
            return prop?.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a static property.
        /// 设置静态属性值。
        /// </summary>
        /// <param name="type">The type containing the static property. 包含静态属性的类型。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetStaticPropertyValue(Type type, string propertyName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = ReflectionTypeBuff.GetProperty(type, propertyName, StaticMemberFlags);
            prop?.SetValue(null, value);
        }

        /// <summary>
        /// Gets the value of a static field.
        /// 获取静态字段值。
        /// </summary>
        /// <param name="type">The type containing the static field. 包含静态字段的类型。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <returns>The field value. 字段值。</returns>
        public static object GetStaticFieldValue(Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = ReflectionTypeBuff.GetField(type, fieldName, StaticMemberFlags);
            return field?.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a static field.
        /// 设置静态字段值。
        /// </summary>
        /// <param name="type">The type containing the static field. 包含静态字段的类型。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetStaticFieldValue(Type type, string fieldName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = ReflectionTypeBuff.GetField(type, fieldName, StaticMemberFlags);
            field?.SetValue(null, value);
        }

        #endregion

        public static bool needRefresh
        {
            get => ReflectionTypeBuff.NeedRefresh;
            set => ReflectionTypeBuff.NeedRefresh = value;
        }
        
        /// <summary>
        /// Gets all instantiable subclasses (including generic subclasses) of a given type in the specified assembly.
        /// 获取指定类型（包括泛型类型）在指定程序集中的所有可实例化子类。
        /// </summary>
        /// <param name="baseType">The base type or generic type definition. 基类或泛型类型定义。</param>
        /// <param name="assembly">The assembly to search. 要搜索的程序集。</param>
        /// <returns>List of instantiable subclasses. 可实例化子类的列表。</returns>
        public static List<Type> GetInstantiableSubtype(Type baseType, params Assembly[] assemblise)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            return ReflectionTypeBuff.GetInstantiableSubtype(baseType, assemblise, CanInstantiate, IsSubTypeOf);
        }

        /// <summary>
        /// Gets instances of all instantiable subclasses (including generic subclasses) of a given type in the specified assembly.
        /// 获取指定类型（包括泛型类型）在指定程序集中的所有可实例化子类的实例。
        /// </summary>
        /// <param name="match">Optional filter to select specific types. 可选的过滤器，用于选择特定类型。</param>
        /// <param name="assemblise">The assemblies to search. 要搜索的程序集。</param>
        /// <typeparam name="T">The base type. 基类类型。</typeparam>
        /// <returns>List of instances of the instantiable subclasses. 可实例化子类的实例列表。</returns>
        public static List<T> GetInstantiableSubtypeInstance<T>(Func<Type, bool> match, params Assembly[] assemblise)
        {
            var baseType = typeof(T);
            if (baseType.ContainsGenericParameters)
                throw new ArgumentException("The provided type must be a generic type definition.", nameof(baseType));
            var types = GetInstantiableSubtype(baseType, assemblise);
            var instances = new List<T>();
            foreach (var type in types)
            {
                if (match != null && !match(type)) continue;
                instances.Add((T)Activator.CreateInstance(type));
            }
            return instances;
        }
        
        /// <summary>
        /// Gets instances of all instantiable subclasses (including generic subclasses) of a given type in the specified assembly.
        /// 获取指定类型（包括泛型类型）在指定程序集中的所有可实例化子类的实例。
        /// </summary>
        /// <param name="assemblise">The assemblies to search. 要搜索的程序集。</param>
        /// <typeparam name="T">The base type. 基类类型。</typeparam>
        /// <returns>List of instances of the instantiable subclasses. 可实例化子类的实例列表。</returns>
        public static List<T> GetInstantiableSubtypeInstance<T>(params Assembly[] assemblise)
        {
            var baseType = typeof(T);
            if (baseType.ContainsGenericParameters)
                throw new ArgumentException("The provided type must be a generic type definition.", nameof(baseType));
            var types = GetInstantiableSubtype(baseType, assemblise);
            var instances = new List<T>();
            foreach (var type in types)
            {
                instances.Add((T)Activator.CreateInstance(type));
            }
            return instances;
        }
        
        /// <summary>
        /// Determines whether a type is a subtype of a specified base type or generic type definition.
        /// 判断一个类型是否是指定基类或泛型类型定义的子类型。
        /// </summary>
        /// <param name="baseType">The base type or generic type definition. 基类或泛型类型定义。</param>
        /// <param name="toCheck">The type to check. 要检查的类型。</ param>
        public static bool IsSubTypeOf(Type toCheck, Type baseType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (toCheck == null) throw new ArgumentNullException(nameof(toCheck));
            
            if (baseType.IsAssignableFrom(toCheck) && baseType != toCheck)
            {
                return true;
            }
            if (IsSubTypeOfRawGeneric(toCheck, baseType))
            {
                return true;
            }
            return false;
        }

        private static bool IsSubTypeOfRawGeneric(Type toCheck, Type generic)
        {
            if (!generic.IsGenericType) return false;
            if (generic.IsInterface)
            {
                Type[] interfaces = ReflectionTypeBuff.GetInterfaces(toCheck);
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type interfaceType = interfaces[i];
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == generic)
                    {
                        return true;
                    }
                }
            }
            else
            {
                var tempType = toCheck;
                while (tempType != null && tempType != typeof(object))
                {
                    if (tempType.IsGenericType && tempType.GetGenericTypeDefinition() == generic)
                        return true;
                    tempType = tempType.BaseType;
                }
            }
            return false;
        }

    }
}
