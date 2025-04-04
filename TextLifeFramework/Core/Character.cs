/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using TMPro;
using UnityEngine;

namespace EnjoyGameClub.TextLifeFramework.Core
{
    /// <summary>
    /// 描述字符的（如文本、UI元素）位置、旋转、缩放等信息的结构体。
    /// </summary>
    /// <remarks>
    /// A structure describing the position, rotation, scaling, and other transform data of 2D objects such as text or UI elements.
    /// </remarks>
    public struct Transform
    {
        // Transform Base Data
        public Vector3[] Vertices;
        public TMP_Text TMPComponent;

        // Position
        private Vector3 _localPosition;
        public Vector3 LocalPosition { get; private set; }

        public Vector3 ScreenPosition => GetScreenPosition();

        // Rotation
        private Quaternion _rotation;
        public Quaternion Rotation { get; private set; }

        private float _eulerAngle;
        public float EulerAngle { get; private set; }

        // Scale
        private Vector2 _localScale;
        public Vector2 LocalScale { get; private set; }
        public float Width => Vertices[3].x - Vertices[0].x;
        public float Height => Vertices[1].y - Vertices[0].y;

        /// <summary>
        /// 移动顶点位置。
        /// </summary>
        /// <remarks>
        /// Move vertices position.
        /// </remarks>
        /// <param name="offset">位移量。</param>
        public void Move(Vector3 offset)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] += offset;
            }

            _localPosition = GetLocalPosition();
        }

        /// <summary>
        /// 缩放网格。
        /// </summary>
        /// <remarks>
        /// Scale mesh.
        /// </remarks>
        /// <param name="scale">缩放比例。</param>
        /// <param name="pivotPercentage">可选的缩放枢轴点（0到1的比例）。</param>
        public void Scale(Vector2 scale, Vector2? pivotPercentage = null)
        {
            Vector3 pivot = pivotPercentage.HasValue
                ? new Vector3(
                    Mathf.Lerp(Vertices[0].x, Vertices[2].x, pivotPercentage.Value.x),
                    Mathf.Lerp(Vertices[0].y, Vertices[2].y, pivotPercentage.Value.y),
                    0
                )
                : GetLocalPosition();
            for (int i = 0; i < Vertices.Length; i++)
            {
                // 将顶点相对 Pivot 进行缩放
                Vertices[i] = pivot + Vector3.Scale(Vertices[i] - pivot, new Vector3(scale.x, scale.y, 1));
            }

            _localScale *= scale;
        }

        /// <summary>
        /// 旋转网格
        /// </summary>
        /// <remarks>
        /// Rotate mesh.
        /// </remarks>
        /// <param name="angle">旋转角度。</param>
        /// <param name="pivotPercentage">可选的旋转枢轴点（0到1的比例）。</param>
        public void Rotate(float angle, Vector2? pivotPercentage = null)
        {
            Vector3 pivot = pivotPercentage.HasValue
                ? new Vector3(
                    Mathf.Lerp(Vertices[0].x, Vertices[2].x, pivotPercentage.Value.x),
                    Mathf.Lerp(Vertices[0].y, Vertices[2].y, pivotPercentage.Value.y),
                    0
                )
                : GetLocalPosition();
            Quaternion rotation = Quaternion.Euler(0, 0, -angle);
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = pivot + rotation * (Vertices[i] - pivot);
            }

            _eulerAngle += angle;
            _rotation *= rotation;
        }

        /// <summary>
        /// 初始化 Transform 数据。
        /// </summary>
        /// <remarks>
        /// Initializes transform data with default values.
        /// </remarks>
        public void InitData()
        {
            _localPosition = GetLocalPosition();
            _eulerAngle = 0;
            _rotation = Quaternion.identity;
            _localScale = new Vector2(1, 1);
        }

        /// <summary>
        /// 记录当前 Transform 数据。
        /// </summary>
        /// <remarks>
        /// Records current transform data for later use.
        /// </remarks>
        public void RecordData()
        {
            LocalPosition = _localPosition;
            Rotation = _rotation;
            EulerAngle = _eulerAngle;
            LocalScale = _localScale;
        }
        /// <summary>
        /// 计算本地坐标中心点。
        /// </summary>
        /// <remarks>
        /// Calculates the center position of local coordinates.
        /// </remarks>
        /// <returns>本地坐标中心点</returns>
        private Vector3 GetLocalPosition()
        {
            Vector3 center = Vector3.zero;
            foreach (var vertex in Vertices)
            {
                center += vertex;
            }

            center /= Vertices.Length;
            return center;
        }
        /// <summary>
        /// 获取屏幕坐标位置。
        /// </summary>
        /// <remarks>
        /// Gets the screen position by combining local and component positions.
        /// </remarks>
        /// <returns>屏幕坐标位置</returns>
        private Vector3 GetScreenPosition()
        {
            return LocalPosition + TMPComponent.transform.position;
        }

        /// <summary>
        /// 将屏幕坐标转换为本地坐标。
        /// </summary>
        /// <remarks>
        /// Converts screen coordinates to local coordinates.
        /// </remarks>
        /// <param name="screenPos">屏幕坐标</param>
        /// <returns>本地坐标</returns>
        public Vector3 TransformScreenToLocal(Vector3 screenPos)
        {
            return screenPos - TMPComponent.transform.position - GetLocalPosition();
        }
    }

    /// <summary>
    /// 描述一个字符的结构体，包含字符数据、颜色、顶点信息等。
    /// </summary>
    /// <remarks>
    /// A structure that describes a character, including character data, color, vertex information, etc.
    /// </remarks>
    public struct Character
    {
        // Character data.
        public int CharIndex;
        public int StartIndex;
        public int EndIndex;
        public int TotalCount;
        public bool Visible;
        public TMP_MeshInfo MeshInfo;
        public TMP_CharacterInfo CharacterInfo;
        public TMP_Text TMPComponent;
        public Transform Transform;
        
        public Color32[] VerticesColor;

        /// <summary>
        /// 设置所有顶点的颜色。
        /// </summary>
        /// <remarks>
        /// Sets color for all vertices.
        /// </remarks>
        /// <param name="color">颜色</param>
        public void SetColor(Color32 color)
        {
            Array.Fill(VerticesColor, color);
        }

        /// <summary>
        /// 设置所有顶点的颜色。
        /// </summary>
        /// <remarks>
        /// Sets color for all vertices using Color struct.
        /// </remarks>
        /// <param name="color">颜色</param>
        public void SetColor(Color color)
        {
            Array.Fill(VerticesColor, color);
        }

        /// <summary>
        /// 设置顶点的透明度。
        /// </summary>
        /// <remarks>
        /// Sets alpha value for all vertices (0 to 1 range).
        /// </remarks>
        /// <param name="alpha">透明度，范围为 0 到 1</param>
        public void SetAlpha(float alpha)
        {
            int alpha32 = (int)(alpha * 255);
            alpha32 = Mathf.Clamp(alpha32, 0, 255);
            for (int i = 0; i < VerticesColor.Length; i++)
            {
                VerticesColor[i].a = Convert.ToByte(alpha32);
            }
        }

        /// <summary>
        /// 设置顶点的透明度。
        /// </summary>
        /// <remarks>
        /// Sets alpha value for all vertices (0 to 255 range).
        /// </remarks>
        /// <param name="alpha32">透明度，范围为 0 到 255</param>
        public void SetAlpha(int alpha32)
        {
            for (int i = 0; i < VerticesColor.Length; i++)
            {
                VerticesColor[i].a = Convert.ToByte(alpha32);
            }
        }
        
    }
}
