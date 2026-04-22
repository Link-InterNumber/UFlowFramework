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

组件依赖关系
```
ChunkDataQueryer<TKey, TData>
        |
        |──> ChunkIndexer<TKey>-----------------> ChunkReader
        |
        └──> ChunkDataMap<TKey, TData>----------> ChunkReader

ChunkMaker ----------> ChunkInfo

ChunkReader ---------> ChunkInfo
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
    deEncrypt: true);
```

会生成两个文件：

- `MyConfigData.bytes`：真实数据文件，按chunk顺序写入数据记录。
- `MyConfigIndex.bytes`：chunk索引文件，保存chunk偏移与对应键集合。

### 2.🔍 **准备查询器**

在运行时使用 `ChunkDataQueryer<TKey, TData>` 加载索引并准备缓存结构。

```csharp
var queryer = new ChunkDataQueryer<int, MyData>();
queryer.Prepare(
    indexFilePath: "Assets/Res/DataChunk/MyConfigIndex.bytes",
    dataFilePath: "Assets/Res/DataChunk/MyConfigData.bytes",
    keySelector: data => data.id);
```

如果需要在协程中异步初始化索引，可使用：

```csharp
yield return queryer.PrepareYieldInstruction(
    indexFilePath: indexPath,
    dataFilePath: dataPath,
    keySelector: data => data.id);
```

> 注意：`Prepare` / `PrepareYieldInstruction` 只初始化索引和缓存结构，不会加载具体数据chunk。

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

- `StreamWriteSync<TData, TKey>(string fileDirectory, string fileName, IEnumerable<TData> data, Func<TData, TKey> keySelector, int chunkSize, bool deEncrypt = true)`
  - 生成数据文件与索引文件。
  - `chunkSize` 是每个chunk最大记录数。
  - `deEncrypt` 决定是否启用AES加密写入。

### `ChunkDataQueryer<TKey, TData>` 常用方法

- `Prepare(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector)`
- `PrepareYieldInstruction(string indexFilePath, string dataFilePath, Func<TData, TKey> keySelector)`
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

- `ReadChunkData<TData>(string filePath, long offset, bool deEncrypt = true)`
- `ReadIndexFile<TKey>(string filePath, bool deEncrypt = true)`

---

## ℹ️ 注意事项

1. `Prepare` 必须先执行，否则查询器无法知道chunk索引与数据偏移。
2. 如果数据源不是 `TData` 的默认可序列化类型，请确保 `SerializeUtils` 支持对应类型的序列化与反序列化。
3. 默认加密由 `ConstSetting.FileEncryptionKey` 和 `EncryptUtils` 控制；如果不需要加密，写入和读取时都应传入 `deEncrypt: false`。
4. `ChunkDataMap` 的 `GetData` 会在访问数据时增加引用次数，避免短期内频繁释放。
5. `Clear()` 会清空所有已加载chunk，并释放索引缓存。

---

## ✅ 场景推荐

- 大型配置表分块加载
- 游戏数据分块存储与快速访问
- 运行时只读取部分数据，避免一次性全量加载
- 需要基于key快速定位chunk并按需加载的场景
