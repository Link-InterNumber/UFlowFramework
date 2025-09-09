namespace PowerCellStudio
{
    public interface IGuidanceConfig
    {
        ///Id
        public int id {get;}
        ///后续引导id
        public int nextGuidance {get;}
        ///引导文字
        public LocalizationStringRef decs {get;}
        ///点击屏幕其他位置跳过
        public bool touchScreenToSkip {get;}
        ///阻止控件触发点击
        public bool blockInteraction {get;}
        ///ui预制体
        public GameObjectRef uiPrefab {get;}
    }
}