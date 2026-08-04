using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
   
    /// <summary>
    /// 引导管理器，用于管理游戏中的引导流程。
    /// Guidance manager for managing guidance processes within the game.
    /// </summary>
    public class GuidanceManager : SingletonBase<GuidanceManager>, IOnGameStartModule, IOnGameResetModule
    {
        public LinkEvent<int> onGuidanceStart = new LinkEvent<int>();
        public LinkEvent<int, int> onGuidanceEnd = new LinkEvent<int, int>();

        private Dictionary<int, GuidanceTag> _guidanceTags;
        private HashSet<int> _onIndex;
        private HashSet<int> _executedIndex;
        private List<int> _currentIndex;
        private Func<int, IGuidanceConfig> _confProvider;

        /// <summary>
        /// 当前引导索引。
        /// Current index of the guidance.
        /// </summary>
        public List<int> currentIndex => _currentIndex;

        private int _nextIndex;

        /// <summary>
        /// 是否处于引导状态。
        /// Indicates whether currently in guidance.
        /// </summary>
        public bool inGuidance => _currentIndex.Count > 0;

        /// <summary>
        /// 用于持久化存储已执行引导索引。
        /// Persistent data for storing executed guidance indices.
        /// </summary>
        [Serializable]
        public class GuidanceSave : IPersistenceData
        {
            public List<int> executedIndex = new List<int>();
        }

        /// <summary>
        /// 初始化引导管理器，载入已经执行的引导索引。
        /// Initializes the guidance manager and loads executed guidance indices.
        /// </summary>
        public void OnInit()
        {
            _currentIndex = new List<int>();
            _guidanceTags = new Dictionary<int, GuidanceTag>();
            _onIndex = new HashSet<int>();
            _executedIndex = new HashSet<int>();
            LoadExecutedIndex();
        }

        protected override void Deinit()
        {
            OnGameReset();
            _guidanceTags = null;
            _onIndex = null;
            _executedIndex = null;
            _currentIndex = null;
        }

        /// <summary>
        /// 重置游戏时调用以清除引导相关数据。
        /// Called to clear guidance data when the game is reset.
        /// </summary>
        public void OnGameReset()
        {
            _guidanceTags.Clear();
            _onIndex.Clear();
            _executedIndex.Clear();
            _currentIndex.Clear();
        }

        /// <summary>
        /// 游戏开始时模块初始化。
        /// Module initialization when the game starts.
        /// </summary>
        public void OnGameStart()
        {
            ModuleLogger.Log<GuidanceManager>("Module Init!");
        }

        private void LoadExecutedIndex()
        {
            var save = PlayerDataUtils.Read<GuidanceSave>(PlayerDataType.PlayerPrefs);
            if (save == null || save.executedIndex == null || save.executedIndex.Count == 0) return;
            for (var i = 0; i < save.executedIndex.Count; i++)
            {
                _executedIndex.Add(save.executedIndex[i]);
            }
        }

        private void SaveExecutedIndex()
        {
            var save = new GuidanceSave();
            foreach (var i in _executedIndex)
            {
                save.executedIndex.Add(i);
            }
            PlayerDataUtils.Save(save, PlayerDataType.PlayerPrefs);
        }

        public void SetConfigProvider(Func<int, IGuidanceConfig> fun)
        {
            _confProvider = fun;
        }

        public IGuidanceConfig GetConf(int id) => _confProvider?.Invoke(id);

        /// <summary>
        /// 判断指定索引的引导是否已执行。
        /// Check if guidance at the specified index has been executed.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        /// <returns>是否已执行 / Whether it has been executed</returns>
        public bool IsGuidancePlayed(int index)
        {
            return _executedIndex.Contains(index);
        }
        
        /// <summary>
        /// 根据索引查找引导对象。
        /// Find a guidance object by index.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        /// <returns>引导对象 / Guidance object</returns>
        public GuidanceTag FindGuidanceByIndex(int index)
        {
            _guidanceTags.TryGetValue(index, out var go);
            return go;
        }

        /// <summary>
        /// 注册引导对象。
        /// Register a guidance object.
        /// </summary>
        /// <param name="guidanceObject">引导对象 / Guidance object</param>
        public void RegisterGuidance(GuidanceTag guidanceObject)
        {
            if(!guidanceObject) return;
            _guidanceTags[guidanceObject.guidanceIndex] = guidanceObject;
            if (_onIndex.Contains(guidanceObject.guidanceIndex)) ActiveGuidanceWhichOn(_nextIndex == guidanceObject.guidanceIndex);
        }

        /// <summary>
        /// 注销引导对象。
        /// Deregister a guidance object.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void DeregisterGuidance(int index)
        {
            if (_currentIndex.Contains(index)) DeExecuteGuidance(index);
            _guidanceTags?.Remove(index);
        }

        /// <summary>
        /// 打开指定索引的引导。
        /// Set guidance at the specified index to active.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        /// <returns>是否正在打开引导界面 / Whether the guidance window is going to open</returns>
        public bool SetGuidanceOn(int index)
        {
            if (IsGuidancePlayed(index)) return false;
            _onIndex.Add(index);
            return ActiveGuidanceWhichOn(true);
        }
        
        /// <summary>
        /// 关闭指定索引的引导。
        /// Set guidance at the specified index to inactive.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void SetGuidanceOff(int index)
        {
            if (_currentIndex.Contains(index)) DeExecuteGuidance(index);
            _onIndex.Remove(index);
        }

        /// <summary>
        /// 重新激活指定索引的引导。
        /// Reactivate guidance at the specified index.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        /// <returns>是否正在打开引导界面 / Whether the guidance window is going to open</returns>
        public bool ReactiveGuidance(int index)
        {
            _executedIndex.Remove(index);
            return SetGuidanceOn(index);
        }

        private bool ActiveGuidanceWhichOn(bool force)
        {
            if (!force && inGuidance) return false;
            if (_onIndex.Count == 0) return false;
            var executeIndex = 0;
            foreach (var i in _onIndex)
            {
                if (!_guidanceTags.TryGetValue(i, out var guidanceTag)) continue;
                executeIndex = i;
                guidanceTag.OnExecute();
                ExecuteGuidance(guidanceTag);
                break;
            }
            _onIndex.Remove(executeIndex);
            if (executeIndex > 0 ) _currentIndex.Add(executeIndex);
            return executeIndex > 0;
        }
        
        /// <summary>
        /// 执行指定的引导。
        /// Execute the specified guidance.
        /// </summary>
        /// <param name="tag">引导标签 / Guidance tag</param>
        private void ExecuteGuidance(GuidanceTag tag)
        {
            if(!tag)
            {
                ModuleLogger.LogError<GuidanceManager>($"Guidance tag was destroy");
                DeExecuteGuidance(0);
                return;
            }
            var conf = _confProvider?.Invoke(tag.guidanceIndex);
            if (conf == null)
            {
                ModuleLogger.LogError<GuidanceManager>($"Guidance index is not exist, index = {tag.guidanceIndex}");
                DeExecuteGuidance(tag.guidanceIndex);
                return;
            }
            UIManager.instance.OpenWindow<GuidanceWindow>(new GuidanceWindow.Info
            {
                conf = conf,
                tag = tag
            });
            onGuidanceStart?.Invoke(conf.id);
        }
        
        /// <summary>
        /// 取消执行引导，并查看是否有后续引导流程。
        /// Deactivate guidance and check if there is a subsequent guidance process.
        /// </summary>
        /// <param name="guidanceIndex">引导索引 / Guidance index</param>
        public void DeExecuteGuidance(int guidanceIndex)
        {
            if (_guidanceTags.TryGetValue(guidanceIndex, out var guidanceTag))
            {
                guidanceTag?.OnDeExecute();
            }
            var conf = _confProvider?.Invoke(guidanceIndex);
            if (conf != null && conf.nextGuidance > 0)
            {
                _nextIndex = conf.nextGuidance;
                onGuidanceEnd?.Invoke(guidanceIndex, _nextIndex);
                var hasNewGuidance = ReactiveGuidance(conf.nextGuidance);
                if (!hasNewGuidance) 
                    UIManager.instance.CloseWindow<GuidanceWindow>();
                return; 
            }

            _nextIndex = 0;
            for (var i = 0; i < _currentIndex.Count; i++)
            {
                _executedIndex.Add(_currentIndex[i]);
            }
            _currentIndex.Clear();
            UIManager.instance.CloseWindow<GuidanceWindow>();
            SaveExecutedIndex();
            onGuidanceEnd?.Invoke(guidanceIndex, _nextIndex);
        }
    }
}