using System.Collections.Generic;

namespace PowerCellStudio
{
    /// <summary>
    /// 引导管理器，用于管理游戏中的引导流程。
    /// Guidance manager for managing guidance processes within the game.
    /// </summary>
    public class GuidanceManager : SingletonBase<GuidanceManager>, IOnGameStartModule, IOnGameResetModule
    {
        private Dictionary<int, GuidanceTag> _guidanceTags;
        private HashSet<int> _onIndex;
        private HashSet<int> _executedIndex;
        private int _currentIndex;

        /// <summary>
        /// 当前引导索引。
        /// Current index of the guidance.
        /// </summary>
        public int currentIndex => _currentIndex;

        /// <summary>
        /// 是否处于引导状态。
        /// Indicates whether currently in guidance.
        /// </summary>
        public bool inGuidance => _currentIndex > 0;

        /// <summary>
        /// 用于持久化存储已执行引导索引。
        /// Persistent data for storing executed guidance indices.
        /// </summary>
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
            _guidanceTags = new Dictionary<int, GuidanceTag>();
            _onIndex = new HashSet<int>();
            _executedIndex = new HashSet<int>();
            LoadExecutedIndex();
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
        }

        /// <summary>
        /// 游戏开始时模块初始化。
        /// Module initialization when the game starts.
        /// </summary>
        public void OnGameStart()
        {
            ModuleLog<GuidanceManager>.Log("Module Init!");
        }

        private void LoadExecutedIndex()
        {
            var save = PlayerDataUtils.ReadPlayerPrefs<GuidanceSave>();
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
            PlayerDataUtils.SavePlayerPrefs(save);
        }

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
            if(!guidanceObject || IsGuidancePlayed(guidanceObject.guidanceIndex)) return;
            _guidanceTags[guidanceObject.guidanceIndex] = guidanceObject;
            if (_onIndex.Contains(guidanceObject.guidanceIndex)) ActiveGuidanceWhichOn();
        }

        /// <summary>
        /// 注销引导对象。
        /// Deregister a guidance object.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void DeregisterGuidance(int index)
        {
            _guidanceTags?.Remove(index);
        }

        /// <summary>
        /// 打开指定索引的引导。
        /// Set guidance at the specified index to active.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void SetGuidanceOn(int index)
        {
            _onIndex.Add(index);
            ActiveGuidanceWhichOn();
        }
        
        /// <summary>
        /// 关闭指定索引的引导。
        /// Set guidance at the specified index to inactive.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void SetGuidanceOff(int index)
        {
            _onIndex.Remove(index);
        }

        /// <summary>
        /// 重新激活指定索引的引导。
        /// Reactivate guidance at the specified index.
        /// </summary>
        /// <param name="index">引导索引 / Guidance index</param>
        public void ReactiveGuidance(int index)
        {
            _executedIndex.Remove(index);
            _onIndex.Add(index);
        }

        private void ActiveGuidanceWhichOn()
        {
            var executeIndex = 0;
            foreach (var i in _onIndex)
            {
                if (!_guidanceTags.TryGetValue(i, out var guidanceTag)) return;
                executeIndex = i;
                _currentIndex = i;
                guidanceTag.OnExecute();
                ExecuteGuidance(guidanceTag);
                break;
            }
            _onIndex.Remove(executeIndex);
            _currentIndex = executeIndex;
        }
        
        /// <summary>
        /// 执行指定的引导。
        /// Execute the specified guidance.
        /// </summary>
        /// <param name="tag">引导标签 / Guidance tag</param>
        public void ExecuteGuidance(GuidanceTag tag)
        {
            if(!tag)
            {
                ModuleLog<GuidanceManager>.LogError($"Guidance tag was destroy");
                DeExecuteGuidance(0);
                return;
            }
            var conf = ConfigManager.instance.guidanceConf.Get(tag.guidanceIndex);
            if (conf == null)
            {
                ModuleLog<GuidanceManager>.LogError($"Guidance index is not exist, index = {tag.guidanceIndex}");
                DeExecuteGuidance(tag.guidanceIndex);
                return;
            }
            UIManager.instance.OpenWindow<GuidanceWindow>(new GuidanceWindow.Info
            {
                conf = conf,
                tag = tag
            });
        }
        
        /// <summary>
        /// 取消执行引导，并查看是否有后续引导流程。
        /// Deactivate guidance and check if there is a subsequent guidance process.
        /// </summary>
        /// <param name="guidanceIndex">引导索引 / Guidance index</param>
        public void DeExecuteGuidance(int guidanceIndex)
        {
            _executedIndex.Add(guidanceIndex);
            if (_guidanceTags.TryGetValue(guidanceIndex, out var guidanceTag))
            {
                guidanceTag.OnDeExecute();
            }
            var conf = ConfigManager.instance.guidanceConf.Get(guidanceIndex);
            if(conf.nextGuidance > 0)
            {
                SetGuidanceOn(conf.nextGuidance);
                ActiveGuidanceWhichOn();
                return;
            }
            UIManager.instance.CloseWindow<GuidanceWindow>();
            _currentIndex = 0;
            SaveExecutedIndex();
        }
    }
}