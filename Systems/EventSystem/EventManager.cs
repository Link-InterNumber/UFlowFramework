using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [ModuleInitOrder(0)]
    public sealed partial class EventManager : SingletonBase<EventManager> , ILaterExecutionModule
    {
        #region 游戏逻辑

        public readonly LinkEvent onLoading = new LinkEvent();
        public readonly LinkEvent<bool> onPause = new LinkEvent<bool>();
        public readonly LinkEvent onQuit = new LinkEvent();
        public readonly LinkEvent<Vector2Int> onChangeResolution = new LinkEvent<Vector2Int>();
        public readonly LinkEvent<Vector2Int> onChangeScreen = new LinkEvent<Vector2Int>();

        public readonly LinkEvent onStartGame = new LinkEvent();
        public readonly LinkEvent onResetGame = new LinkEvent();
        public readonly LinkEvent onClearUnusedAsset = new LinkEvent();
        #endregion

        #region Ui事件
        public readonly LinkEvent<IUIParent> onPageOpen = new LinkEvent<IUIParent>();
        public readonly LinkEvent<IUIParent> onPageClose = new LinkEvent<IUIParent>();
        
        public readonly LinkEvent<IUIChild> onUIOpen = new LinkEvent<IUIChild>();
        public readonly LinkEvent<IUIChild> onUIClose = new LinkEvent<IUIChild>();
        
        public readonly LinkEvent<ScreenOrientation> onScreenOrientationChange = new LinkEvent<ScreenOrientation>();
        
        public readonly LinkEvent<bool> onUIInputEnable = new LinkEvent<bool>();

        #endregion

        #region 语言

        public readonly LinkEvent<Language> onLanguageChange = new LinkEvent<Language>();
        
        #endregion

        #region 时间缩放

        public readonly LinkEvent<float> onTimeScaleReplaced = new LinkEvent<float>();
        public readonly LinkEvent<bool> onTimeScalePause = new LinkEvent<bool>();
        
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