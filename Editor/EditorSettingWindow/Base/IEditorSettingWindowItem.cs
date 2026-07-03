using UnityEditor;

#if UNITY_EDITOR

namespace PowerCellStudio.Editor
{
    public interface IEditorSettingWindowItem
    {
        string itemName {get;}

        void InitSave();

        void OnDestroy();

        void OnGUI(EditorWindow window);

        void SaveData();
    }
}
#endif