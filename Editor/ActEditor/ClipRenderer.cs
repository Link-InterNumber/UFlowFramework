using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class ClipRenderer
    {
        private ActAsset _actAsset;
        private ActTrackData _track;
        private ActClipData _clip;
        private TrackRenderer _trackRenderer;

        private enum EventHandle
        {
            None,
            Draw,
            LeftDraw,
            RightDraw,
        }
        private EventHandle _eventHandle;

        public ClipRenderer(ActAsset asset, ActTrackData track, ActClipData clip, TrackRenderer trackRenderer)
        {
            _actAsset = asset;
            _track = track;
            _clip = clip;
            _trackRenderer = trackRenderer;
            _eventHandle = EventHandle.None;
        }

        // hasSelected 是输出参数，用于告诉 TrackRenderer 本行是否包含当前选中项
        public void DrawClip(Rect trackRect, ref ActClipData selection, float pixelsPerSecond, ref Vector2 scroll, ref bool hasSelected)
        {
            float x = trackRect.x + _clip.start * pixelsPerSecond - scroll.x;
            float w = Mathf.Max(_clip.length * pixelsPerSecond, 8f);
            var clipRect = new Rect(x, trackRect.y + 4, w, trackRect.height - 8);
            var drawRect = new Rect(clipRect.x + 4, clipRect.y, clipRect.width - 8, clipRect.height);
            var leftHandleRect = new Rect(clipRect.x, clipRect.y, 4, clipRect.height);
            var rightHandleRect = new Rect(clipRect.xMax - 4, clipRect.y, 4, clipRect.height);
            EditorGUIUtility.AddCursorRect(drawRect, MouseCursor.Pan);
            EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeHorizontal);

            EditorGUI.DrawRect(clipRect, _clip.editorColor * 0.85f);
            GUI.Label(new Rect(clipRect.x + 4, clipRect.y, clipRect.width - 8, clipRect.height),
                $"{_clip.editorName} ({_clip.length:0.00}s)", EditorStyles.miniBoldLabel);

            var e = Event.current;
            // 选中
            if (selection == _clip)
            {
                hasSelected = true;
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 2f), Color.yellow);
                if (e.keyCode == KeyCode.Delete && selection != null) // Delete 删除选中片段
                {
                    Undo.RecordObject(_actAsset, "Delete Clip");
                    selection = null;
                    Destory();
                    e.Use();
                    return;
                }
            }

            // 交互：拖拽移动 & 选中响应
            if (e.type == EventType.MouseDown && clipRect.Contains(e.mousePosition))
            {
                selection = _clip;
                GUI.FocusControl(null);
                e.Use();
                if (drawRect.Contains(e.mousePosition))
                {
                    _eventHandle = EventHandle.Draw;
                }
                else if (leftHandleRect.Contains(e.mousePosition))
                {
                    _eventHandle = EventHandle.LeftDraw;
                }
                else if (rightHandleRect.Contains(e.mousePosition))
                {
                    _eventHandle = EventHandle.RightDraw;
                }
            }
            else if (e.type == EventType.MouseDown)
            {
                _eventHandle = EventHandle.None;
            }

            if (e.type != EventType.MouseDrag) return;

            if (_eventHandle == EventHandle.Draw)
            {
                Undo.RecordObject(_actAsset, "Move Clip");
                var deltaTime = e.delta.x / pixelsPerSecond;
                _clip.start = Mathf.Max(0f, _clip.start + deltaTime);
                EditorUtility.SetDirty(_actAsset);
                e.Use();
            }
            else if (_eventHandle == EventHandle.LeftDraw)
            {
                Undo.RecordObject(_actAsset, "Move Clip");
                var deltaTime = e.delta.x / pixelsPerSecond;
                var start = Mathf.Clamp(_clip.start + deltaTime, 0f, _clip.start + _clip.length - 0.1f);
                _clip.length += _clip.start - start;
                _clip.start = start;
                EditorUtility.SetDirty(_actAsset);
                e.Use();
            }
            else if (_eventHandle == EventHandle.RightDraw)
            {
                Undo.RecordObject(_actAsset, "Move Clip");
                var deltaTime = e.delta.x / pixelsPerSecond;
                _clip.length = Mathf.Max(_clip.length + deltaTime, 0.1f);
                EditorUtility.SetDirty(_actAsset);
                e.Use();
            }
        }

        public void Destory()
        {
            foreach (var tk in _actAsset.tracks)
            {
                if (tk.clips.Remove(_clip))
                {
                    break;
                }
            }
            EditorUtility.SetDirty(_actAsset);
            GUI.FocusControl(null);
        }
    }
}