---
name: comments-it
argument-hint: '<file-or-symbol>'
description: 'Use when: 为代码 public 类、struct、字段、属性、方法生成或补全中英文 XML 注释；generate bilingual XML documentation comments for public classes, structs, fields, properties, and methods with Chinese first and English second.'
user-invocable: true
disable-model-invocation: false
---

# XML Code Comments

## Goal

为代码中的 public API 生成准确、简洁、可维护的 XML 注释。

Generate accurate, concise, maintainable XML documentation comments for public APIs.

## When to Use

Use this skill when the user asks to:

- 给代码添加 XML 注释。
- 为 public 类、struct、字段、属性、方法补充注释。
- 为 Unity/C# 代码生成中英文说明。
- 检查或改写已有 XML 注释。
- 统一注释格式为“中文在前，英文在后”。
- Generate XML comments for public classes, structs, fields, properties, and methods.
- Add bilingual Chinese/English documentation comments to C# code.

## Scope

Only add or update XML comments for public API unless the user explicitly requests otherwise:

- `public class`
- `public struct`
- `public interface`
- `public enum`
- `public field`
- `public property`
- `public method`
- `public constructor`
- `public event`
- `public delegate`

Do not add XML comments to private, protected, internal, or local members unless explicitly requested.

## Required Format

All XML comments must put Chinese first and English second.

### Summary Format

```csharp
/// <summary>
/// 中文说明。
/// English description.
/// </summary>
```

### Parameter Format

```csharp
/// <param name="value">中文参数说明。English parameter description.</param>
```

### Return Format

```csharp
/// <returns>中文返回值说明。English return value description.</returns>
```

### Type Parameter Format

```csharp
/// <typeparam name="T">中文泛型类型说明。English generic type description.</typeparam>
```

### Exception Format

Only add `<exception>` when the code explicitly throws the exception or the behavior is obvious from the implementation.

```csharp
/// <exception cref="ArgumentNullException">当参数为空时抛出。Thrown when the argument is null.</exception>
```

## Style Rules

1. 中文必须在英文前面。
2. 注释必须描述“做什么”和“什么时候用”，不要逐行复述代码。
3. 保持简洁，避免过度解释。
4. 保留代码标识符原文，不翻译类名、方法名、参数名、字段名。
5. 不要编造代码中不存在的行为。
6. 如果行为无法从代码确定，使用保守描述。
7. 对 Unity API，说明世界坐标、本地坐标、生命周期、运行时/编辑器差异等直接相关约束。
8. 对性能敏感 API，必要时说明分配、递归、延迟销毁、平方距离等注意点。
9. 不改变原有代码逻辑，除非用户明确要求。
10. 不为显而易见的 private 实现细节添加注释。

## Procedure

1. Identify the target file, selected code, or mentioned symbol.
2. Read enough surrounding code to understand the API purpose and dependencies.
3. Find public classes, structs, fields, properties, methods, constructors, events, delegates, and enums that lack XML comments or have incomplete comments.
4. Add or update XML comments using the required Chinese-first English-second format.
5. Preserve existing correct comments when possible.
6. If an existing comment is inaccurate, update it to match the implementation.
7. Avoid changing formatting unrelated to comments.
8. Validate the edited file for syntax errors.

## Examples

### Public Method

```csharp
/// <summary>
/// 判断当前位置是否在目标位置的指定范围内。
/// Checks whether the current position is within the specified range from the target position.
/// </summary>
/// <param name="position">当前位置。Current position.</param>
/// <param name="target">目标位置。Target position.</param>
/// <param name="range">检测范围半径。Detection range radius.</param>
/// <returns>如果在范围内则返回 true，否则返回 false。Returns true if within range; otherwise, false.</returns>
public static bool IsInRange(Vector3 position, Vector3 target, float range)
```

### Public Class

```csharp
/// <summary>
/// 提供 Transform 相关的扩展方法。
/// Provides extension methods for Transform.
/// </summary>
public static class TransformExtension
```

### Public Property

```csharp
/// <summary>
/// 获取当前配置是否启用。
/// Gets whether the current configuration is enabled.
/// </summary>
public bool IsEnabled { get; }
```

### Public Field

```csharp
/// <summary>
/// 默认移动速度。
/// Default movement speed.
/// </summary>
public float defaultMoveSpeed;
```

## Quality Checklist

Before finishing, verify that:

- All requested public APIs have XML comments.
- Chinese appears before English in every XML comment.
- `<param>` names exactly match method parameters.
- `<typeparam>` names exactly match generic type parameters.
- `<returns>` exists for non-void methods and properties only when appropriate.
- Comments match actual behavior.
- No unrelated code logic was changed.
- The file still compiles or has no new syntax errors.
