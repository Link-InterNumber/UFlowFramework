#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_STANDALONE_WIN
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace PowerCellStudio
{
    public class TextureBatchResizer : EditorWindow
    {
        string sourceFolder = "";
        string targetFolder = "Assets/ResizedImages";
        float scalePercent = 50f;
        int textureMinSize = 64;

        [MenuItem("Tools/Batch Resize and Save Images")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TextureBatchResizer), false, "Batch Resize and Save Images");
        }

        void OnEnable()
        {
            sourceFolder = EditorPrefs.GetString("TextureBatchResizer_SourceFolder", "");
            targetFolder = EditorPrefs.GetString("TextureBatchResizer_TargetFolder", "Assets/ResizedImages");
        }

        void OnDisable()
        {
            EditorPrefs.SetString("TextureBatchResizer_SourceFolder", sourceFolder);
            EditorPrefs.SetString("TextureBatchResizer_TargetFolder", targetFolder);
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

            EditorGUILayout.BeginHorizontal();
            targetFolder = EditorGUILayout.TextField("Target Folder (Assets path)", targetFolder);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Target Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        targetFolder = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        Debug.LogError("Target folder must be inside the Assets folder!");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            scalePercent = EditorGUILayout.Slider("Scale Percent", scalePercent, 1, 100);
            textureMinSize = EditorGUILayout.IntField("Minimum Size (pixels)", textureMinSize);

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
                    string relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string saveDir = Path.Combine(targetFolder, Path.GetDirectoryName(relativePath));
                    string fileName = Path.GetFileNameWithoutExtension(file) + "_scaled" + Path.GetExtension(file);
                    string savePath = Path.Combine(saveDir, fileName);

                    if (File.Exists(savePath))
                        return;

                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    using (var srcImage = Image.FromFile(file))
                    {
                        int newWidth = (int)(srcImage.Width * scalePercent / 100f);
                        int newHeight = (int)(srcImage.Height * scalePercent / 100f);

                        if (newWidth < textureMinSize || newHeight < textureMinSize)
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
                });
            }
            else
            {
                // 非Windows平台，主线程用Unity API处理
                foreach (string file in imageFiles)
                {
                    string relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string saveDir = Path.Combine(targetFolder, Path.GetDirectoryName(relativePath));
                    string fileName = Path.GetFileNameWithoutExtension(file) + "_scaled" + Path.GetExtension(file);
                    string savePath = Path.Combine(saveDir, fileName);

                    if (File.Exists(savePath))
                        continue;

                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    byte[] bytes = File.ReadAllBytes(file);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);

                    int newWidth = Mathf.RoundToInt(tex.width * scalePercent / 100f);
                    int newHeight = Mathf.RoundToInt(tex.height * scalePercent / 100f);
                    if (newWidth < textureMinSize || newHeight < textureMinSize)
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

                        byte[] outBytes = file.EndsWith(".png") ? newTexture.EncodeToPNG() : newTexture.EncodeToJPG();
                        File.WriteAllBytes(savePath, outBytes);

                        DestroyImmediate(newTexture);
                        rt.Release();
                    }
                    DestroyImmediate(tex);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Batch resize and save completed!");
        }

        void SetPixelPerUnit()
        {
            float ppu = scalePercent;
            string[] files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories);
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
            Debug.Log("Batch pixelPerUnit setting completed!");
        }
    }
}

#endif