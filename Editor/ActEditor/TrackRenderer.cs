using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class TrackRenderer
    {
        private ActAsset _asset;
        private ActTrackData _track;

        public TrackRenderer(ActAsset asset, ActTrackData track)
        {
            _asset = asset;
            _track = track;
            _clipRenderers = new List<ClipRenderer>();
            RefreshClips();
        }

        public void RefreshClips()
        {
            _clipRenderers.Clear();
            foreach (var clip in _track.clips)
            {
                _clipRenderers.Add(new ClipRenderer(_asset, _track, clip, this));
            }
        }

        // 返回本行高度（包含 inspector 扩展高度）
        private List<ClipRenderer> _clipRenderers = new List<ClipRenderer>();

        public float DrawTrackRow(Rect rect, ref ActClipData selection, ref Vector2 scroll, float pixelsPerSecond)
        {
            var trackHeight = rect.height;
            // Header
            var header = new Rect(rect.x, rect.y, ActEditorWindow.HeaderWidth, rect.height);
            EditorGUI.DrawRect(header, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(new Rect(header.x + 2, header.y + 4, 4, rect.height - 8), _track.color);
            _track.name = EditorGUI.TextField(new Rect(header.x + 10, header.y + 4, header.width - 50, rect.height - 8), _track.name);

            // 删除按钮
            if (GUI.Button(new Rect(header.xMax - 30, header.y + 5, 25, 16), "X", ActEditorWindow.GetRedMiniButton()))
            {
                if (EditorUtility.DisplayDialog("Delete Track", $"Are you sure to delete track '{_track.name}'?", "Yes", "No"))
                {
                    Undo.RecordObject(_asset, "Delete Track");
                    _asset.tracks.Remove(_track);
                    if (selection != null && _track.clips.Contains(selection))
                        selection = null;
                    EditorUtility.SetDirty(_asset);
                    // Repaint caller should handle Repaint; we call it here for safety
                    var win = EditorWindow.GetWindow<ActEditorWindow>();
                    win?.Repaint();
                    return trackHeight;
                }
            }

            // Body
            var body = new Rect(rect.x + ActEditorWindow.HeaderWidth, rect.y, rect.width - ActEditorWindow.HeaderWidth, rect.height);
            EditorGUI.DrawRect(body, new Color(0.18f, 0.18f, 0.18f));

            var hasSelected = false;
            if (_clipRenderers == null || _clipRenderers.Count != _track.clips.Count)
            {
                RefreshClips();
            }
            // Clips
            foreach (var clip in _clipRenderers)
            {
                clip.DrawClip(body, ref selection, pixelsPerSecond, ref scroll, ref hasSelected);
            }

            // 右键菜单
            var e = Event.current;
            if (e.type == EventType.ContextClick && body.Contains(e.mousePosition))
            {
                var scrollX = scroll.x;
                // 反射获取继承 ActClipData 所有子类
                var clipClass = typeof(ActClipData);
                var types = System.AppDomain.CurrentDomain.GetAssemblies();
                var clipTypes = new List<System.Type>();
                foreach (var assembly in types)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(clipClass))
                        {
                            clipTypes.Add(type);
                        }
                    }
                }
                if (clipTypes.Count > 0)
                {
                    var menu = new GenericMenu();
                    foreach (var type in clipTypes)
                    {
                        menu.AddItem(new GUIContent($"Add {type.Name}"), false, () =>
                        {
                            Undo.RecordObject(_asset, "Add Clip");
                            var newClip = (ActClipData)System.Activator.CreateInstance(type);
                            newClip.start = Mathf.Max(0f, scrollX / pixelsPerSecond);
                            newClip.length = 1f;
                            _track.clips.Add(newClip);
                            EditorUtility.SetDirty(_asset);
                            RefreshClips();
                            // Repaint caller should handle Repaint; we call it here for safety
                            var win = EditorWindow.GetWindow<ActEditorWindow>();
                            win?.Repaint();
                        });
                    }
                    menu.ShowAsContext();
                }
                e.Use();
            }

            // Inspector 区域（当行有选中 clip 并且该选中 clip 属于本 track）
            if (selection != null && hasSelected)
            {
                trackHeight += ClipInspectorRenderer.DrawInspector(selection, _asset, rect.y);
            }

            return trackHeight;
        }
    }
}