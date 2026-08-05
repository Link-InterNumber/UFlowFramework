using UnityEditor;

namespace PowerCellStudio.Editor
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    internal static class PlayModeAssetCleanup
    {
        static PlayModeAssetCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // 托管引用清理完成后，再卸载 Editor 原生资源缓存。
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    break;
                default:
                    break;
            }
        }
    }
#endif
}