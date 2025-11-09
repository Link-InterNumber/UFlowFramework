using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SmoothNormalsProcessor : EditorWindow
{
   [MenuItem("Tools/Mesh/Smooth Normals for Outline")]
   static void Init()
   {
      SmoothNormalsProcessor window = (SmoothNormalsProcessor)EditorWindow.GetWindow(typeof(SmoothNormalsProcessor));
      window.Show();
   }

   private Mesh targetMesh;
   // private int textureSize = 1024;
   // private bool showPreview = true;
   private Texture2D previewTexture;
   private int writeToUVIndex = 0;

   private string[] uvOptions = new string[] { "UV2", "UV3", "UV4", "UV5", "UV6", "UV7", "UV8" };
   private int[] uvOptionValues = new int[] { 0, 1, 2, 3, 4, 5, 6 };

   void OnGUI()
   {
      GUILayout.Label("Smooth Normals for Outline", EditorStyles.boldLabel);
      targetMesh = EditorGUILayout.ObjectField("Target Mesh", targetMesh, typeof(Mesh), false) as Mesh;
      // textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
      writeToUVIndex = EditorGUILayout.IntPopup(writeToUVIndex, uvOptions, uvOptionValues);
      // showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);

      GUI.enabled = targetMesh != null;
      if (GUILayout.Button("Generate Smooth Normal To " + uvOptions[writeToUVIndex]))
      {
         var smoothNormals = SmoothNormals(targetMesh);
         targetMesh.SetUVs(writeToUVIndex + 1, smoothNormals);
         AssetDatabase.Refresh();
         Debug.Log("Smooth normal generated and assigned.");
      }
      GUI.enabled = true;

      // if (showPreview && previewTexture != null)
      // {
      //    GUILayout.Label("Preview:");
      //    Rect rect = GUILayoutUtility.GetRect(256, 256);
      //    EditorGUI.DrawPreviewTexture(rect, previewTexture);
      // }

      // if (GUILayout.Button("Process Selected Meshes"))
      // {
      //    ProcessSelectedMeshes();
      // }
   }

   void OnDisable()
   {
      if (previewTexture != null)
      {
         DestroyImmediate(previewTexture);
         previewTexture = null;
      }
      targetMesh = null;
   }

   // void ProcessSelectedMeshes()
   // {
   //    foreach (GameObject obj in Selection.gameObjects)
   //    {
   //       MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
   //       if (meshFilter != null)
   //       {
   //          Mesh mesh = meshFilter.sharedMesh;
   //          SmoothNormals(mesh);
   //       }
   //    }
   // }

   Vector3[] SmoothNormals(Mesh mesh)
   {
      Vector3[] vertices = mesh.vertices;
      Vector3[] normals = new Vector3[vertices.Length];

      // 创建顶点到法线的映射
      Dictionary<Vector3, List<int>> vertexMap = new Dictionary<Vector3, List<int>>();

      for (int i = 0; i < vertices.Length; i++)
      {
         if (!vertexMap.ContainsKey(vertices[i]))
            vertexMap[vertices[i]] = new List<int>();
         vertexMap[vertices[i]].Add(i);
      }

      // 计算平滑法线
      foreach (var kvp in vertexMap)
      {
         Vector3 smoothNormal = Vector3.zero;
         foreach (int index in kvp.Value)
         {
            smoothNormal += mesh.normals[index];
         }
         smoothNormal.Normalize();

         foreach (int index in kvp.Value)
         {
            normals[index] = smoothNormal;
         }
      }

      return normals;
   }

   // void GenerateNormalTexture(Mesh mesh, string savePath)
   // {
   //    // 计算平滑法线
   //    Vector3[] smoothNormals = SmoothNormals(mesh);

   //    // 获取UV坐标
   //    Vector2[] uvs = mesh.uv;
   //    if (uvs.Length == 0)
   //    {
   //       Debug.LogError("Mesh has no UV coordinates!");
   //       return;
   //    }

   //    // 创建法线贴图
   //    Texture2D normalTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
   //    Color[] pixels = new Color[textureSize * textureSize];

   //    // 初始化为默认法线 (0.5, 0.5, 1, 1)
   //    for (int i = 0; i < pixels.Length; i++)
   //    {
   //       pixels[i] = new Color(0.5f, 0.5f, 1f, 1f);
   //    }

   //    // 将平滑法线烘焙到贴图
   //    int[] triangles = mesh.triangles;
   //    Vector3[] vertices = mesh.vertices;

   //    for (int i = 0; i < triangles.Length; i += 3)
   //    {
   //       int i0 = triangles[i];
   //       int i1 = triangles[i + 1];
   //       int i2 = triangles[i + 2];

   //       Vector2 uv0 = uvs[i0];
   //       Vector2 uv1 = uvs[i1];
   //       Vector2 uv2 = uvs[i2];

   //       Vector3 normal0 = smoothNormals[i0];
   //       Vector3 normal1 = smoothNormals[i1];
   //       Vector3 normal2 = smoothNormals[i2];

   //       // 光栅化三角形并写入法线
   //       RasterizeTriangle(pixels, textureSize, uv0, uv1, uv2, normal0, normal1, normal2);
   //    }

   //    normalTexture.SetPixels(pixels);
   //    normalTexture.Apply();
   //    previewTexture = normalTexture;

   //    // 保存贴图
   //    SaveNormalTexture(normalTexture, savePath);
   // }

   // void RasterizeTriangle(Color[] pixels, int textureSize, Vector2 uv0, Vector2 uv1, Vector2 uv2,
   //                    Vector3 normal0, Vector3 normal1, Vector3 normal2)
   // {
   //    // 转换UV到像素坐标
   //    Vector2Int p0 = new Vector2Int(Mathf.RoundToInt(uv0.x * (textureSize - 1)), Mathf.RoundToInt(uv0.y * (textureSize - 1)));
   //    Vector2Int p1 = new Vector2Int(Mathf.RoundToInt(uv1.x * (textureSize - 1)), Mathf.RoundToInt(uv1.y * (textureSize - 1)));
   //    Vector2Int p2 = new Vector2Int(Mathf.RoundToInt(uv2.x * (textureSize - 1)), Mathf.RoundToInt(uv2.y * (textureSize - 1)));

   //    // 简单的三角形光栅化（可以优化）
   //    int minX = Mathf.Max(0, Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)));
   //    int maxX = Mathf.Min(textureSize - 1, Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
   //    int minY = Mathf.Max(0, Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)));
   //    int maxY = Mathf.Min(textureSize - 1, Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)));

   //    for (int y = minY; y <= maxY; y++)
   //    {
   //       for (int x = minX; x <= maxX; x++)
   //       {
   //          Vector2 p = new Vector2(x, y);
   //          Vector3 barycentric = CalculateBarycentric(p, p0, p1, p2);

   //          if (barycentric.x >= 0 && barycentric.y >= 0 && barycentric.z >= 0)
   //          {
   //             // 插值法线
   //             Vector3 interpolatedNormal = normal0 * barycentric.x + normal1 * barycentric.y + normal2 * barycentric.z;
   //             interpolatedNormal.Normalize();

   //             // 转换法线到贴图格式 (0-1范围)
   //             Color normalColor = new Color(
   //                 interpolatedNormal.x * 0.5f + 0.5f,
   //                 interpolatedNormal.y * 0.5f + 0.5f,
   //                 interpolatedNormal.z * 0.5f + 0.5f,
   //                 1f
   //             );

   //             int pixelIndex = y * textureSize + x;
   //             pixels[pixelIndex] = normalColor;
   //          }
   //       }
   //    }
   // }

   // Vector3 CalculateBarycentric(Vector2 p, Vector2Int a, Vector2Int b, Vector2Int c)
   // {
   //    Vector2 v0 = c - a;
   //    Vector2 v1 = b - a;
   //    Vector2 v2 = p - a;

   //    float dot00 = Vector2.Dot(v0, v0);
   //    float dot01 = Vector2.Dot(v0, v1);
   //    float dot02 = Vector2.Dot(v0, v2);
   //    float dot11 = Vector2.Dot(v1, v1);
   //    float dot12 = Vector2.Dot(v1, v2);

   //    float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
   //    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
   //    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

   //    return new Vector3(1 - u - v, v, u);
   // }

   // void SaveNormalTexture(Texture2D texture, string filePath)
   // {
   //    string folderPath = "Assets/GeneratedNormals";
   //    if (!AssetDatabase.IsValidFolder(folderPath))
   //    {
   //       AssetDatabase.CreateFolder("Assets", "GeneratedNormals");
   //    }

   //    byte[] pngData = texture.EncodeToPNG();
   //    // string fileName = $"{objectName}_SmoothNormals.png";
   //    // string filePath = $"{folderPath}/{fileName}";

   //    System.IO.File.WriteAllBytes(filePath, pngData);
   //    AssetDatabase.ImportAsset(filePath);

   //    // 设置为法线贴图
   //    TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
   //    if (textureImporter != null)
   //    {
   //       textureImporter.textureType = TextureImporterType.NormalMap;
   //       textureImporter.SaveAndReimport();
   //    }

   //    Debug.Log($"Normal texture saved: {filePath}");
   // }
}