using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class ActEditorWindow : EditorWindow
    {
        private const float TrackHeight = 28f;
        public static readonly float HeaderWidth = 180f;
        private const float TimeRulerHeight = 22f;
        private const float ToolbarControlHeight = 20f;

        internal static readonly Color TimelineBackground = new Color(0.105f, 0.115f, 0.135f, 1f);
        internal static readonly Color TimelineRowBackground = new Color(0.145f, 0.16f, 0.19f, 1f);
        internal static readonly Color TimelineHeaderBackground = new Color(0.12f, 0.135f, 0.16f, 1f);
        internal static readonly Color TimelineGridColor = new Color(0.55f, 0.62f, 0.72f, 0.12f);
        internal static readonly Color PlayheadColor = new Color(1f, 0.32f, 0.26f, 1f);

        private ActAsset _asset;
        private Vector2 _scroll;
        private float _zoom = 1f; // 秒/像素比例的倒数
        private float _pixelsPerSecond = 100f;

        private ActClipData _selection;

        #region Preview

        private ActRuntimePlayer _previewTarget;
        private ActPreview _preview;

        #endregion

        public static void Open()
        {
            GetWindow<ActEditorWindow>("Act Editor");
        }

        private void OnEnable()
        {
            _zoom = 1f;
            _pixelsPerSecond = 100f;
            minSize = new Vector2(720f, 320f);
        }

        private void OnDisable()
        {
            _preview?.Dispose();
            _preview = null;
        }

        private void OnGUI()
        {
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            HandleKeyboardShortcutsIMGUI();
            DrawToolbar();
            if (_asset == null)
            {
                GUILayout.Space(EditorUIStyle.ContentSpacing);
                using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
                {
                    EditorGUILayout.LabelField("No ActAsset Selected", EditorUIStyle.HeaderTitle);
                    EditorGUILayout.LabelField("Assign an existing asset above or create a new timeline to begin.", EditorUIStyle.MutedLabel);
                }
                return;
            }

            // Ruler
            var rulerRect = GUILayoutUtility.GetRect(0, position.width, TimeRulerHeight, TimeRulerHeight);
            DrawTimeRuler(rulerRect);

            // Tracks
            var contentHeight = Mathf.Max(position.height - TimeRulerHeight, (_asset.tracks.Count + 1) * TrackHeight);
            var viewRect = new Rect(0, TimeRulerHeight, position.width, position.height - TimeRulerHeight);
            var contentRect = new Rect(0, TimeRulerHeight, position.width - 5, contentHeight);
            EditorGUI.DrawRect(viewRect, TimelineBackground);
            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
            float y = TimeRulerHeight + TrackHeight;

            for (int i = 0; i < _trackRenders.Count; i++)
            {
                var track = _trackRenders[i];
                var rowRect = new Rect(0, y, contentRect.width, TrackHeight);
                var height = track.DrawTrackRow(rowRect, ref _selection, ref _scroll, _pixelsPerSecond);
                y += height;
            }

            GUI.EndScrollView();

            if (Event.current.type == EventType.Repaint) Repaint();
        }

        // 键盘监听（IMGUI）
        private void HandleKeyboardShortcutsIMGUI()
        {
            var e = Event.current;
            if (e.isScrollWheel && e.mousePosition.x > HeaderWidth) // Ctrl + 滚轮缩放
            {
                if (e.delta.y > 0) // Ctrl + 滚轮向下
                    _pixelsPerSecond = Mathf.Clamp(_pixelsPerSecond * 0.9f, 30f, 400f);
                else
                    _pixelsPerSecond = Mathf.Clamp(_pixelsPerSecond * 1.1f, 30f, 400f);
                e.Use();
            }
            // if (e.isMouse && e.type == EventType.MouseDown )
            // {
            //     _selection = null;
            //     GUI.FocusControl(null);
            // }
            if (e.type != EventType.KeyDown) return;
            bool ctrl = e.control || e.command;

            if (ctrl && e.keyCode == KeyCode.S) // Ctrl/Cmd + S
            {
                if (_asset != null)
                {
                    EditorUtility.SetDirty(_asset);
                    AssetDatabase.SaveAssets();
                    Debug.Log("ACT saved.");
                }
                e.Use();
            }

            if (e.keyCode == KeyCode.Delete && _selection != null) // Delete 删除选中片段
            {
                Undo.RecordObject(_asset, "Delete Clip");
                foreach (var tk in _asset.tracks)
                    if (tk.clips.Remove(_selection)) break;
                _selection = null;
                EditorUtility.SetDirty(_asset);
                Repaint();
                e.Use();
            }

            if (ctrl && (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.Plus)) // Ctrl + +
            {
                _pixelsPerSecond = Mathf.Clamp(_pixelsPerSecond * 1.1f, 30f, 400f);
                e.Use();
            }
            if (ctrl && e.keyCode == KeyCode.Minus) // Ctrl + -
            {
                _pixelsPerSecond = Mathf.Clamp(_pixelsPerSecond * 0.9f, 30f, 400f);
                e.Use();
            }
        }

        private List<TrackRenderer> _trackRenders;
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ACT", EditorStyles.boldLabel, GUILayout.Width(30f));
                _asset = (ActAsset)EditorGUILayout.ObjectField(_asset, typeof(ActAsset), false,
                    GUILayout.Width(220f), GUILayout.Height(ToolbarControlHeight));
                if (_asset == null)
                {
                    if (GUILayout.Button("Create", EditorUIStyle.TabButton, GUILayout.Width(58f)))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Create New ActAsset", "New ActAsset", "asset", "Please enter a file name to save the act asset to");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var newAsset = CreateInstance<ActAsset>();
                            AssetDatabase.CreateAsset(newAsset, path);
                            AssetDatabase.SaveAssets();
                            _asset = newAsset;
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = newAsset;
                        } 
                    }
                }
                
                GUILayout.Space(6f);
                GUILayout.Label("Preview", EditorStyles.miniLabel, GUILayout.Width(46f));
                _previewTarget = (ActRuntimePlayer)EditorGUILayout.ObjectField(_previewTarget, typeof(ActRuntimePlayer), true,
                    GUILayout.Width(170f), GUILayout.Height(ToolbarControlHeight));
                if (_preview == null && _previewTarget && _asset)
                {
                    _preview = new ActPreview(_asset, _previewTarget);
                }
                if (_previewTarget == null || _asset == null)
                {
                    if (_preview != null)
                    {
                        _preview.Dispose();
                        _preview = null;
                    }
                }
                else if (_preview != null && _preview.Asset != _asset)
                {
                    _preview.SetAsset(_asset);
                }
                else if (_preview != null && _preview.Target != _previewTarget)
                {
                    _preview.SetTarget(_previewTarget);
                }

                if (_preview != null)
                {
                    _preview.Loop = GUILayout.Toggle(_preview.Loop, "Loop", EditorStyles.toolbarButton, GUILayout.Width(44f));
                    GUILayout.Label($"{_preview.CurrentTime:0.00}s", EditorStyles.miniLabel, GUILayout.Width(48f));
                    GUI.enabled = !_preview.IsPlaying;
                    if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(30f)))
                    {
                        _preview.Play();
                    }
                    GUI.enabled = _preview.IsPlaying;
                    if (GUILayout.Button("Ⅱ", EditorStyles.toolbarButton, GUILayout.Width(30f)))
                    {
                        _preview.Pause();
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("■", EditorStyles.toolbarButton, GUILayout.Width(30f)))
                    {
                        _preview.Stop();
                    }
                }
                if (_asset == null)
                {
                    if (_trackRenders != null) _trackRenders = null;
                    return;
                }
                if (_trackRenders == null || _trackRenders.Count != _asset.tracks.Count)
                {
                    _trackRenders = _asset.tracks.Select(o => new TrackRenderer(_asset, o)).ToList();
                }


                GUILayout.FlexibleSpace();
                //  GUILayout.Label($"Duration: {_asset.duration:0.00}s");
                //  _asset.duration = Mathf.Max(0.1f, EditorGUILayout.Slider(_asset.duration, 0.1f, 120f, GUILayout.Width(200)));

                GUILayout.Space(8f);
                GUILayout.Label("Zoom", EditorStyles.miniLabel, GUILayout.Width(34f));
                _pixelsPerSecond = Mathf.Clamp(EditorGUILayout.Slider(_pixelsPerSecond, 30f, 400f, GUILayout.Width(130f)), 10f, 600f);

                if (!GUILayout.Button("+ Track", EditorUIStyle.TabButton, GUILayout.Width(62f))) return;
                Undo.RecordObject(_asset, "Add Track");
                _asset.tracks.Add(new ActTrackData() { name = $"Track {_asset.tracks.Count + 1}" });
                EditorUtility.SetDirty(_asset);
            }
        }

        private bool onPreviewDraw;
        private void DrawTimeRuler(Rect rect)
        {
            EditorGUI.DrawRect(rect, TimelineBackground);
            float totalPixels = Mathf.Max(rect.width - HeaderWidth, 0);
            float totalTime = totalPixels / _pixelsPerSecond;

            // Header
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, HeaderWidth, rect.height), TimelineHeaderBackground);
            GUI.Label(new Rect(rect.x + 10, rect.y + 2, HeaderWidth - 20, rect.height - 4), "TRACK / TIME", EditorStyles.miniBoldLabel);

            // Ruler body
            var body = new Rect(rect.x + HeaderWidth, rect.y, rect.width - HeaderWidth, rect.height);
            Handles.color = TimelineGridColor;
            Handles.DrawLine(new Vector3(body.x, body.yMax), new Vector3(body.xMax, body.yMax));

            // Tick every 0.5s
            for (float t = 0; t <= Mathf.Max(_asset?.duration ?? totalTime, totalTime) + 0.001f; t += 0.5f)
            {
                float x = body.x + t * _pixelsPerSecond - _scroll.x;
                if (x < body.x || x > body.xMax) continue;

                bool major = Mathf.Abs(t - Mathf.Round(t)) < 0.001f;
                float h = major ? 14f : 8f;
                Handles.color = new Color(0.72f, 0.78f, 0.88f, major ? 0.5f : 0.24f);
                Handles.DrawLine(new Vector3(x, body.yMax - h), new Vector3(x, body.yMax));

                if (major)
                    GUI.Label(new Rect(x + 2, body.y + 2, 40, 16), t.ToString("0.0") + "s", EditorStyles.miniLabel);
            }
            // 绘制一条线表示当前播放时间_preview.CurrentTime
            if (_preview != null)
            {
                float x = body.x + _preview.CurrentTime * _pixelsPerSecond - _scroll.x;
                if (x >= body.x && x <= body.xMax)
                {
                    // Handles.color = Color.red;
                    // Handles.DrawLine(new Vector3(x, body.y), new Vector3(x, body.yMax));
                    var drawRect = new Rect(x, body.y, 2, body.height);
                    EditorGUI.DrawRect(drawRect, PlayheadColor);
                    EditorGUIUtility.AddCursorRect(drawRect, MouseCursor.ResizeHorizontal);
                    var e = Event.current;
                    if (e.type == EventType.MouseDown && drawRect.Contains(e.mousePosition))
                    {
                        onPreviewDraw = true;
                        e.Use();
                    }
                    if (onPreviewDraw)
                    {
                        if (e.type == EventType.MouseDrag)
                        {
                            float t = (e.mousePosition.x - body.x + _scroll.x) / _pixelsPerSecond;
                            t = Mathf.Clamp(t, 0f, Mathf.Max(_asset?.duration ?? totalTime, totalTime));
                            _preview.EvaluateAt(t);
                            e.Use();
                        }
                        else if (e.type == EventType.MouseUp)
                        {
                            onPreviewDraw = false;
                            e.Use();
                        }
                    }

                }
            }
        }

        private static GUIStyle s_RedMiniButton;
        public static GUIStyle GetRedMiniButton()
        {
            if (s_RedMiniButton != null) return s_RedMiniButton;
            s_RedMiniButton = new GUIStyle(EditorStyles.miniButton);
            // 删除按钮，背景红色
            // s_RedMiniButton.normal.background = Texture2D.grayTexture;
            //     s_RedMiniButton.hover.background = Texture2D.redTexture;
            //     s_RedMiniButton.active.background = Texture2D.redTexture;
            s_RedMiniButton.normal.textColor = new Color(0.9f, 0.52f, 0.5f);
            s_RedMiniButton.hover.textColor = Color.white;
            s_RedMiniButton.active.textColor = Color.white;
            // s_RedMiniButton.active.textColor = Color.white;

            return s_RedMiniButton;
        }
    }
}