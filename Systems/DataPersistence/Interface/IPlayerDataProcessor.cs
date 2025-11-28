using System;

/// <summary>
/// 数据处理器接口，用于定义玩家数据的基本操作。
/// Interface for processing player data, defining basic operations.
/// </summary>
public interface IPlayerDataProcessor
{
    /// <summary>
    /// 获取保存数据的目录名称。
    /// Gets the directory name for saving data.
    /// </summary>
    public string directoryName { get; }

    /// <summary>
    /// 获取保存数据的文件扩展名。
    /// Gets the file extension for saving data.
    /// </summary>
    public string extension { get; }

    /// <summary>
    /// 检查是否存在指定键的保存数据。
    /// Checks if there is saved data for the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <returns>如果存在保存数据，则返回 true；否则返回 false。Returns true if the saved data exists; otherwise, false.</returns>
    public bool HasSave(string saveKey);

    /// <summary>
    /// 清除指定键的保存数据。
    /// Clears the saved data for the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    public void Clear(string saveKey);

    /// <summary>
    /// 清除所有保存数据。
    /// Clears all saved data.
    /// </summary>
    public void ClearAll();
}

/// <summary>
/// 数据保存器接口，用于定义保存玩家数据的操作。
/// Interface for saving player data, defining save operations.
/// </summary>
public interface IPlayerDataSaver
{
    /// <summary>
    /// 保存数据到指定键。
    /// Saves data to the specified key.
    /// </summary>
    /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="data">要保存的数据。The data to save.</param>
    /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
    /// <returns>如果保存成功，则返回 true；否则返回 false。Returns true if the data is saved successfully; otherwise, false.</returns>
    public bool Save<T>(string saveKey, T data, bool encrypt);

    /// <summary>
    /// 异步保存数据到指定键。
    /// Asynchronously saves data to the specified key.
    /// </summary>
    /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="data">要保存的数据。The data to save.</param>
    /// <param name="onComplete">保存完成时的回调。The callback to invoke when the save operation is complete.</param>
    /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
    public void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt);
}

/// <summary>
/// 泛型数据保存器接口，用于定义保存特定类型玩家数据的操作。
/// Generic interface for saving specific type of player data.
/// </summary>
/// <typeparam name="T">数据的类型。The type of the data.</typeparam>
public interface IPlayerDataSaver<T>
{
    /// <summary>
    /// 保存数据到指定键。
    /// Saves data to the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="data">要保存的数据。The data to save.</param>
    /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
    /// <returns>如果保存成功，则返回 true；否则返回 false。Returns true if the data is saved successfully; otherwise, false.</returns>
    public bool Save(string saveKey, T data, bool encrypt);

    /// <summary>
    /// 异步保存数据到指定键。
    /// Asynchronously saves data to the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="data">要保存的数据。The data to save.</param>
    /// <param name="onComplete">保存完成时的回调。The callback to invoke when the save operation is complete.</param>
    /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
    public void SaveAsync(string saveKey, T data, Action<bool> onComplete, bool encrypt);
}

/// <summary>
/// 数据读取器接口，用于定义读取玩家数据的操作。
/// Interface for reading player data, defining read operations.
/// </summary>
public interface IPlayerDataReader
{
    /// <summary>
    /// 从指定键读取数据。
    /// Reads data from the specified key.
    /// </summary>
    /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
    /// <returns>读取的数据。The data that was read.</returns>
    public T Read<T>(string saveKey, bool decrypt);

    /// <summary>
    /// 异步从指定键读取数据。
    /// Asynchronously reads data from the specified key.
    /// </summary>
    /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="onComplete">读取完成时的回调。The callback to invoke when the read operation is complete.</param>
    /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
    public void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt);
}

/// <summary>
/// 泛型数据读取器接口，用于定义读取特定类型玩家数据的操作。
/// Generic interface for reading specific type of player data.
/// </summary>
/// <typeparam name="T">数据的类型。The type of the data.</typeparam>
public interface IPlayerDataReader<T>
{
    /// <summary>
    /// 从指定键读取数据。
    /// Reads data from the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
    /// <returns>读取的数据。The data that was read.</returns>
    public T Read(string saveKey, bool decrypt);

    /// <summary>
    /// 异步从指定键读取数据。
    /// Asynchronously reads data from the specified key.
    /// </summary>
    /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
    /// <param name="onComplete">读取完成时的回调。The callback to invoke when the read operation is complete.</param>
    /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
    public void ReadAsync(string saveKey, Action<T> onComplete, bool decrypt);
}