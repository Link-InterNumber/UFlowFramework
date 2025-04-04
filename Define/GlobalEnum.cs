namespace PowerCellStudio
{
    /// <summary>
    /// 分辨率级别
    /// </summary>
    public enum ResolutionLv
    {
        Low = 0,
        Mid,
        High,
        OneK,
        TwoK,
    }
    
    /// <summary>
    /// 运行状态
    /// </summary>
    public enum ApplicationState
    {
        Loading,
        Playing,
        Pause,
        Quit
    }

    /// <summary>
    /// 资源类型
    /// </summary>
    public enum AssetType
    {
        Prefab,
        Sprite,
        Audio,
        Material
    }

    /// <summary>
    /// 播放类型
    /// </summary>
    public enum AudioSourceType
    {
        Music,
        Ambience,
        Effect3D,
        Dialog,
        UIEffect
    }
    
    /// <summary>
    /// 二维方向
    /// </summary>
    public enum Direction2D
    {
        Left,
        Right,
        Up,
        Down
    }
    
    /// <summary>
    /// 三维方向
    /// </summary>
    public enum Direction3D
    {
        Forward,
        Backward,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// 二维移动方向
    /// </summary>
    public enum MoveDir2D
    {
        Vertical,
        Horizontal,
        Both,
        None
    }

    /// <summary>
    /// 语言
    /// </summary>
    public enum Language
    {
        ChineseSimplified = 0,
        ChineseTraditional,
        English,
        // Japanese
    }
    
    /// <summary>
    /// 资源模块初始化状态
    /// </summary>
    public enum AssetInitState
    {
        InitModule,
        CheckForResourceUpdates,
        DownloadTheUpdateFile,
        Complete
    }
    
    /// <summary>
    /// 资源加载状态
    /// </summary>
    public enum AssetLoadStatus
    {
        Unload,
        Loading,
        Loaded,
    }
}