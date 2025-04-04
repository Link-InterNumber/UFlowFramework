using UnityEditor;

namespace PowerCellStudio
{
    public class DataPersistenceEditorTool
    {
        [MenuItem("Tools/DataPersistence/ClearPlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerDataUtils.ClearAllPlayerPrefs();
        }
        
        [MenuItem("Tools/DataPersistence/ClearJson")]
        public static void ClearJson()
        {
            PlayerDataUtils.ClearAllJson();
        }
        
        [MenuItem("Tools/DataPersistence/ClearBinary")]
        public static void ClearBinary()
        {
            PlayerDataUtils.ClearAllBinary();
        }
        
        [MenuItem("Tools/DataPersistence/DeleteAllCapture")]
        public static void DeleteAllCapture()
        {
            PlayerDataUtils.ClearCapture();
        }
        
        [MenuItem("Tools/DataPersistence/ClearAll")]
        public static void ClearAll()
        {
            PlayerDataUtils.ClearAll();
        }
    }
}