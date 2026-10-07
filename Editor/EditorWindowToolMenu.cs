using UnityEditor;

namespace PowerCellStudio.Editor
{
    public static class EditorWindowToolMenu
    {
        #region 配置工具

        [MenuItem("Tools/Config/Config Setting Window", false, 100)]
        public static void OpenConfigSettingWindow() => ConfigSettingWindow.OpenEditorSettingWindow();

        [MenuItem("Tools/Config/Create Cs Files", false, 101)]
        public static void CreateCsFiles() => ConfigMenu.CreateCsFiles();

        [MenuItem("Tools/Config/Create Config Assets", false, 102)]
        public static void CreateConfigAssets() => ConfigMenu.CreateConfigAsset();

        [MenuItem("Tools/Config/Create Config Assets By Force", false, 103)]
        public static void CreateConfigAssetsByForce() => ConfigMenu.CreateConfigAssetByForce();

        [MenuItem("Tools/Config/Delete Config Assets", false, 104)]
        public static void DeleteConfigAssets() => ConfigMenu.DeleteConfigAsset();

        [MenuItem("Tools/Config/Create Localization csv", false, 105)]
        public static void CreateLocalizationCsv() => UnityLocalizationCsvExporter.Export();

        #endregion

        #region UFlow 编辑器工具

        [MenuItem("Tools/UFlow/Act/Act Editor", false, 200)]
        public static void OpenActEditor() => ActEditorWindow.Open();

        [MenuItem("Tools/UFlow/Editor Setting Window", false, 201)]
        public static void OpenEditorSettingWindow() => EditorSettingWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Assets/Runtime Loaded Assets", false, 202)]
        public static void OpenRuntimeLoadedAssetViewer() => RuntimeLoadedAssetViewerWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Guidance/Editor Graph", false, 203)]
        public static void OpenGuidanceGraph() => GuidanceGraphWindow.OpenWindow();

        [MenuItem("Tools/UFlow/Notify/Editor Graph", false, 204)]
        public static void OpenNotifyGraph() => NotifyGraphWindow.OpenWindow();

        [MenuItem("Tools/UFlow/Notify/TreeView Window", false, 205)]
        public static void OpenNotifyTreeView() => NotifyTreeViewWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Texture/Batch Resize and Save Images", false, 206)]
        public static void OpenTextureBatchResizer() => TextureBatchResizer.ShowWindow();

        [MenuItem("Tools/UFlow/Texture/设置图片压缩格式（整个文件夹）", false, 207)]
        public static void OpenTextureFormatSetter() => TextureFormatSetter.SetTextureFormat();

        [MenuItem("Tools/UFlow/Mesh/Smooth Normals for Outline", false, 208)]
        public static void OpenSmoothNormalsProcessor() => SmoothNormalsProcessor.Init();

        [MenuItem("Tools/UFlow/Mesh/Generate Thickness Map", false, 209)]
        public static void OpenThicknessMapGenerator() => ThicknessMapGenerator.ShowWindow();

        #endregion

        #region Bundle 分析工具

        [MenuItem("Tools/UFlow/Bundle Analysis/Bundle Reference", false, 300)]
        public static void OpenBundleReferenceViewer() => BundleReferenceViewerWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Bundle Analysis/Create Analysis File", false, 301)]
        public static void CreateBundleReferenceAnalysis() => BundleReferenceExporter.BundleReferenceExporterHandler();

        [MenuItem("Tools/UFlow/Bundle Analysis/Read Analysis File", false, 302)]
        public static void ReadBundleReferenceAnalysis() => BundleReferenceTextViewerWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Bundle Analysis/Builted Bundle Reference Compare", false, 303)]
        public static void CompareBuiltBundleReference() => BundleReferenceCompareWindow.ShowWindow();

        [MenuItem("Tools/UFlow/Bundle Analysis/查找资源bundle（整个文件夹）", false, 304)]
        public static void FindBundleInFolder() => FindBundleWindow.SetTextureFormat();

        #endregion

        #region Addressable 构建工具

        [MenuItem("Build/Addressable/Folder Addressable Settings", false, 400)]
        public static void OpenFolderAddressableSettings() => FolderAddressableGroupEditorWindow.ShowWindow();

        [MenuItem("Build/Addressable/Build Addressable Bundle only", false, 401)]
        public static void BuildAddressableBundle() => AddressableBuilder.BuildAddressables();

        [MenuItem("Build/Addressable/Default Build", false, 402)]
        public static void DefaultBuild() => PlayerBuilder.DefaultPlayerBuilder();

        [MenuItem("Build/Addressable/Window Build", false, 403)]
        public static void WindowBuild() => PlayerBuilder.BuildWindowAssets();

        [MenuItem("Build/Addressable/Andriod Build", false, 404)]
        public static void AndroidBuild() => PlayerBuilder.BuildAndroidAssets();

        [MenuItem("Build/Addressable/WebGl Build", false, 405)]
        public static void WebGlBuild() => PlayerBuilder.BuildWebGlAssets();

        [MenuItem("Build/Addressable/Switch Build", false, 406)]
        public static void SwitchBuild() => PlayerBuilder.BuildSwitchAssets();

        #endregion

        #region AssetBundle 构建工具

        [MenuItem("Build/AssetBundle/Folder AssetBundle Settings", false, 410)]
        public static void OpenFolderBundleSettings() => FolderBundleEditorWindow.ShowWindow();

        [MenuItem("Build/AssetBundle/AssetBundleMap", false, 411)]
        public static void CreateAssetBundleMap() => AssetBundleMapTool.CreateAssetBundleMap();

        [MenuItem("Build/AssetBundle/Build AssetBundle", false, 412)]
        public static void BuildAssetBundle() => EditorBundleBuild.BuildAsserBundleOnly();

        [MenuItem("Build/AssetBundle/Build AssetBundle Incrementally", false, 413)]
        public static void BuildAssetBundleIncrementally() => EditorBundleBuild.BuildAsserBundleIncrementally();

        [MenuItem("Build/AssetBundle/Build Play", false, 414)]
        public static void BuildPlay() => EditorBundleBuild.BuildPlayApp();

        [MenuItem("Build/AssetBundle/Build Play Only", false, 415)]
        public static void BuildPlayOnly() => EditorBundleBuild.BuildPlayAppOnly();

        #endregion

        #region 远程资源工具

        [MenuItem("Build/Remote/Remote Manifest配置", false, 420)]
        public static void OpenRemoteManifest() => GenerateRemoteManifestWindow.ShowWindow();

        #endregion

        #region GameObject UI 工具

        [MenuItem("GameObject/UI/ButtonEx", false, 500)]
        public static void AddButtonEx(MenuCommand command) => ButtonExMenu.AddButton(command);

        [MenuItem("GameObject/UI/TextEx", false, 501)]
        public static void AddTextEx(MenuCommand command) => TextExMenu.AddText(command);

        [MenuItem("GameObject/UI/Replace TextEx", false, 502)]
        public static void ReplaceTextEx(MenuCommand command) => TextExMenu.ReplaceWithText(command);

        [MenuItem("GameObject/UI/TextMeshProUGUIEx", false, 503)]
        public static void AddTextMeshProUGUIEx(MenuCommand command) => TextMeshProExMenu.AddTextMeshPro(command);

        [MenuItem("GameObject/UI/Replace TextMeshProUGUIEx", false, 504)]
        public static void ReplaceTextMeshProUGUIEx(MenuCommand command) => TextMeshProExMenu.ReplaceWithTextMeshPro(command);

        #endregion

        #region UI 代码生成工具

        [MenuItem("Tools/UFlow/Advanced UI Code Generator", false, 510)]
        public static void OpenAdvancedUICodeGenerator() => AdvancedUICodeGeneratorWindow.Open();

        [MenuItem("Assets/UFlow/Create UI Script", true, 511)]
        public static bool ValidateCreateUIScript() => UICodeGenerator.ValidateCreateUIScript();

        [MenuItem("Assets/UFlow/Create UI Script", false, 511)]
        public static void CreateUIScript() => UICodeGenerator.CreateUIScript();

        [MenuItem("Assets/UFlow/Add UI Component", false, 512)]
        public static void AddUIComponent() => UICodeGenerator.AddUIComponentToPrefab();

        #endregion
    }
}