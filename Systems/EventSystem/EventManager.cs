using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    public sealed partial class EventManager : SingletonBase<EventManager>, ILaterExecutionModule
    {
        #region 游戏逻辑

        /// <summary>
        /// 调用ApplicationManager.instance.SetLoading(true)时触发
        /// Invoke in ApplicationManager.instance.SetLoading(true)
        /// </summary>
        public readonly LinkEvent onLoading = new LinkEvent();

        /// <summary>
        /// 应用程序暂停或恢复时触发（参数为是否暂停）  
        /// Pause event (parameter indicates pause state)
        /// </summary>
        public readonly LinkEvent<bool> onPause = new LinkEvent<bool>();

        /// <summary>
        /// 应用程序退出时触发  
        /// Quit event
        /// </summary>
        public readonly LinkEvent onQuit = new LinkEvent();

        /// <summary>
        /// 分辨率变化事件  
        /// Resolution change event
        /// </summary>
        public readonly LinkEvent<Vector2Int> onChangeResolution = new LinkEvent<Vector2Int>();

        /// <summary>
        /// UI屏幕大小改变时触发  
        /// Invoke when the size of UI screen changing
        /// </summary>
        public readonly LinkEvent<Vector2Int> onChangeScreen = new LinkEvent<Vector2Int>();

        /// <summary>
        /// 开始游戏事件  
        /// Start game event
        /// </summary>
        public readonly LinkEvent onStartGame = new LinkEvent();

        /// <summary>
        /// 重置游戏事件  
        /// Reset game event
        /// </summary>
        public readonly LinkEvent onResetGame = new LinkEvent();

        /// <summary>
        /// 清理未使用资源事件  
        /// Clear unused asset event
        /// </summary>
        public readonly LinkEvent onClearUnusedAsset = new LinkEvent();
        #endregion

        #region Ui事件

        /// <summary>
        /// UI page打开事件  
        /// UI page open event
        /// </summary>
        public readonly LinkEvent<IUIParent> onPageOpen = new LinkEvent<IUIParent>();

        /// <summary>
        /// UI page关闭事件  
        /// UI page close event
        /// </summary>
        public readonly LinkEvent<IUIParent> onPageClose = new LinkEvent<IUIParent>();

        /// <summary>
        /// UI window 打开事件  
        /// UI window open event
        /// </summary>
        public readonly LinkEvent<IUIChild> onUIOpen = new LinkEvent<IUIChild>();

        /// <summary>
        /// UI window 关闭事件  
        /// UI window close event
        /// </summary>
        public readonly LinkEvent<IUIChild> onUIClose = new LinkEvent<IUIChild>();

        /// <summary>
        /// 屏幕方向变化事件  
        /// Screen orientation change event
        /// </summary>
        public readonly LinkEvent<ScreenOrientation> onScreenOrientationChange = new LinkEvent<ScreenOrientation>();

        /// <summary>
        /// UI输入使能事件  
        /// UI input enable event
        /// </summary>
        public readonly LinkEvent<bool> onUIInputEnable = new LinkEvent<bool>();

        #endregion

        #region 引导

        public LinkEvent<int> onGuidanceStart = new LinkEvent<int>();
        public LinkEvent<int, int> onGuidanceEnd = new LinkEvent<int, int>();

        #endregion

        #region 语言

        /// <summary>
        /// 语言切换事件  
        /// Language change event
        /// </summary>
        public readonly LinkEvent<Language> onLanguageChange = new LinkEvent<Language>();

        #endregion

        #region 时间缩放

        /// <summary>
        /// 时间缩放替换事件  
        /// Time scale replaced event
        /// </summary>
        public readonly LinkEvent<float> onTimeScaleReplaced = new LinkEvent<float>();

        /// <summary>
        /// 时间缩放暂停事件  
        /// Time scale pause event
        /// </summary>
        public readonly LinkEvent<bool> onTimeScalePause = new LinkEvent<bool>();

        #endregion

        #region 网络

        /// <summary>
        /// 网络连接事件，重连时也会触发
        /// Network connect event，invoke when connect or reconnect;
        /// </summary>
        public readonly LinkEvent onNetConnect = new LinkEvent();

        /// <summary>
        /// 网络断开事件  
        /// Network disconnect event
        /// </summary>
        public readonly LinkEvent onNetDisconnect = new LinkEvent();

        #endregion

        #region 业务逻辑

        // TODO write here

        #endregion

        #region LatereEvents

        private HashSet<IInvolke> _latereEvents = new HashSet<IInvolke>();

        public bool inExecution { get; set; }

        private HashSet<IInvolke> _executedEvents = new HashSet<IInvolke>();

        public void OnInit()
        {
            _latereEvents = new HashSet<IInvolke>();
        }

        public void OnDeinit()
        {
            _latereEvents.Clear();
        }

        public void LaterExecute(float dt)
        {
            if (!inExecution || _latereEvents.Count == 0) return;
            foreach (var laterEvent in _latereEvents)
            {
                _executedEvents.Add(laterEvent);
            }
            _latereEvents.Clear();
            foreach (var laterEvent in _executedEvents)
            {
                laterEvent.Invoke();
            }
            _executedEvents.Clear();
        }

        public void InvokeLaterEvent(IInvolke @event)
        {
            _latereEvents.Add(@event);
        }

        #endregion
    }
}