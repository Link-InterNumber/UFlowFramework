using System.Collections.Generic;
using UnityEngine;

namespace UFlowFramework.Sample
{
    /// <summary>
    /// 测试 LegacyInputAdapter 与 InputStack 的输入采集、页面切换和监听清理。
    /// Tests input capture, page switching, and listener cleanup for LegacyInputAdapter and InputStack.
    /// </summary>
    [RequireComponent(typeof(LegacyInputAdapter))]
    public sealed class LegacyInputAdapterTest : MonoBehaviour
    {
        [SerializeField] private bool _logHoldEvents;
        private LegacyInputAdapter _adapter;
        private int _pageDepth;
        private int _receivedEventCount;

        private void Start()
        {
            _adapter = GetComponent<LegacyInputAdapter>();
            PushTestPage();

            Debug.Log(
                "[LegacyInputAdapterTest] Ready. Use WASD to move, left mouse button to fire, " +
                "Space to jump, F1 to push a page, F2 to pop a page, and F3 to clear the current page.",
                this);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1))
            {
                PushTestPage();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                PopTestPage();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                ClearCurrentPage();
            }
#endif
        }

        private void OnEnable()
        {
            if (_adapter != null)
            {
                PushTestPage();
            }
        }

        private void OnDisable()
        {
            _pageDepth = 0;
        }

        /// <summary>压入并配置一个测试输入页。 Pushes and configures a test input page.</summary>
        [ContextMenu("Push Test Page")]
        public void PushTestPage()
        {
            if (!TryGetInputStack(out var inputStack))
            {
                return;
            }

            _pageDepth++;
            var pageName = $"Page {_pageDepth}";
            inputStack.PushPage();
            inputStack.AddListener(LegacyInputKey.Move, inputEvent => LogInput(pageName, inputEvent));
            inputStack.AddListener(LegacyInputKey.Fire, inputEvent => LogInput(pageName, inputEvent));
            inputStack.AddListener(LegacyInputKey.Jump, inputEvent => LogInput(pageName, inputEvent));

            Debug.Log($"[LegacyInputAdapterTest] Pushed {pageName}.", this);
        }

        /// <summary>弹出当前测试输入页。 Pops the current test input page.</summary>
        [ContextMenu("Pop Test Page")]
        public void PopTestPage()
        {
            if (!TryGetInputStack(out var inputStack) || inputStack.GetCurrentPage() == null)
            {
                return;
            }

            inputStack.PopPage();
            _pageDepth = Mathf.Max(0, _pageDepth - 1);
            Debug.Log($"[LegacyInputAdapterTest] Popped page. Current depth: {_pageDepth}.", this);
        }

        /// <summary>清空当前页的全部输入监听。 Clears all listeners on the current page.</summary>
        [ContextMenu("Clear Current Page")]
        public void ClearCurrentPage()
        {
            if (!TryGetInputStack(out var inputStack))
            {
                return;
            }

            inputStack.ClearPeekPage();
            Debug.Log("[LegacyInputAdapterTest] Cleared listeners on the current page.", this);
        }

        private bool TryGetInputStack(out InputStack<LegacyInputKey> inputStack)
        {
            inputStack = null;
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return false;
            }

            if (_adapter == null)
            {
                _adapter = GetComponent<LegacyInputAdapter>();
            }

            inputStack = _adapter != null ? _adapter.inputStack : null;
            if (inputStack != null)
            {
                return true;
            }

            Debug.LogWarning(
                "[LegacyInputAdapterTest] LegacyInputAdapter has not initialized its InputStack yet. " +
                "Run this operation in Play Mode after Awake.",
                this);
            return false;
        }

        private void LogInput(string pageName, InputEvent<LegacyInputKey> inputEvent)
        {
            _receivedEventCount++;
            if (inputEvent.state == InputEventState.Released ||
                (inputEvent.state == InputEventState.Hold && !_logHoldEvents))
            {
                return;
            }
            
            if ((inputEvent.type & InputEventType.Button) != 0 && !inputEvent.isClickPerformance)
                return;

            Debug.Log(
                $"[LegacyInputAdapterTest] #{_receivedEventCount} {pageName} | " +
                $"Key={inputEvent.actionKey}, Type={inputEvent.type}, State={inputEvent.state}, " +
                $"Value={inputEvent.value}, Vector={inputEvent.v2}",
                this);
        }
    }
}
