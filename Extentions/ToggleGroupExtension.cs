namespace PowerCellStudio
{
    public static class ToggleGroupExtension
    {
        public static void AddListener(this UnityEngine.UI.ToggleGroup toggleGroup, System.Action<bool> action)
        {
            var toggles = toggleGroup.GetComponentsInChildren<UnityEngine.UI.Toggle>();
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.AddListener(isOn => action(isOn));
            }
        }

        public static void RemoveListener(this UnityEngine.UI.ToggleGroup toggleGroup, System.Action<bool> action)
        {
            var toggles = toggleGroup.GetComponentsInChildren<UnityEngine.UI.Toggle>();
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveListener(isOn => action(isOn));
            }
        }
    }
}