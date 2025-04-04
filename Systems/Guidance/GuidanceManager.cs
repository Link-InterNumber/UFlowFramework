using System.Collections.Generic;

namespace PowerCellStudio
{
    public class GuidanceManager : SingletonBase<GuidanceManager>, IOnGameStartModule, IOnGameResetModule
    {
        private Dictionary<int, GuidanceTag> _guidanceTags;
        private HashSet<int> _onIndex;
        private HashSet<int> _executedIndex;
        private int _currentIndex;

        public int currentIndex => _currentIndex;
        public bool inGuidance => _currentIndex > 0;

        public class GuidanceSave : IPersistenceData
        {
            public List<int> executedIndex = new List<int>();
        }

        public void OnInit()
        {
            _guidanceTags = new Dictionary<int, GuidanceTag>();
            _onIndex = new HashSet<int>();
            _executedIndex = new HashSet<int>();
            LoadExecutedIndex();
        }

        public void OnGameReset()
        {
            _guidanceTags.Clear();// = new Dictionary<int, GuidanceTag>();
            _onIndex.Clear();// = new HashSet<int>();
            _executedIndex.Clear();// = new HashSet<int>();
        }

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

        public bool IsGuidancePlayed(int index)
        {
            return _executedIndex.Contains(index);
        }
        
        public GuidanceTag FindGuidanceByIndex(int index)
        {
            _guidanceTags.TryGetValue(index, out var go);
            return go;
        }

        public void RegisterGuidance(GuidanceTag guidanceObject)
        {
            if(!guidanceObject || IsGuidancePlayed(guidanceObject.guidanceIndex)) return;
            _guidanceTags[guidanceObject.guidanceIndex] = guidanceObject;
            if (_onIndex.Contains(guidanceObject.guidanceIndex)) ActiveGuidanceWhichOn();
        }

        public void DeregisterGuidance(int index)
        {
            _guidanceTags?.Remove(index);
        }

        public void SetGuidanceOn(int index)
        {
            _onIndex.Add(index);
            ActiveGuidanceWhichOn();
        }
        
        public void SetGuidanceOff(int index)
        {
            _onIndex.Remove(index);
        }

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
        
        public void DeExecuteGuidance(int guidanceIndex)
        {
            _executedIndex.Add(guidanceIndex);
            if (_guidanceTags.TryGetValue(guidanceIndex, out var guidanceTag))
            {
                guidanceTag.OnDeExecute();
            }
            // 获取配置表，查看是否有下一个引导
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