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

        [MenuItem("Tools/Batch Resize and Save Images")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TextureBatchResizer), false, "Batch Resize and Save Images");
        }

        void OnGUI()
        {
            GUILayout.Label("Batch Resize and Save Images", EditorStyles.boldLabel);
            sourceFolder = EditorGUILayout.TextField("Source Folder (absolute path)", sourceFolder);
            targetFolder = EditorGUILayout.TextField("Target Folder (Assets path)", targetFolder);
            scalePercent = EditorGUILayout.Slider("Scale Percent", scalePercent, 1, 100);
            minSize = EditorGUILayout.IntField("Minimum Size (pixels)", minSize);

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
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    string relativePath = file.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string saveDir = Path.Combine(targetFolder, Path.GetDirectoryName(relativePath));
                    string fileName = Path.GetFileNameWithoutExtension(file) + "_scaled" + Path.GetExtension(file);
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
                        // Copy original image directly
                        File.Copy(file, savePath, true);
                    }
                    else
                    {
                        // Create RenderTexture
                        RenderTexture rt = new RenderTexture(newWidth, newHeight, 0);
                        rt.filterMode = FilterMode.Bilinear;
                        // Use Graphics.Blit for scaling and copying
                        Graphics.Blit(tex, rt);

                        // Read from RenderTexture to new Texture2D
                        Texture2D newTexture = new Texture2D(rt.width, rt.height, tex.format, false);
                        RenderTexture.active = rt;
                        newTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        newTexture.Apply();
                        RenderTexture.active = null;

                        // Write to disk
                        byte[] outBytes = file.EndsWith(".png") ? newTexture.EncodeToPNG() : newTexture.EncodeToJPG();
                        File.WriteAllBytes(savePath, outBytes);

                        // Release intermediate files
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
            float ppu = 100f / scalePercent;
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