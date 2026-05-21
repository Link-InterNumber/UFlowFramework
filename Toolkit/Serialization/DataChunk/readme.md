# 数据Chunk系统

## 😀 概述

数据Chunk系统用于对大型数据文件进行分块存储和按需加载，适合配置表、资源数据、离线数据等场景。

该系统将数据分为多个chunk，并且为每个chunk维护索引和键集合。在运行时，只加载访问到的chunk，从而减少内存占用和加载时间。

主要组件如下：

| 组件 | 用途 |
|---|---|
| `ChunkMaker` | 将数据序列写入数据文件，并生成chunk索引文件。 |
| `ChunkReader` | 从数据文件和索引文件中读取chunk记录和索引描述。 |
| `ChunkIndexer<TKey>` | 解析索引文件，建立键到chunk索引、chunk索引到偏移的映射。 |
| `ChunkDataMap<TKey, TData>` | 缓存已加载chunk的数据，并维护chunk引用计数。 |
| `ChunkDataQueryer<TKey, TData>` | 对外提供按键查找、按条件查询、遍历所有数据等接口。 |
| `ChunkInfo` | chunk元数据结构，包含索引、偏移和键数组字节数据。 |
| `ChunkDataOptions ` | 定义chunk数据的加密/解密、序列化行为。 |

组件依赖关系
```
ChunkDataQueryer<TKey, TData>
        |
        |──> ChunkIndexer<TKey>-----------------> ChunkReader
        |
        └──> ChunkDataMap<TKey, TData>----------> ChunkReader

ChunkMaker -----> ChunkDataOptions -----> IChunkEncryptor / IChunkSerializer -----> ChunkInfo

ChunkReader -----> ChunkDataOptions -----> IChunkEncryptor / IChunkSerializer -----> ChunkInfo
```

---

## 🛠️ 使用

### 1.✨ **生成Chunk数据文件**

使用 `ChunkMaker.StreamWriteSync<TData, TKey>` 将数据写入磁盘，并同时生成数据文件与索引文件。

```csharp
var dataDirectory = "Assets/Res/DataChunk";
var fileName = "MyConfig";
var records = myDataList; // IEnumerable<MyData>
ChunkMaker.StreamWriteSync<MyData, int>(
    fileDirectory: dataDirectory,
    fileName: fileName,
    data: records,
    keySelector: item => item.id,
    chunkSize: 256,
    options: new ChunkDataOptions { EnableEncryption = true });
```

会生成两个文件：

- `MyConfigData.bytes`：真实数据文件，按chunk顺序写入数据记录。
- `MyConfigIndex.bytes`：chunk索引文件，保存chunk偏移与对应键集合。

如果你希望分块规则直接由外部决定，也可以传入一个 `key => chunkId` 方法作为分组依据：

```csharp
ChunkMaker.StreamWriteSync<MyData, int>(
    fileDirectory: dataDirectory,
    fileName: fileName,
    data: records,
    keySelector: item => item.id,
    keyToChunkIndex: key => key / 100,
    options: new ChunkDataOptions { EnableEncryption = true });
```

此时相同 `chunkId` 的记录会写入同一个 chunk，`ChunkInfo.index` 直接保存这个 `chunkId`。

### 2.🔍 **准备查询器**

在运行时使用 `ChunkDataQueryer<TKey, TData>` 加载索引并准备缓存结构。

```csharp
var queryer = new ChunkDataQueryer<int, MyData>();
var options = new ChunkDataOptions { EnableEncryption = true };
queryer.Prepare(
    indexFilePath: "Assets/Res/DataChunk/MyConfigIndex.bytes",
    dataFilePath: "Assets/Res/DataChunk/MyConfigData.bytes",
    keySelector: data => data.id,
    options: options);
```

如果需要在协程中异步初始化索引，可使用：

```csharp
yield return queryer.PrepareYieldInstruction(
    indexFilePath: indexPath,
    dataFilePath: dataPath,
    keySelector: data => data.id,
    options: options);
```

> 注意：`Prepare` / `PrepareYieldInstruction` 只初始化索引和缓存结构，不会加载具体数据chunk。

### 2.1.🧩 **自定义序列化与加密**

`ChunkDataOptions` 支持替换默认的序列化器和加密器：

- `Serializer` 默认使用 `DefaultChunkSerializer`，底层走 `BinarySerializer`。
- `Encryptor` 默认使用 `DefaultChunkEncryptor`，底层走 `EncryptUtils.AESEncrypt/AESDecrypt`。
- `EnableEncryption = false` 时不会调用任何加密器。

最重要的约束是：写入和读取必须使用同一套 `ChunkDataOptions`，否则会出现无法解密或无法反序列化的问题。

#### a. 自定义序列化器

```csharp
public sealed class VersionedChunkSerializer : IChunkSerializer
{
    private const byte CurrentVersion = 1;

    public byte[] Write<T>(T data)
    {
        byte[] payload = BinarySerializer.Serialize(data);
        byte[] result = new byte[payload.Length + 1];
        result[0] = CurrentVersion;
        Buffer.BlockCopy(payload, 0, result, 1, payload.Length);
        return result;
    }

    public T Read<T>(byte[] bytes, int offset, int count)
    {
        if (count <= 0)
            throw new InvalidOperationException("Chunk payload is empty.");

        byte version = bytes[offset];
        if (version != CurrentVersion)
            throw new InvalidOperationException($"Unsupported chunk serializer version: {version}");

        return BinarySerializer.Deserialize<T>(bytes, offset + 1, count - 1);
    }
}
```

使用方式：

```csharp
var options = new ChunkDataOptions
{
    EnableEncryption = false,
    Serializer = new VersionedChunkSerializer()
};

ChunkMaker.StreamWriteSync<MyData, int>(
    fileDirectory: dataDirectory,
    fileName: fileName,
    data: records,
    keySelector: item => item.id,
    chunkSize: 256,
    options: options);

var queryer = new ChunkDataQueryer<int, MyData>();
queryer.Prepare(indexPath, dataPath, data => data.id, options);
```

#### b. 自定义加密器

```csharp
public sealed class XorChunkEncryptor : IChunkEncryptor
{
    private readonly byte _key;

    public XorChunkEncryptor(byte key)
    {
        _key = key;
    }

    public byte[] Encrypt(byte[] data)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ _key);
        }
        return result;
    }

    public byte[] Decrypt(byte[] data, int offset, int count)
    {
        var result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = (byte)(data[offset + i] ^ _key);
        }
        return result;
    }
}
```

使用方式：

```csharp
var options = new ChunkDataOptions
{
    EnableEncryption = true,
    Serializer = DefaultChunkSerializer.Instance,
    Encryptor = new XorChunkEncryptor(0x5A)
};

ChunkMaker.StreamWriteSync<MyData, int>(
    fileDirectory: dataDirectory,
    fileName: fileName,
    data: records,
    keySelector: item => item.id,
    chunkSize: 256,
    options: options);

var queryer = new ChunkDataQueryer<int, MyData>();
queryer.Prepare(indexPath, dataPath, data => data.id, options);
```

#### c. 同时自定义序列化和加密

```csharp
var options = new ChunkDataOptions
{
    EnableEncryption = true,
    Serializer = new VersionedChunkSerializer(),
    Encryptor = new XorChunkEncryptor(0x5A)
};
```

该配置会同时作用于：

- 数据文件中每条记录的写入与读取。
- 索引文件中 `ChunkInfo` 的写入与读取。
- 每个 chunk 键数组 `TKey[]` 的序列化与反序列化。

#### d. 接口实现注意点

- `IChunkSerializer.Read` 必须正确处理 `offset` 和 `count`，不要默认整个数组都是有效数据。
- `IChunkEncryptor.Decrypt` 必须只解密 `[offset, offset + count)` 这段有效区间。
- 自定义 `Serializer` 需要能处理 `TData`、`ChunkInfo` 和 `TKey[]`，因为这三类数据都会经过同一个序列化器。
- 如果启用了自定义加密，读取端必须传入相同的加密器配置，否则 `ChunkReader` 无法还原原始数据。

### 3.📦 **按需加载与查询**

#### a. 获取单条数据

```csharp
var data = queryer.Get(key, onAdd: item => {
    // 可选回调：当新的单条数据加载时触发
});
```

`Get` 会根据键找到对应chunk索引，若chunk尚未加载则自动加载；之后在chunk缓存中查找对应数据。

#### b. 批量查找符合键条件的数据

```csharp
foreach (var item in queryer.GetByKey(k => k > 100, onAdd: item => {
    // chunk加载回调
}))
{
    // 处理 item
}
```

该方法会根据键条件找到对应chunk索引，并加载关联chunk，之后逐条返回满足条件的结果。

#### c. 遍历所有数据

```csharp
foreach (var item in queryer.GetAll())
{
    // 处理 item
}
```

`GetAll` 会遍历所有chunk。如果chunk已加载则直接从缓存读取，否则按需从数据文件读取该chunk并返回数据。

#### d. 通用条件查询

```csharp
foreach (var item in queryer.Find(data => data.name.Contains("test")))
{
    // 处理 item
}
```

`Find` 会基于 `GetAll()` 遍历所有数据，并应用给定的谓词过滤。

---

## 🌟 内部机制与优化

### 1. chunk索引和偏移

索引文件由 `ChunkMaker` 创建，记录每个chunk的：

- `index`：chunk编号。
- `offset`：chunk在数据文件中的起始字节偏移。
- `keys`：该chunk包含的所有键。

`ChunkIndexer<TKey>` 会读取这些信息，并建立：

- `_keyMap`：键到chunk编号的映射。
- `_offsetMap`：chunk编号到文件偏移的映射。

### 2. 分块数据读取格式

每个chunk内的数据文件结构为：

- 4字节长度前缀
- 实际数据字节流
- 重复上述记录
- 4字节 `0` 作为chunk结束标志

`ChunkReader.ReadChunkData<TData>` 会按该格式读取每条记录，并反序列化为 `TData`。

### 3. 引用计数缓存管理

`ChunkDataMap<TKey, TData>` 通过 `_loadedChunkRefCount` 记录chunk被访问次数。

- `AddChunk` 加载chunk时会递增引用计数。
- `GetData` / `GetAllData` 访问chunk时也会继续增加引用计数。
- `TryClearUnused` 会保留访问最频繁的top 1/3 chunk，其它chunk会被释放。

这样可避免频繁访问的chunk被过早释放，同时对冷数据chunk进行内存回收。

---

## ⚙️ 关键接口说明

### `ChunkMaker` 常用方法

- `StreamWriteSync<TData, TKey>(string fileDirectory, string fileName, IEnumerable<TData> data, Func<TData, TKey> keySelector, int chunkSize, ChunkDataOptions options = null)`
  - 生成数据文件与索引文件。
  - `chunkSize` 是每个chunk最大记录数。
  - `options` 使用的二进制序列化方式和加密/解密设置。
- `StreamWriteSync<TData, TKey>(string fileDirectory, string fileName, IEnumerable<TData> data, Func<TData, TKey> keySelector, Func<TKey, int> keyToChunkIndex, ChunkDataOptions options = null)`
    - 按自定义 `chunkId` 分组生成数据文件与索引文件。

### `ChunkDataQueryer<TKey, TData>` 常用方法

- `Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, Func<TKey, int> keyToChunkIndex, ChunkDataOptions options = null)`
- `Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, ChunkDataOptions options = null)`
- `PrepareYieldInstruction(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector, int operationUnit = 512, ChunkDataOptions options = null)`
- `Get(TKey key, Action<TData> onAdd)`
- `GetByKey(Func<TKey, bool> keyPredicate, Action<TData> onAdd)`
- `GetAll()`
- `Find(Func<TData, bool> predicate)`
- `TryClearUnused()`
- `Clear()`

### `ChunkIndexer<TKey>` 常用方法

- `Init(IEnumerable<(int index, long offset, TKey[] keys)> chunks)`
- `InitAsync(IEnumerable<(int index, long offset, TKey[] keys)> chunks, int operationUnit = 512)`
- `Clear()`
- `GerChunkIndex(TKey key)`
- `GetChunkIndexByKey(Func<TKey, bool> keyPredicate)`
- `GetChunkOffset(int chunkIndex)`

### `ChunkReader` 常用方法

- `ReadChunkData<TData>(string filePath, long offset, ChunkDataOptions options = null)`
- `ReadIndexFile<TKey>(string filePath, ChunkDataOptions options = null)`

---

## ℹ️ 注意事项

1. `Prepare` 必须先执行，否则查询器无法知道chunk索引与数据偏移。
2. 如果使用自定义 `Serializer`，它不仅要能处理 `TData`，还必须能处理 `ChunkInfo` 和 `TKey[]`。
3. 默认加密由 `ConstSetting.FileEncryptionKey` 和 `EncryptUtils` 控制；如果不需要加密，请传入 `new ChunkDataOptions { EnableEncryption = false }` 或 `ChunkDataOptions.WithoutEncryption`。
4. 写入和读取必须使用相同的 `ChunkDataOptions`，尤其是自定义 `Serializer` / `Encryptor` 时。
5. `IChunkEncryptor.Decrypt(byte[] data, int offset, int count)` 的 `offset` 和 `count` 表示当前有效载荷区间，不能忽略。
6. `ChunkDataMap` 的 `GetData` 会在访问数据时增加引用次数，避免短期内频繁释放。
7. `Clear()` 会清空所有已加载chunk，并释放索引缓存。
8. 使用自定义 `chunkId` 写入时，读取端的 `keyToChunkIndex` 必须与写入时保持一致。
9. 自定义 `chunkId` 需要是非负整数；负值会被视为无效配置。

---

## ✅ 场景推荐

- 大型配置表分块加载
- 游戏数据分块存储与快速访问
- 运行时只读取部分数据，避免一次性全量加载
- 需要基于key快速定位chunk并按需加载的场景
