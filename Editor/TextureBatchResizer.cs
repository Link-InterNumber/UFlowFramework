#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

namespace PowerCellStudio
{
    public class TextureBatchResizer : EditorWindow
    {
        string sourceFolder = "";
        string targetFolder = "Assets/ResizedImages";
        float scalePercent = 50f;
        int minSize = 64;

        [MenuItem("Tools/图片批量缩放转存")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TextureBatchResizer), false, "图片批量缩放转存");
        }

        void OnGUI()
        {
            GUILayout.Label("图片批量缩放转存", EditorStyles.boldLabel);
            sourceFolder = EditorGUILayout.TextField("源文件夹(绝对路径)", sourceFolder);
            targetFolder = EditorGUILayout.TextField("目标文件夹(Assets下路径)", targetFolder);
            scalePercent = EditorGUILayout.Slider("缩放百分比", scalePercent, 1, 100);
            minSize = EditorGUILayout.IntField("最小尺寸(像素)", minSize);

            if (GUILayout.Button("开始处理"))
            {
                if (Directory.Exists(sourceFolder))
                {
                    ProcessImages();
                }
                else
                {
                    Debug.LogError("源文件夹不存在！");
                }
            }

            if (GUILayout.Button("批量设置pixelPerUnit"))
            {
                SetPixelPerUnit();
            }
        }

        void ProcessImages()
        {
            string[] files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    string relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string saveDir = Path.Combine(targetFolder, Path.GetDirectoryName(relativePath));
                    string savePath = Path.Combine(saveDir, Path.GetFileName(file));

                    if (File.Exists(savePath))
                        continue;

                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    byte[] bytes = File.ReadAllBytes(file);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);

                    int newWidth = Mathf.RoundToInt(tex.width * scalePercent / 100f);
                    int newHeight = Mathf.RoundToInt(tex.height * scalePercent / 100f);

                    if (newWidth < minSize || newHeight < minSize)
                    {
                        // 直接复制原图
                        File.Copy(file, savePath, true);
                    }
                    else
                    {
                        Texture2D resized = new Texture2D(newWidth, newHeight, tex.format, false);
                        for (int y = 0; y < newHeight; y++)
                        {
                            for (int x = 0; x < newWidth; x++)
                            {
                                Color color = tex.GetPixelBilinear((float)x / newWidth, (float)y / newHeight);
                                resized.SetPixel(x, y, color);
                            }
                        }
                        resized.Apply();

                        byte[] outBytes = file.EndsWith(".png") ? resized.EncodeToPNG() : resized.EncodeToJPG();
                        File.WriteAllBytes(savePath, outBytes);

                        DestroyImmediate(resized);
                    }
                    DestroyImmediate(tex);
                }
            }
            AssetDatabase.Refresh();
            Debug.Log("图片批量缩放转存完成！");
        }

        void SetPixelPerUnit()
        {
            float ppu = 100f / scalePercent;
            string[] files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    // 计算原图路径
                    string relativePath = file.Substring(targetFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string sourcePath = Path.Combine(sourceFolder, relativePath);

                    if (!File.Exists(sourcePath))
                        continue;

                    // 读取目标图片尺寸
                    byte[] targetBytes = File.ReadAllBytes(file);
                    Texture2D targetTex = new Texture2D(2, 2);
                    targetTex.LoadImage(targetBytes);

                    // 读取原图尺寸
                    byte[] sourceBytes = File.ReadAllBytes(sourcePath);
                    Texture2D sourceTex = new Texture2D(2, 2);
                    sourceTex.LoadImage(sourceBytes);

                    // 只有尺寸变化才设置pixelPerUnit
                    if (targetTex.width != sourceTex.width || targetTex.height != sourceTex.height)
                    {
                        string assetPath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer != null)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spritePixelsPerUnit = ppu;
                            importer.SaveAndReimport();
                        }
                    }

                    DestroyImmediate(targetTex);
                    DestroyImmediate(sourceTex);
                }
            }
            Debug.Log("pixelPerUnit批量设置完成！");
        }
    }
}

#endif