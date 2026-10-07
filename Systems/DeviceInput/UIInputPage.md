# UI 用户设备输入响应系统

## 1. 系统概述

该系统为 Unity UI 页面提供基于栈的输入响应机制。输入适配层负责读取具体输入来源并生成统一的 `InputEvent<TKey>`，`InputStack<TKey>` 只把事件发送给当前栈顶页面，因此页面逻辑不需要依赖旧版 `Input` API 或 Unity Input System。

系统适合按钮、数值轴和二维方向输入；当前未提供指针、文本输入、滚轮或多指触控的专用事件模型。

主要特点：

- 通过页面栈实现当前页面独占输入，栈顶页面为空或没有对应监听器时不会向下层页面冒泡。
- 通过 `InputEvent<TKey>` 隔离输入来源与页面响应逻辑。
- 使用 `Dictionary<TKey, InputHandler<TKey>>` 按 action key 查找监听器，使用 `Stack<InputPage<TKey>>` 管理页面生命周期。
- 通过 `IInputAdapter<TKey>` 为外部输入适配器提供统一的栈访问契约。

`Sample/` 目录中的 `LegacyInputAdapter`、`ILegacyInputCapturer<TKey>` 和 `MoveCapturer` 仅用于演示如何接入旧版 Unity 输入，不属于框架核心成员。文档后续会以示例形式介绍它们。

### 主要组件 / 接口

- `InputEvent<TKey>`：描述输入键、事件类型、状态、标量值和二维值。
- `InputPage<TKey>`：保存一个页面的输入监听器，并实现 `IInputPage<TKey>`。
- `IInputPage<TKey>`：定义页面处理输入、查询和管理监听器的公共契约。
- `InputStack<TKey>`：创建、压入、弹出页面，并向栈顶页面分发事件。
- `IInputAdapter<TKey>`：暴露外部输入适配器持有的 `InputStack<TKey>`。

### 组件依赖关系

```text
外部输入适配器（示例：`Sample/LegacyInputAdapter`）
    |
    |──> IInputAdapter<TKey>
    |
    └──> InputStack<TKey>
            |
            |──> Func<InputPage<TKey>> 页面工厂
            |
            └──> InputPage<TKey>
                    |
                    |──> IInputPage<TKey>
                    |
                    └──> InputHandler<TKey>
```

`InputEvent<TKey>` 是采集器、适配器和页面之间传递的数据结构；它不属于某个具体输入后端。依赖关系图中的适配器和采集器均为外部接入示例，不是框架核心类型。

## 2. 工作原理与优化

### 初始化与页面创建

以 `Sample/LegacyInputAdapter.Awake()` 为例，示例适配器创建 `InputStack<TKey>`，并传入页面工厂：

```csharp
new InputStack<LegacyInputKey>(() => new InputPage<LegacyInputKey>())
```

调用 `PushPage()` 时，栈通过该工厂创建页面并压入栈顶。这样页面创建策略由适配器注入，`InputStack<TKey>` 本身不直接固定页面构造方式。

### 事件分发

以 `Sample/LegacyInputAdapter.Update()` 为例，示例适配器的每帧运行流程如下：

1. 适配器检查当前页面是否存在且包含监听器。
2. 适配器遍历输入采集器。
3. 采集器生成 `InputEvent<TKey>`。
4. `InputStack<TKey>.Dispatch()` 获取栈顶页面。
5. 页面按 `actionKey` 查找 `InputHandler<TKey>` 并执行监听器。

`Dispatch()` 会在栈为空或当前页面 `isEmpty` 时直接返回。当前实现不向下层页面传播事件。

### 监听器管理

`InputPage<TKey>` 为每个 action key 创建一个 `InputHandler<TKey>`。同一 key 可以注册多个回调。

- `HasListener()` 根据处理器的监听器数量返回状态。
- `RemoveListener()` 移除指定回调；处理器为空时会从字典删除。
- `RemoveAllListener()` 删除指定 key 的处理器。
- `ClearAll()` 释放当前页面的全部处理器。

### 释放流程

`PopPage()` 弹出栈顶页面并调用其 `Dispose()`。`InputStack.Dispose()` 会逐个弹出并释放所有页面，然后将内部栈置为 `null`。页面释放后，其公开操作会抛出 `ObjectDisposedException`。

### 性能和内存特点

- 页面和处理器按需创建，key 查找平均复杂度为 $O(1)$。
- 栈操作平均复杂度为 $O(1)$。
- `InputEvent<TKey>` 是值类型，事件传递本身不需要托管堆分配。
- 示例适配器使用普通 `for` 循环遍历采集器；该遍历方式属于示例实现，不是核心框架的强制约定。
- 移除最后一个监听器时会删除对应处理器，避免动态 key 场景下保留大量空处理器。

这些优化不改变事件语义：示例 `MoveCapturer` 当前即使没有移动输入，也会返回并派发 `NoInput` 事件；页面若不需要该状态，应在回调中自行过滤。

## 3. 使用方法

下面使用 `Sample/LegacyInputAdapter` 和 `Sample/LegacyInputKey` 作为接入示例，展示页面打开、注册监听、接收输入和关闭页面的最小闭环。它们不是框架核心成员。

### 3.1 创建适配器

在场景中的 GameObject 上挂载示例脚本 `Sample/LegacyInputAdapter`。该示例会在 `Awake()` 中初始化输入栈和 `MoveCapturer`；实际项目也可以替换为自己的适配器。

### 3.2 打开页面并注册监听

```csharp
using UnityEngine;
using UFlowFramework;
using UFlowFramework.Sample;

public sealed class InventoryPage : MonoBehaviour
{
    private InputPage<LegacyInputKey> _inputPage;

    public void Open(LegacyInputAdapter adapter)
    {
        _inputPage = adapter.inputStack.PushPage();
        _inputPage.AddListener(LegacyInputKey.Move, OnMove);
    }

    private void OnMove(InputEvent<LegacyInputKey> inputEvent)
    {
        if (inputEvent.state != InputEventState.PressThisFrame &&
            inputEvent.state != InputEventState.Hold)
        {
            return;
        }

        Vector2 moveDirection = inputEvent.v2;
        Debug.Log($"Move: {moveDirection}");
    }
}
```

也可以使用 `InputStack<TKey>.AddListener()` 注册到当前栈顶页面，但页面对象直接调用 `AddListener()` 更明确，能够避免页面切换后误注册到新的栈顶页面。

### 3.3 关闭页面

页面关闭时必须弹出对应的栈顶页面：

```csharp
public void Close(LegacyInputAdapter adapter)
{
    adapter.inputStack.PopPage();
    _inputPage = null;
}
```

`PopPage()` 会释放页面内的处理器和回调。页面被弹出后，不应继续使用之前保存的 `InputPage<TKey>` 引用。

### 3.4 清理监听但保留页面

如果页面仍需留在栈顶、但暂时不响应输入，可以调用：

```csharp
adapter.inputStack.ClearPeekPage();
```

该操作只清理栈顶页面的监听器，不会弹出页面。

## 4. 扩展方法

框架提供的公共扩展点是：

1. 实现 `IInputAdapter<TKey>`，接入新的输入来源。

`Sample/` 中的 `ILegacyInputCapturer<TKey>`、`LegacyInputAdapter` 和 `MoveCapturer` 只是示例代码。若采用类似的采集器组织方式，项目可以自行定义等价接口和实现，但它们不属于框架公共扩展点。

### 4.1 以示例方式添加旧版输入采集器

下面示例实现一个真实可用的按键采集器，并展示它与示例接口 `ILegacyInputCapturer<TKey>` 的关系。该代码属于接入示例，不会扩展框架核心类型：

```csharp
using UnityEngine;
using UFlowFramework;

namespace UFlowFramework.Sample
{
    public sealed class FireCapturer : ILegacyInputCapturer<LegacyInputKey>
    {
        public bool TryGetEvent(out InputEvent<LegacyInputKey> inputEvent)
        {
            bool pressed = Input.GetKey(KeyCode.Space);
            inputEvent = InputEvent<LegacyInputKey>.CreateButtonEvent(
                LegacyInputKey.Fire,
                Input.GetKeyDown(KeyCode.Space),
                Input.GetKeyUp(KeyCode.Space),
                pressed);
            return true;
        }

        public void Dispose()
        {
        }
    }
}
```

将采集器加入示例 `LegacyInputAdapter.Awake()` 中的 `_capturers` 列表后，它会在后续 `Update()` 中被轮询。自定义采集器必须使用相同的 `TKey`，并在 `Dispose()` 中释放自己持有的资源。

### 4.2 实现新的输入适配器

新的适配器应实现 `IInputAdapter<TKey>`，自行创建 `InputStack<TKey>`，把外部输入转换为 `InputEvent<TKey>` 后调用 `Dispatch()`。Input System 类型只能出现在适配器层；页面层只依赖 `InputPage<TKey>` 和 `InputEvent<TKey>`。

当前仓库没有实际的 Input System 适配器。项目可以按该接口约定自行新增适配器，但不能把 `Sample/` 下的示例适配器当作框架内置实现或稳定公共 API。

扩展时必须保持以下不变量：

- 同一适配器内的页面和事件使用一致的 `TKey` 类型及比较语义。
- 输入分发和页面回调在 Unity 主线程执行。
- 外部适配器销毁时释放自己的 `InputStack<TKey>` 和输入采集资源。
- 不应让旧版输入适配器和新输入适配器同时派发同一逻辑动作。

## 5. 注意事项

### 生命周期

- `InputStack<TKey>` 必须先创建页面，再注册监听器。
- `InputStack.Dispose()` 后继续调用其方法会抛出 `ObjectDisposedException`。
- `InputPage<TKey>.Dispose()` 后继续调用页面方法也会抛出 `ObjectDisposedException`。
- `PopPage()` 只允许按栈顶顺序关闭页面；关闭非栈顶页面需要由业务层维护页面关闭顺序。
- 外部输入适配器应在销毁时释放自己创建的 `InputStack<TKey>` 和输入采集资源；示例 `LegacyInputAdapter` 的实现仅供参考。

### 栈顶行为

输入只发送给栈顶页面。即使栈顶页面没有注册当前 action key，也不会继续尝试下层页面。

## 6. 推荐使用场景

### UI 页面独占输入

打开背包、设置或暂停页面时压入新页面，使导航输入只作用于当前页面。

### 多层弹窗

确认弹窗压入栈顶后，可以阻止下层背包页面继续接收相同输入。

### 输入后端替换

页面只依赖统一的 `InputEvent<TKey>`，可以替换旧版输入采集器或新增其他适配器，而不修改页面回调逻辑。

### 临时输入屏蔽

压入一个没有监听器的页面可以阻挡下层页面；操作完成后弹出该页面即可恢复下层页面。

### 不推荐场景

该系统不适合需要多个页面广播接收、向下层冒泡、后台线程处理，或复杂手势与文本输入的场景。这些需求需要额外的传播策略、线程调度或专用输入事件模型。
