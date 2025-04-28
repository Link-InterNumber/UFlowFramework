#if UNITY_EDITOR

namespace PowerCellStudio
{
    public interface IEditorSettingWindowItem
    {
        string itemName {get;}

        void InitSave();

        void OnDestroy();

        void OnGUI();

        void SaveData();
    }
}
#endif