# 二进制序列化系统

## 😀 概述

二进制序列化系统用于将运行时对象序列化为紧凑的二进制字节流，并在需要时快速反序列化回对象实例。

该系统基于类型格式化器、集合类型推断、自定义类型选择器和 LZ4 压缩实现，适合配置缓存、存档数据、运行时快照、离线数据打包等场景。

主要组件如下：

| 组件 | 用途 |
|---|---|
| `BinarySerializer` | 对外公开的序列化/反序列化入口。 |
| `BinaryFormatterResolver` | 根据类型选择对应的格式化器。 |
| `BinarySerializeTypeBuffer` | 缓存字段布局、实例构造器、集合类型信息与自定义选择器。 |
| `IBinaryData` | 为类型提供自定义二进制写入/读取逻辑。 |
| `IBinarySerializerTypeSelector` | 为特殊类型注册独立的读写处理器。 |
| `BinarySerializerTypeSelector<T>` | 泛型自定义选择器基类，便于实现强类型扩展。 |
| 各类 `Formatter` | 处理基础类型、数组、集合、对象、枚举等具体序列化逻辑。 |

组件依赖关系
```
BinarySerializer
    |
    └──> BinaryFormatterResolver
            |
            |──> BinarySerializeTypeBuffer
            |       |
            |       |──> 自定义 TypeSelector 注册表
            |       └──> 字段布局 / 构造器 / 集合信息缓存
            |
            |──> IBinaryData
            |
            └──> 各类 Formatter
                    |
                    ├──> 基础类型 Formatter
                    ├──> 数组 / 集合 Formatter
                    ├──> CustomSelectorFormatter
                    └──> ObjectFormatter
```

---

## 🛠️ 使用

### 1.✨ **序列化对象**

使用 `BinarySerializer.Serialize<T>` 将对象序列化为字节数组。序列化结果会先写入二进制流，再经过 LZ4 压缩。

```csharp
[Serializable]
public class PlayerProfile
{
    public int Id;
    public string Name;
    public List<int> Scores;
}

var profile = new PlayerProfile
{
    Id = 7,
    Name = "Hero",
    Scores = new List<int> { 10, 20, 30 }
};

byte[] bytes = BinarySerializer.Serialize(profile);
```

### 2.🔁 **反序列化对象**

使用 `BinarySerializer.Deserialize<T>` 将字节数组恢复为目标类型。

```csharp
PlayerProfile profile = BinarySerializer.Deserialize<PlayerProfile>(bytes);
```

### 3.🔤 **修改字符串编码**

默认编码是 `UTF8`。如果业务需要，也可以在使用前改成其他编码。

```csharp
BinarySerializer.Encoding = Encoding.UTF8;
```

> 注意：同一份数据的序列化与反序列化应使用一致的编码配置。

---

## 🌟 扩展方式

### 1. 使用 `IBinaryData` 自定义二进制读写

如果某个类型希望完全接管自己的序列化过程，可以实现 `IBinaryData`。

```csharp
public struct ItemSnapshot : IBinaryData
{
    public int Id;
    public float Weight;

    public void ToBinary(BinaryWriter writer, Encoding encoding)
    {
        writer.Write(Id);
        writer.Write(Weight);
    }

    public void FromBinary(BinaryReader reader, Encoding encoding)
    {
        Id = reader.ReadInt32();
        Weight = reader.ReadSingle();
    }
}
```

这类类型在运行时会优先走 `BinaryDataTypeSelector<T>`，而不是默认对象字段反射路径。

### 2. 使用 `IBinarySerializerTypeSelector` 注册特殊类型处理器

如果不想修改目标类型本身，或者要为第三方类型编写专用读写逻辑，可以注册自定义 TypeSelector。

推荐继承 `BinarySerializerTypeSelector<T>`：

```csharp
public sealed class CustomEncodedValueSelector : BinarySerializerTypeSelector<CustomEncodedValue>
{
    public override void Write(BinaryWriter writer, CustomEncodedValue value, Encoding encoding)
    {
        writer.Write(value.Number);

        byte[] bytes = encoding.GetBytes(value.Text ?? string.Empty);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    public override CustomEncodedValue Read(BinaryReader reader, Encoding encoding)
    {
        int number = reader.ReadInt32();
        int length = reader.ReadInt32();
        string text = encoding.GetString(reader.ReadBytes(length));

        return new CustomEncodedValue
        {
            Number = number,
            Text = text
        };
    }
}

BinarySerializer.RegisterCustomSelector(new CustomEncodedValueSelector());
```

### 3. 内置特殊类型支持

系统启动时已经默认注册了以下特殊类型选择器：

- `IntPtr`
- `UIntPtr`
- `Guid`
- `TimeSpan`
- `DateTimeOffset`

这些类型会优先走内置 TypeSelector，而不是默认对象字段反射流程。

---

## 📦 支持的类型范围

根据当前实现和测试覆盖，系统支持以下常见类型：

### 1. 基础类型

- `bool`
- `byte` / `sbyte`
- `short` / `ushort`
- `int` / `uint`
- `long` / `ulong`
- `float`
- `double`
- `decimal`
- `char`
- `string`
- `DateTime`
- `enum`

### 2. 常见值类型

- `Guid`
- `TimeSpan`
- `DateTimeOffset`
- 其他实现了 `IBinaryData` 或注册了 selector 的值类型

### 3. 数组与集合

- 一维数组 `T[]`
- `List<T>`
- `Dictionary<TKey, TValue>`
- `HashSet<T>`
- `Queue<T>`
- `Stack<T>`
- `KeyValuePair<TKey, TValue>`
- 字段声明为 `IList<T>` / `ICollection<T>` / `IDictionary<TKey, TValue>` / `ISet<T>` 时，也会自动推断为可用的具体集合类型

### 4. 普通对象

默认对象会走 `ObjectFormatter<T>`，系统会读取实例字段并递归序列化。

支持内容包括：

- 公有实例字段
- Unity 环境下带 `[SerializeField]` 的私有实例字段
- 嵌套对象
- 嵌套集合
- 字段值为 `null` 的引用类型

---

## ⚙️ 内部机制与优化

### 1. 类型格式化器缓存

`BinaryFormatterResolver` 会为每个类型缓存对应的 `IBinaryFormatter`，避免重复反射创建。

### 2. 字段布局缓存

`BinarySerializeTypeBuffer` 会缓存：

- 类型字段布局
- 实例构造器
- 集合泛型信息
- 集合辅助方法（如 `Enqueue` / `Push`）
- 自定义 TypeSelector 映射

这样可以减少重复反射和运行时分配。

### 3. 集合接口自动落地

当字段声明为接口类型时，系统会尝试映射到默认具体实现：

- `IList<T>` / `ICollection<T>` -> `List<T>`
- `IDictionary<TKey, TValue>` -> `Dictionary<TKey, TValue>`
- `ISet<T>` -> `HashSet<T>`

### 4. LZ4 压缩

`BinarySerializer.Serialize<T>` 在完成对象写入后，会调用 LZ4 进行压缩；`Deserialize<T>` 则会先解压，再进入反序列化流程。

这能减小持久化体积，但也意味着序列化结果并不是裸二进制流，而是压缩后的字节数组。

---

## 🔍 示例场景

### 1. 普通对象往返序列化

```csharp
[Serializable]
public class ConfigData
{
    public int Id;
    public string Name;
    public Dictionary<string, int> Stats;
}

var source = new ConfigData
{
    Id = 1,
    Name = "ConfigA",
    Stats = new Dictionary<string, int>
    {
        { "hp", 100 },
        { "mp", 50 }
    }
};

byte[] bytes = BinarySerializer.Serialize(source);
ConfigData clone = BinarySerializer.Deserialize<ConfigData>(bytes);
```

### 2. 使用接口字段承载集合

```csharp
[Serializable]
public class InterfaceCollectionContainer
{
    public IList<int> Items;
    public IDictionary<string, int> Mappings;
    public ISet<string> Tags;
}
```

这类接口字段在反序列化时会恢复为默认具体集合实现。

### 3. 处理 Unity 私有序列化字段

```csharp
[Serializable]
public class PrivateFieldContainer
{
    [SerializeField]
    private int _hiddenNumber;
}
```

在 Unity 环境下，这类字段会被纳入序列化字段列表。

---

## ⚙️ 关键接口说明

### `BinarySerializer` 常用方法

- `Serialize<T>(T obj)`
  - 将对象写入二进制流并进行 LZ4 压缩。
- `Deserialize<T>(byte[] data)`
  - 将压缩字节解压并还原为目标对象。
- `RegisterCustomSelector(IBinarySerializerTypeSelector selector)`
  - 注册一个自定义类型处理器。

### `IBinaryData`

- `ToBinary(BinaryWriter writer, Encoding encoding)`
- `FromBinary(BinaryReader reader, Encoding encoding)`

### `IBinarySerializerTypeSelector`

- `TargetType`
- `Write(BinaryWriter writer, object value, Encoding encoding)`
- `Read(BinaryReader reader, Encoding encoding)`

### `IBinarySerializerTypeSelector<T>`

- `Write(BinaryWriter writer, T value, Encoding encoding)`
- `Read(BinaryReader reader, Encoding encoding)`

### `BinarySerializerTypeSelector<T>`

- 为自定义 TypeSelector 提供泛型基类，推荐优先使用。

---

## ℹ️ 注意事项

1. 根类型不能是接口、抽象类或未闭合泛型类型。
2. `UnityEngine.Object` 及其派生类型不在这套序列化工具的直接支持范围内。
3. 普通引用类型在反序列化时，优先要求存在可调用的无参构造函数；若没有无参构造函数，则需要显式标记 `[Serializable]`，系统才会使用未初始化对象回退。
4. 字段序列化是基于字段而不是属性；自动属性的 backing field 是否参与，取决于其字段可见性和是否满足字段筛选条件。
5. 字符串编码来自 `BinarySerializer.Encoding`；序列化与反序列化必须保持一致。
6. 如果你为某个类型同时提供了 `IBinaryData` 和 `IBinarySerializerTypeSelector`，当前解析顺序会优先走 `IBinaryData`。
7. 自定义 selector 注册通常建议在系统初始化阶段完成，避免在序列化过程中动态切换同一类型的处理策略。

---

## ✅ 场景推荐

- 配置对象二进制缓存
- 本地存档或快照数据
- 需要较小存储体积的离线数据打包
- 需要对特定类型接管底层读写逻辑的场景
- 需要在 Unity 运行时快速恢复结构化对象数据的场景
- 网络通信中序列化信息包