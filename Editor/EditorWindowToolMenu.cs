using UnityEditor;

namespace PowerCellStudio.Editor
{
    public static class EditorWindowToolMenu
    {
        private const string _CONFIG_MENU_PATH_CREAT = "Tools/Config/Create Cs Files";
        private const int _CONFIG_MENU_PRIORITY_CREATE = 100;

        [MenuItem(_CONFIG_MENU_PATH_CREAT, false, _CONFIG_MENU_PRIORITY_CREATE)]
        public static void CreateCsFiles()
        {
            ConfigMenu.CreateCsFiles();
        }

        private const string _ACT_MENU_PATH_OPEN = "Tools/UFlow/Act/Act Editor";
        private const int _ACT_MENU_PRIORITY_OPEN = 900;
        
        [MenuItem(_ACT_MENU_PATH_OPEN, false, _ACT_MENU_PRIORITY_OPEN)]
        public static void OpenActEditor()
        {
            ActEditorWindow.Open();
        }
        
    }
}