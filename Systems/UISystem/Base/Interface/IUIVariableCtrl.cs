namespace PowerCellStudio
{
    /// <summary>
    /// 可变的UI控制逻辑，用来处理复用同一个界面但业务逻辑不同的情况
    /// Variable UI control logic is used to handle the case of reusing the same UI prefab but the business logic is different
    /// </summary>
    public interface IUIVariableCtrl<T> : IDisposable
        where T : UIVariableWindow
    {
        T ui {get;};

        void BindUIEvent(T ui);

        void DisbindUIEvent(T ui);

        void OnOpen(object data, T ui);

        void OnClose(T ui);
        
        void OnFocus(T ui);
    }

    public interface IUIVariableUpdateCtrl<T> : IUIVariableCtrl<T>
    {
        void Update(T ui);
    }
}