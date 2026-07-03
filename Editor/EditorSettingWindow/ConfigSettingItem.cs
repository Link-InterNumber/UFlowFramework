#if UNITY_EDITOR
using UnityEditor;

namespace PowerCellStudio.Editor
{
    public class ConfigSettingItem: IEditorSettingWindowItem
    {
        public string itemName => "Excel To Config";

        private readonly ConfigSettingLogic _logic = new ConfigSettingLogic();

        public void InitSave()
        {
            _logic.Initialize();
        }

        public void OnDestroy()
        {
            _logic.Dispose();
        }
        
        public void OnGUI(EditorWindow window)
        {
            _logic.OnGUI();
        }

        public void SaveData()
        {
            _logic.SaveData();
        }
    }
}
#endif