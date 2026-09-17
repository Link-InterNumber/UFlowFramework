# UI 用户设备输入响应系统

## 1. 系统概述

该系统用于为 UI 页面提供基于栈的输入响应机制。注意：仓库中的 `Sample` 目录只包含接入示例代码，示例脚本用于演示如何把旧版 `Input` 或其它输入源转换为 `InputEvent<TKey>`，但不属于框架核心实现，也不应被视为框架内部组件。

- 每个 UI 页面对应一个 `InputPage<TKey>`。
- 新页面打开时，通过 `InputStack<TKey>.PushPage()` 压入页面。
- 页面关闭时，通过 `InputStack<TKey>.PopPage()` 移除栈顶页面。
- 输入事件只会分发给当前栈顶页面。
- 输入采集逻辑与 UI 响应逻辑分离，可使用旧版 `Input` API 或 Unity Input System 实现不同的输入适配器。

系统当前更适合处理按钮、轴和方向输入，不包含指针、文本输入等事件类型。

### 主要组件 / 接口

- `InputEvent<TKey>`：统一描述输入动作、输入类型、输入状态和值。
- `InputEventType`：区分按钮输入和轴输入。
- `InputEventState`：描述本帧按下、持续、抬起或无输入状态。
- `InputHandler<TKey>`：保存某个输入键对应的回调。
- `InputPage<TKey>`：保存一个 UI 页面注册的输入回调。
- `InputStack<TKey>`：管理页面栈，并将事件发送给栈顶页面。
- `IInputAdapter<TKey>`：输入适配器接口，持有对应的 `InputStack<TKey>`。

说明：`ILegacyInputCapturer<TKey>`、`LegacyInputAdapter`、`MoveCapturer` 等位于 `Sample` 目录，仅作为接入示例存在，不属于框架的核心结构或运行时依赖。

### 组件依赖关系

```text
外部输入适配器（示例代码在 `Sample/`，非框架核心）
    |
    |──> IInputAdapter<TKey>
    |       |
    |       └──> InputStack<TKey>
    |               |
    |               |──> InputPage<TKey>
    |               |       |
    |               |       └──> InputHandler<TKey>
    |               |                   |
    |               |                   └──> Action<InputEvent<TKey>>
    |               |
    |               └──> InputEvent<TKey>
    |
    └──> 外部采集器（例如：Sample 中的 ILegacyInputCapturer、MoveCapturer）
```

`InputEvent<TKey>` 是输入采集器和页面响应逻辑之间的共享数据结构。当前代码中还没有实际的 Unity Input System 适配器实现。

---

## 2. 工作原理与优化

### 初始化流程

`LegacyInputAdapter.Awake()` 中会：

1. 创建 `InputStack<LegacyInputKey>`。
2. 创建采集器列表。
3. 添加 `MoveCapturer`。

`MoveCapturer` 不依赖页面对象，只负责读取键盘状态并生成：

```csharp
InputEvent<LegacyInputKey>
```

### 页面注册流程

调用 `PushPage()` 创建并压入新的 `InputPage<TKey>`：

```csharp
var page = inputStack.PushPage();
```

随后可以向当前页面注册输入回调：

```csharp
page.AddListener(
    LegacyInputKey.Move,
    OnMove);
```

`InputPage<TKey>` 内部使用：

```csharp
Dictionary<TKey, InputHandler<TKey>>
```

按输入键查找对应处理器。每个输入键只创建一个 `InputHandler<TKey>`，同一输入键可以注册多个回调。

### 输入分发流程

每帧 `LegacyInputAdapter.Update()` 会遍历所有采集器：

1. 采集器调用 `TryGetEvent()`。
2. 采集器生成 `InputEvent<TKey>`。
3. 适配器调用 `InputStack<TKey>.Dispatch()`。
4. 输入栈获取栈顶 `InputPage<TKey>`。
5. 页面根据 `actionKey` 查找处理器。
6. 处理器执行注册的回调。

页面栈不会向下层页面传播事件，因此只有最顶层页面可以响应输入。

### 输入状态

`MoveCapturer` 根据移动向量和上一帧的 `isMove` 状态生成：

- `PressThisFrame`：从无移动变为移动。
- `Hold`：持续移动。
- `ReleaseThisFrame`：从移动变为无移动。
- `NoInput`：当前没有移动且上一帧也没有移动。

### 性能和内存特点

当前实现具有以下特点：

- 使用 `Dictionary<TKey, InputHandler<TKey>>`，输入键查找平均复杂度为 $O(1)$。
- 使用 `Stack<InputPage<TKey>>` 管理页面，压入和弹出页面平均复杂度为 $O(1)$。
- `InputEvent<TKey>` 是 `readonly struct`，输入事件本身不需要堆分配。
- `Update()` 中使用普通 `for` 循环遍历采集器，避免枚举器分配。
- 输入回调使用委托事件保存，注册后不会在分发时重新构建集合。

当前实现仍存在以下约束：

- `MoveCapturer.TryGetEvent()` 每帧都会创建一个 `InputEvent`，但结构体本身通常不会造成托管堆分配。
- `InputEvent.v2` 每次访问都会创建一个新的 `Vector2` 值，但 `Vector2` 是值类型。
- 页面关闭时会释放当前页面的回调处理器。
- `InputStack.Dispose()` 当前只清空栈，没有逐页调用 `InputPage.Dispose()`，可能导致页面内部事件引用未被显式清理。

---

## 3. 使用方法

### 3.1 创建输入适配器

在场景中创建挂载 `LegacyInputAdapter` 的 GameObject。

当前 `LegacyInputAdapter` 在 `Awake()` 中自动初始化输入栈和 `MoveCapturer`。

但需要注意，当前代码定义了：

```csharp
private InputStack<LegacyInputKey> _inputStack;
public InputStack<LegacyInputKey> inputStack { get; }
```

`inputStack` 属性没有返回 `_inputStack`，因此外部通过接口访问时会得到 `null`。建议修正为：

```csharp
public InputStack<LegacyInputKey> inputStack => _inputStack;
```

### 3.2 打开页面并注册输入

下面示例表示打开一个页面，并注册移动输入：

```csharp
using UnityEngine;
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
        Vector2 moveDirection = inputEvent.v2;

        if (inputEvent.state == InputEventState.PressThisFrame ||
            inputEvent.state == InputEventState.Hold)
        {
            Debug.Log($"Move: {moveDirection}");
        }
    }
}
```

该示例使用的是当前项目中真实存在的：

- `LegacyInputAdapter`
- `LegacyInputKey`
- `InputPage<TKey>`
- `InputEvent<TKey>`
- `InputEventState`

### 3.3 关闭页面

关闭页面时弹出栈顶页面：

```csharp
public void Close(LegacyInputAdapter adapter)
{
    adapter.inputStack.PopPage();
    _inputPage = null;
}
```

`PopPage()` 会调用页面的 `Dispose()`，从而清理页面内部的输入处理器和回调。

### 3.4 清理当前页面监听

如果页面仍然保持打开状态，但需要暂时清理其所有监听，可以调用：

```csharp
adapter.inputStack.ClearPeekPage();
```

该方法只清理栈顶页面，不会移除页面本身。

### 3.5 销毁适配器

`LegacyInputAdapter.OnDestroy()` 负责：

- 释放输入栈。
- 释放输入采集器。
- 清空采集器列表。

输入适配器应当与其持有的 `InputStack<TKey>` 保持相同生命周期，不建议在适配器销毁后继续使用页面引用。

---

## 4. 扩展方法

当前系统提供两个主要扩展点：

- `ILegacyInputCapturer<TKey>`：扩展旧版输入采集逻辑。
- `IInputAdapter<TKey>`：扩展输入适配器类型。

### 4.1 自定义旧版输入采集器

例如，为 `Fire` 输入增加一个旧版输入采集器：

```csharp
using UnityEngine;

namespace UFlowFramework.Sample
{
    public sealed class FireCapturer : ILegacyInputCapturer<LegacyInputKey>
    {
        public bool TryGetEvent(out InputEvent<LegacyInputKey> inputEvent)
        {
            bool pressed = Input.GetKey(KeyCode.Space);
            bool pressThisFrame = Input.GetKeyDown(KeyCode.Space);
            bool releaseThisFrame = Input.GetKeyUp(KeyCode.Space);

            InputEventState state = InputEventState.NoInput;

            if (pressThisFrame)
            {
                state = InputEventState.PressThisFrame;
            }
            else if (releaseThisFrame)
            {
                state = InputEventState.ReleaseThisFrame;
            }
            else if (pressed)
            {
                state = InputEventState.Hold;
            }

            inputEvent = InputEvent<LegacyInputKey>.CreateButtonEvent(
                LegacyInputKey.Fire,
                pressThisFrame,
                releaseThisFrame,
                pressed);

            return true;
        }

        public void Dispose()
        {
        }
    }
}
```

然后在 `LegacyInputAdapter.Awake()` 中注册：

```csharp
_capturers.Add(new FireCapturer());
```

### 4.2 增加 Unity Input System 适配器

当前代码没有 `InputSystem` 适配器。可以按照相同原则实现一个独立适配器：

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

namespace UFlowFramework.Sample
{
    public sealed class InputSystemAdapter : UnityEngine.MonoBehaviour,
        IInputAdapter<LegacyInputKey>
    {
        private InputStack<LegacyInputKey> _inputStack;

        public InputStack<LegacyInputKey> inputStack => _inputStack;

        private void Awake()
        {
            _inputStack = new InputStack<LegacyInputKey>();
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            InputEventState state;

            if (context.started)
            {
                state = InputEventState.PressThisFrame;
            }
            else if (context.canceled)
            {
                state = InputEventState.ReleaseThisFrame;
            }
            else if (context.performed)
            {
                state = InputEventState.Hold;
            }
            else
            {
                state = InputEventState.NoInput;
            }

            var inputEvent = InputEvent<LegacyInputKey>.CreateButtonEvent(
                LegacyInputKey.Fire,
                InputEventType.Button,
                context.started,
                context.canceled,
                context.ReadValueAsButton());

            _inputStack.Dispatch(in inputEvent);
        }

        private void OnDestroy()
        {
            _inputStack?.Dispose();
            _inputStack = null;
        }
    }
}
#endif
```

该扩展需要满足以下条件：

- 使用 `#if ENABLE_INPUT_SYSTEM` 包裹 Input System 专属代码。
- 输入系统类型只能出现在适配器层。
- 页面层只能依赖 `InputEvent<TKey>`。
- Input System 适配器和旧版适配器不应同时重复派发同一逻辑输入。
- 适配器必须在 `OnDestroy()` 中释放自己的输入栈。

### 4.3 输入键类型

`InputStack<TKey>` 和 `InputEvent<TKey>` 使用泛型键，可以使用枚举作为输入键：

```csharp
public enum LegacyInputKey
{
    Move,
    Fire,
    Jump
}
```

也可以使用其他稳定的值类型或字符串类型，但同一个输入栈中的键必须保持一致的比较语义。

---

## 5. 注意事项

### 生命周期

`InputStack<TKey>.Dispose()` 会将内部栈设置为 `null`。释放后继续调用以下方法会抛出 `ObjectDisposedException`：

- `PushPage()`
- `PopPage()`
- `GetCurrentPage()`
- 通过这些方法间接调用的监听操作

页面引用也不应在 `PopPage()` 后继续使用。

### 栈顶页面行为

当前 `Dispatch()` 只调用栈顶页面：

```csharp
GetCurrentPage()?.Handle(in input);
```

如果栈顶页面没有注册对应的 `actionKey`，输入会被直接忽略，不会继续传递给下层页面。

因此，如果一个页面需要允许底层页面继续接收输入，当前系统尚未提供该能力。

### 页面注册位置

`InputStack<TKey>.AddListener()` 和 `RemoveListener()` 操作的是当前栈顶页面：

```csharp
adapter.inputStack.AddListener(...);
```

页面逻辑更推荐保存 `PushPage()` 返回的 `InputPage<TKey>`，并直接在该页面上注册监听，避免因栈顶页面变化而注册到错误页面。

### 空页面

可以压入一个没有任何监听器的页面。该页面仍然会阻挡下层页面接收输入。

如果页面暂时不需要响应输入，应根据业务需求决定：

- 不压入页面；
- 压入空页面并作为输入屏蔽层；
- 使用 `ClearPeekPage()` 清理当前监听。

### 当前 `InputStack.Dispose()` 的资源清理

当前实现：

```csharp
public void Dispose()
{
    _pages.Clear();
    _pages = null;
}
```

只清空页面栈，没有遍历调用每个页面的 `Dispose()`。如果页面内部仍然持有委托回调，可能无法及时解除引用。

建议改为逐个释放页面：

```csharp
public void Dispose()
{
    if (_pages == null)
    {
        return;
    }

    while (_pages.Count > 0)
    {
        _pages.Pop()?.Dispose();
    }

    _pages = null;
}
```

### `MoveCapturer.TryGetEvent()` 的返回值

当前 `MoveCapturer` 无论是否存在移动输入，都会返回：

```csharp
return true;
```

因此即使没有移动，也会每帧生成一个 `NoInput` 的移动事件。

如果不需要发送空输入，可以改为在 `moveDir == Vector2.zero` 且上一帧没有移动时返回 `false`。但这会改变当前事件流语义，是否修改应由页面对 `NoInput` 的使用方式决定。

### Unity 主线程

当前采集器调用了 `UnityEngine.Input`，适配器也依赖 `MonoBehaviour.Update()`。因此：

- 输入采集应在 Unity 主线程执行。
- 页面回调也应视为在 Unity 主线程执行。
- 不应从后台线程直接调用 `InputStack<TKey>.Dispatch()`，除非额外增加线程安全和主线程派发机制。

### 输入宏

旧版输入和新输入系统可以通过宏隔离：

```csharp
#if ENABLE_LEGACY_INPUT_MANAGER
// UnityEngine.Input
#elif ENABLE_INPUT_SYSTEM
// UnityEngine.InputSystem
#endif
```

如果项目设置为 `Both`，两个宏都会生效。应避免两个适配器同时对同一个逻辑动作派发重复事件。

### 当前事件模型的边界

`InputEvent<TKey>` 当前只包含：

- 按钮；
- 单轴数值；
- 二维向量；
- 输入状态。

暂未包含：

- 鼠标或触摸指针坐标；
- Pointer Down / Up；
- 滚轮；
- 文本输入；
- 输入设备类型；
- 修饰键；
- 多指触控信息。

如需支持这些输入，需要扩展 `InputEventType` 和 `InputEvent<TKey>`，并同步修改两个输入后端的转换逻辑。

---

## 6. 推荐使用场景

### UI 页面独占输入

例如打开背包、设置、暂停菜单时，将新页面压入栈顶，使输入只作用于当前 UI。

### 多层弹窗

例如：

```text
主界面
└── 背包界面
    └── 确认弹窗
```

确认弹窗打开后压入栈顶，可以阻止背包界面响应输入。

### UI 导航

可以将确认、取消、上下移动等逻辑动作转换为统一的 `InputEvent<TKey>`，页面无需关心输入来自键盘、手柄还是触摸设备。

### 输入后端替换

旧版 `Input` 和 Unity Input System 只需要分别实现适配层，页面代码继续使用相同的 `InputEvent<TKey>` 和 `InputStack<TKey>`。

### 临时输入屏蔽

可以压入一个没有监听器的 `InputPage<TKey>`，暂时阻止下层 UI 收到输入；操作完成后再弹出该页面。

当前系统不推荐直接用于：

- 需要多个页面同时接收同一输入的广播场景；
- 需要输入事件向下层页面冒泡的场景；
- 需要后台线程处理输入的场景；
- 需要复杂手势、文本输入或多指触摸的场景。

这些场景需要在当前栈模型之上增加事件传播策略、输入类型扩展或主线程消息队列。