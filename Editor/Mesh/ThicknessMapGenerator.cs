using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class ThicknessMapGenerator : EditorWindow
{
    private Mesh targetMesh;
    private float maxRayDistance = 10.0f;
    private int textureSize = 1024;
    private int rayCount = 256;
    private bool showPreview = true;
    private Texture2D previewTexture;
    
    [MenuItem("Tools/Mesh/Generate Thickness Map")]
    public static void ShowWindow()
    {
        GetWindow<ThicknessMapGenerator>("Thickness Map Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Thickness Map Generator", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        targetMesh = EditorGUILayout.ObjectField("Target Mesh", targetMesh, typeof(Mesh), false) as Mesh;
        maxRayDistance = EditorGUILayout.FloatField("Max Ray Distance", maxRayDistance);
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        rayCount = EditorGUILayout.IntField("Ray Count per Direction", rayCount);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        
        GUI.enabled = targetMesh != null;
        if (GUILayout.Button("Generate Thickness Map"))
        {
            GenerateThicknessMap();
        }
        GUI.enabled = true;
        
        if (showPreview && previewTexture != null)
        {
            GUILayout.Label("Preview:");
            Rect rect = GUILayoutUtility.GetRect(256, 256);
            EditorGUI.DrawPreviewTexture(rect, previewTexture);
        }
    }
    
    private void GenerateThicknessMap()
    {
        if (targetMesh == null)
            return;
            
        // 创建厚度图纹理
        Texture2D thicknessMap = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        // 获取网格数据
        Vector3[] vertices = targetMesh.vertices;
        int[] triangles = targetMesh.triangles;
        Vector3[] normals = targetMesh.normals;
        
        // 如果没有法线数据，则计算法线
        if (normals == null || normals.Length == 0)
        {
            targetMesh.RecalculateNormals();
            normals = targetMesh.normals;
        }
        
        // 创建加速结构
        Bounds bounds = targetMesh.bounds;
        float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

        float diag = bounds.size.magnitude;
        // 1) 自适应追踪上限：<=0 表示自动
        float traceMax = (maxRayDistance <= 0f ? diag * 4f : maxRayDistance);
        // 尺度相关的偏移与最小距离
        float eps = Mathf.Max(1e-5f, maxDimension * 1e-4f);
        
        // 创建射线，以顶点位置和法线方向为基础
        float maxThickness = 0.0f;
        Dictionary<int, float> vertexThickness = new Dictionary<int, float>();
        
        EditorUtility.DisplayProgressBar("Computing Thickness", "Calculating thickness values...", 0.0f);
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 origin = vertices[i];
            Vector3 normal = normals[i];
            
            // 计算厚度 (向内部发射射线)
            float thickness = CalculateThicknessAtPoint(i, origin, -normal, vertices, triangles, traceMax, eps);
            vertexThickness[i] = thickness;
            
            // 记录最大厚度值用于归一化
            if (thickness > maxThickness)
                maxThickness = thickness;
                
            if (i % 200 == 0)
                EditorUtility.DisplayProgressBar("Computing Thickness", "Calculating thickness values...", (float)i / vertices.Length);
        }

        // 3) 防御：避免除零；若全部为0，则直接退出
        if (maxThickness <= 1e-8f)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning("All thickness values are zero. Check mesh closedness or ray settings.");
            return;
        }
        
        // 创建用于烘焙的网格
        List<Vector3> unwrappedVertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> unwrappedTriangles = new List<int>();
        
        // 使用已有的UV，或者如果没有UV则自动生成
        Vector2[] meshUVs = targetMesh.uv;
        bool hasUVs = meshUVs != null && meshUVs.Length == vertices.Length;
        
        if (!hasUVs)
        {
            EditorUtility.DisplayProgressBar("Computing Thickness", "No UVs found, generating UV layout...", 0.5f);
            // 简单地生成一个新的网格并自动生成UV
            Unwrapping.GenerateSecondaryUVSet(targetMesh);
            meshUVs = targetMesh.uv2;
        }
        
        // 将厚度值绘制到纹理上
        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                pixels[y * textureSize + x] = Color.black;
            }
        }
        
        EditorUtility.DisplayProgressBar("Computing Thickness", "Baking thickness to texture...", 0.7f);
        
        // 对每个三角形，将厚度烘焙到纹理
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];
            
            Vector2 uv1 = meshUVs[v1];
            Vector2 uv2 = meshUVs[v2];
            Vector2 uv3 = meshUVs[v3];
            
            float t1 = vertexThickness[v1] / maxThickness;
            float t2 = vertexThickness[v2] / maxThickness;
            float t3 = vertexThickness[v3] / maxThickness;
            
            RasterizeTriangle(pixels, textureSize, textureSize, 
                              uv1, uv2, uv3, 
                              t1, t2, t3);
        }
        
        thicknessMap.SetPixels(pixels);
        thicknessMap.Apply();
        
        // 保存纹理到文件
        string meshPath = AssetDatabase.GetAssetPath(targetMesh);
        string directory = Path.GetDirectoryName(meshPath);
        // 获取mesh文件名
        string meshName = targetMesh.name;
        string fileName = Path.GetFileNameWithoutExtension(meshPath) + $"_{meshName}" + "_Thickness.png";
        string savePath = Path.Combine(directory, fileName);
        
        byte[] pngData = thicknessMap.EncodeToPNG();
        File.WriteAllBytes(savePath, pngData);
        AssetDatabase.Refresh();
        
        // 更新预览
        previewTexture = thicknessMap;
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"厚度图已保存到: {savePath}");
    }
    
    private float CalculateThicknessAtPoint(
        int sourceVertex,
        Vector3 origin,
        Vector3 direction,
        Vector3[] vertices,
        int[] triangles,
        float traceMax,
        float eps)
    {
        // 射线起点向内偏移一个与尺度相关的量
        Vector3 rayOrigin = origin + direction.normalized * eps;

        float best = float.PositiveInfinity;
        bool hit = false;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            // 4) 跳过包含当前顶点的三角形，避免“立刻命中自身”
            if (i0 == sourceVertex || i1 == sourceVertex || i2 == sourceVertex)
                continue;

            if (RayIntersectsTriangle(rayOrigin, direction, vertices[i0], vertices[i1], vertices[i2], out float t))
            {
                if (t > eps && t < best)
                {
                    best = t;
                    hit = true;
                }
            }
        }

        if (!hit) return 0f;
        // 5) 不做硬 clamp；可做早停上限过滤（超出追踪距离认为无效，避免把“上限”当最大值）
        if (best > traceMax) return 0f;

        return best;
    }
    
    private bool RayIntersectsTriangle(Vector3 origin, Vector3 direction, 
                                      Vector3 v1, Vector3 v2, Vector3 v3, 
                                      out float distance)
    {
        distance = 0;
        
        // 计算三角形法线
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        Vector3 h = Vector3.Cross(direction, edge2);
        float a = Vector3.Dot(edge1, h);
        
        // 如果射线平行于三角形，则没有相交
        if (a > -0.0000001f && a < 0.0000001f)
            return false;
            
        float f = 1.0f / a;
        Vector3 s = origin - v1;
        float u = f * Vector3.Dot(s, h);
        
        if (u < 0.0f || u > 1.0f)
            return false;
            
        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(direction, q);
        
        if (v < 0.0f || u + v > 1.0f)
            return false;
            
        // 计算交点距离
        float t = f * Vector3.Dot(edge2, q);
        
        if (t > 0.0000001f)
        {
            distance = t;
            return true;
        }
        
        return false;
    }
    
    private void RasterizeTriangle(Color[] pixels, int width, int height,
                                  Vector2 uv1, Vector2 uv2, Vector2 uv3,
                                  float t1, float t2, float t3)
    {
        // 确保UV在[0,1]范围内
        uv1 = new Vector2(Mathf.Clamp01(uv1.x), Mathf.Clamp01(uv1.y));
        uv2 = new Vector2(Mathf.Clamp01(uv2.x), Mathf.Clamp01(uv2.y));
        uv3 = new Vector2(Mathf.Clamp01(uv3.x), Mathf.Clamp01(uv3.y));

        // 将UV坐标转换为像素坐标
        Vector2 p1 = new Vector2(uv1.x * width, uv1.y * height);
        Vector2 p2 = new Vector2(uv2.x * width, uv2.y * height);
        Vector2 p3 = new Vector2(uv3.x * width, uv3.y * height);
        
        // 计算三角形的包围盒
        int minX = Mathf.FloorToInt(Mathf.Min(p1.x, Mathf.Min(p2.x, p3.x)));
        int maxX = Mathf.CeilToInt(Mathf.Max(p1.x, Mathf.Max(p2.x, p3.x)));
        int minY = Mathf.FloorToInt(Mathf.Min(p1.y, Mathf.Min(p2.y, p3.y)));
        int maxY = Mathf.CeilToInt(Mathf.Max(p1.y, Mathf.Max(p2.y, p3.y)));
        
        // 限制在纹理范围内
        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(width - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(height - 1, maxY);
        
        // 三角形光栅化
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 pixelPos = new Vector2(x + 0.5f, y + 0.5f);
                
                // 计算重心坐标
                Vector3 barycentric = ComputeBarycentric(pixelPos, p1, p2, p3);
                
                if (barycentric.x >= 0 && barycentric.y >= 0 && barycentric.z >= 0)
                {
                    // 插值厚度值
                    float thickness = barycentric.x * t1 + barycentric.y * t2 + barycentric.z * t3;
                    // 写入像素
                    int pixelIndex = y * width + x;
                    pixels[pixelIndex] = new Color(thickness, thickness, thickness, 1);
                }
            }
        }
    }
    
    private Vector3 ComputeBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = point - a;
        
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        
        float denom = d00 * d11 - d01 * d01;
        
        if (Mathf.Abs(denom) < 0.0000001f)
            return new Vector3(-1, -1, -1);
            
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        
        return new Vector3(u, v, w);
    }
}