namespace PowerCellStudio
{
    public class GuidanceGraphEditorConfigGroup : IGuidanceGraphConfigProvider
    {
        private EditorConfigGroup _configGroup;

        public void Load()
        {
            _configGroup = new EditorConfigGroup();
            // _configGroup.Append<>
            _configGroup.LoadConfig();
        }

        public IGuidanceConfig Get(int id)
        {
            // TODO : 根据id获取对应的引导配置
            return null;
        }
    }
}