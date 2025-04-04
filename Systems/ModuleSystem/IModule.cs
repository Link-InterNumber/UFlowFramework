using System;

namespace PowerCellStudio
{
    public interface IModule : IDisposable
    {
        /// <summary>
        /// 自动在模块首次调用时初始化
        /// </summary>
        public void OnInit();
    }

    public interface IOnGameStartModule : IModule
    {
        /// <summary>
        /// 自动在游戏开始前初始化
        /// </summary>
        public void OnGameStart();
    }
    
    public interface IOnGameResetModule : IModule
    {
        /// <summary>
        /// 自动在游戏返回标题界面调用时
        /// </summary>
        public void OnGameReset();
    }
    
    public interface IExecutionModule : IModule
    {
        public bool inExecution { set; get; }
        /// <summary>
        /// 在Unity的Update中执行
        /// </summary>
        public void Execute(float dt);
    }
    
    public interface ILaterExecutionModule : IModule
    {
        public bool inExecution { set; get; }
        /// <summary>
        /// 在Unity的LaterUpdate中执行
        /// </summary>
        public void LaterExecute(float dt);
    }
    
    public interface IEventModule : IModule
    {
        /// <summary>
        /// 事件注册
        /// </summary>
        public void RegisterEvent();
        /// <summary>
        /// 事件注销
        /// </summary>
        public void UnRegisterEvent();
    }
}