```mermaid 
        IUIParent
            |
            |--> IOpenWindowRequest-->IUIChild;
                                        |
                                        |--> IUIPoolable;
                                        |
                                        |--> IUIStandAlone;
```
# UISystem

## 概述
UISystem是一个基于Unity的UI框架，使用基于Page的堆管理逻辑，提供了UI的加载、显示、关闭、管理等功能。
`IUIComponent` : `IUIParent`, `IUIChild`的基础接口。  
`IUIParent` : 父节点接口，包含子节点的加载、显示关闭管理。  
`IUIChild` : 子节点接口，有两个特殊的子接口`IUIPoolable`和`IUIStandAlone`。继承`IUIPoolable`会从专用都对象池IUIParent加载和存放，继承I`UIStandAlone`会在UI最上层显示，并且不被Page系统管理。

## 使用
### 1. 创建UI
1. 创建一个Page类，继承`UIPage`
```
public class TestPage : UIPage
{
    public override void OnOpen(object data)
    {
        // 打开UI时调用
    }

    public override void OnClose()
    {
        // 关闭UI时调用
    }

    public override void RegisterEvent()
    {
        // 注册事件
    }

    public override void DeregisterEvent()
    {
        // 注销事件
    }
    
    public override void OnFocus()
    {
        // 获得焦点时调用
    }
}
```
2.创建一个Window类，继承`UIWindow`
```
public class TestWindow : UIWindow
{
    public virtual void RegisterEvent()
    {
        // 注册事件
        if(closeBtn) closeBtn.onClick.AddListener(OnCloseBtnClick);
    }
        
    public virtual void DeregisterEvent()
    {
        // 注销事件
        if(closeBtn) closeBtn.onClick.RemoveListener(OnCloseBtnClick);
    }
        
    public override void OnOpen(object data)
    {
        // 打开UI时调用
    }

    public override void OnClose()
    {
        // 关闭UI时调用
    }

    public override void OnFocus()
    {
        // 获得焦点时调用
    }
}
```