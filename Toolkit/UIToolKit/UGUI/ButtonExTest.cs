using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// ButtonEx 的运行时回归测试。将此组件挂载到场景对象后启用即可执行。
    /// </summary>
    public class ButtonExTest : RunTestMono
    {
        public ButtonEx _buttonEx;

        private void OnEnable()
        {
            _buttonEx.onLongPress.RemoveAllListeners();
            _buttonEx.onLongPress.AddListener(OnLongPress);
        }

        private void OnLongPress()
        {
            Debug.LogWarning($"ButtonExTest: Long press event triggered. time = {_buttonEx.longPressedTime}, state = {_buttonEx.pressState}");
        }
    }
}
