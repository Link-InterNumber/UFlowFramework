#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json;

namespace PowerCellStudio.Editor
{
    public class TextureBatchResizer : EditorWindow
    {
        string sourceFolder = "";

        [Serializable]
        public class configSetting
        {
            public string targetFolder = "Assets/ResizedImages";
            public float scalePercent = 50f;
            public int textureMinSize = 64;
        }

        private List<configSetting> configSettings = new List<configSetting>();

        // string targetFolder = "Assets/ResizedImages";
        // float scalePercent = 50f;
        // int textureMinSize = 64;

        [MenuItem("Tools/UFlow/Batch Resize and Save Images")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TextureBatchResizer), false, "Batch Resize and Save Images");
        }

        void OnEnable()
        {
            sourceFolder = EditorSaveUtils.GetEditorPref("TextureBatchResizer_SourceFolder", "");
            var configJson = EditorSaveUtils.GetEditorPref("TextureBatchResizer_Config", "{}");
            configSettings = JsonConvert.DeserializeObject<List<configSetting>>(configJson);
        }

        void OnDisable()
        {
            EditorSaveUtils.SetEditorPref("TextureBatchResizer_SourceFolder", sourceFolder);
            EditorSaveUtils.SetEditorPref("TextureBatchResizer_Config", JsonConvert.SerializeObject(configSettings));
        }

        void OnGUI()
        {
            GUILayout.Label("Batch Resize and Save Images", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            sourceFolder = EditorGUILayout.TextField("Source Folder (absolute path)", sourceFolder);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Source Folder", "", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    sourceFolder = selectedPath;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Config:", EditorStyles.boldLabel);
            // EditorGUILayout.BeginHorizontal();
            var removeIndex = -1;
            for (var i = 0; i < configSettings.Count; i++)
            {
                var configSetting = configSettings[i];
                configSetting.targetFolder =
                    EditorGUILayout.TextField("Target Folder (Assets path)", configSetting.targetFolder);
                if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
                {
                    string selectedPath =
                        EditorUtility.OpenFolderPanel("Select Target Folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        if (selectedPath.StartsWith(Application.dataPath))
                        {
                            configSetting.targetFolder = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            Debug.LogError("Target folder must be inside the Assets folder!");
                        }
                    }
                }

                configSetting.scalePercent = EditorGUILayout.Slider("Scale Percent", configSetting.scalePercent, 1, 100);
                configSetting.textureMinSize = EditorGUILayout.IntField("Minimum Size (pixels)", configSetting.textureMinSize);
                if (GUILayout.Button("-"))
                {
                    removeIndex = i;
                }

                EditorGUILayout.Space();
            }
            // EditorGUILayout.EndHorizontal();

            if (removeIndex > 0) configSettings.RemoveAt(removeIndex);

            if (GUILayout.Button("+")) configSettings.Add(new configSetting());

            if (GUILayout.Button("Start Processing"))
            {
                if (Directory.Exists(sourceFolder))
                {
                    ProcessImages();
                }
                else
                {
                    Debug.LogError("Source folder does not exist!");
                }
            }

            if (GUILayout.Button("Batch Set pixelPerUnit"))
            {
                SetPixelPerUnit();
            }
        }

        void ProcessImages()
        {
            string[] files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            var imageFiles = new List<string>();
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    imageFiles.Add(file);
                }
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Parallel.ForEach(imageFiles, file =>
                {
                    for (var i = 0; i < configSettings.Count; i++)
                    {
                        var configSetting = configSettings[i];
                        string relativePath = file.Substring(sourceFolder.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string saveDir = Path.Combine(configSetting.targetFolder, Path.GetDirectoryName(relativePath));
                        string fileName = Path.GetFileNameWithoutExtension(file) + "_scaled" + Path.GetExtension(file);
                        string savePath = Path.Combine(saveDir, fileName);

                        if (File.Exists(savePath))
                            return;

                        if (!Directory.Exists(saveDir))
                            Directory.CreateDirectory(saveDir);

                        using (var srcImage = Image.FromFile(file))
                        {
                            int newWidth = (int)(srcImage.Width * configSetting.scalePercent / 100f);
                            int newHeight = (int)(srcImage.Height * configSetting.scalePercent / 100f);

                            if (newWidth < configSetting.textureMinSize || newHeight < configSetting.textureMinSize)
                            {
                                // 直接拷贝原图
                                File.Copy(file, Path.Combine(saveDir, Path.GetFileName(file)), true);
                            }
                            else
                            {
                                using (var newBmp = new Bitmap(newWidth, newHeight))
                                using (var g = System.Drawing.Graphics.FromImage(newBmp))
                                {
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.DrawImage(srcImage, 0, 0, newWidth, newHeight);

                                    if (file.EndsWith(".png"))
                                        newBmp.Save(savePath, ImageFormat.Png);
                                    else
                                        newBmp.Save(savePath, ImageFormat.Jpeg);
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                // 非Windows平台，主线程用Unity API处理
                foreach (string file in imageFiles)
                {
                    for (var i = 0; i < configSettings.Count; i++)
                    {
                        var configSetting = configSettings[i];
                        string relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string saveDir = Path.Combine(configSetting.targetFolder, Path.GetDirectoryName(relativePath));
                        string fileName = Path.GetFileNameWithoutExtension(file) + "_scaled" + Path.GetExtension(file);
                        string savePath = Path.Combine(saveDir, fileName);

                        if (File.Exists(savePath))
                            continue;

                        if (!Directory.Exists(saveDir))
                            Directory.CreateDirectory(saveDir);

                        byte[] bytes = File.ReadAllBytes(file);
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(bytes);

                        int newWidth = Mathf.RoundToInt(tex.width * configSetting.scalePercent / 100f);
                        int newHeight = Mathf.RoundToInt(tex.height * configSetting.scalePercent / 100f);
                        if (newWidth < configSetting.textureMinSize || newHeight < configSetting.textureMinSize)
                        {
                            // 直接拷贝原图
                            File.Copy(file, Path.Combine(saveDir, Path.GetFileName(file)), true);
                        }
                        else
                        {
                            RenderTexture rt = new RenderTexture(newWidth, newHeight, 0);
                            rt.filterMode = FilterMode.Bilinear;
                            UnityEngine.Graphics.Blit(tex, rt);

                            Texture2D newTexture = new Texture2D(rt.width, rt.height, tex.format, false);
                            RenderTexture.active = rt;
                            newTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                            newTexture.Apply();
                            RenderTexture.active = null;

                            byte[] outBytes = file.EndsWith(".png")
                                ? newTexture.EncodeToPNG()
                                : newTexture.EncodeToJPG();
                            File.WriteAllBytes(savePath, outBytes);

                            DestroyImmediate(newTexture);
                            rt.Release();
                        }

                        DestroyImmediate(tex);
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Batch resize and save completed!");
        }

        void SetPixelPerUnit()
        {
            for (var i = 0; i < configSettings.Count; i++)
            {
                var configSetting = configSettings[i];
                float ppu = configSetting.scalePercent;
                string[] files = Directory.GetFiles(configSetting.targetFolder, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                    {
                        var filaName = Path.GetFileNameWithoutExtension(file);
                        if (!filaName.Contains("_scaled")) continue;

                        string assetPath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer == null) continue;
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = ppu;
                        importer.SaveAndReimport();
                    }
                }
            }
            Debug.Log("Batch pixelPerUnit setting completed!");
        }
    }
}

#endif