---
name: check-it
argument-hint: '<file-or-symbol>'
description: 'Use when: 检查脚本功能是否达成目标，是否存在明显逻辑错误、运行时报错风险、严重性能或内存占用隐患；检查后不修改脚本，只返回问题位置与完善建议。 Review scripts for goal fit, obvious logic bugs, runtime error risks, and serious performance/memory hazards without editing the code.'
user-invocable: true
disable-model-invocation: false
---

# Script Audit

## Goal

检查脚本是否真正达到预期功能，是否存在明显的逻辑错误、运行时报错风险、严重性能问题或内存占用隐患。

Audit scripts for functional correctness, obvious logic defects, runtime error risks, and serious performance/memory hazards.

## When to Use

Use this skill when the user asks to:

- 检查某个脚本、类、方法或模块是否实现了目标。
- 排查明显的逻辑错误、空引用、越界、死循环、错误条件判断、状态机问题。
- 识别明显的运行时报错隐患，但**不修改代码**。
- 排查严重的性能瓶颈、重复分配、明显的 GC 压力、异常的内存增长风险。
- 只要问题定位和改进建议，不要直接改脚本。

## Core Principles

1. **只检查，不修改**
   - 不要编辑、重构或自动修复脚本。
   - 只输出问题位置、原因、风险等级和建议。

2. **以明显问题为主**
   - 优先报告高概率、可确认的问题。
   - 不要把纯猜测、风格偏好或很弱的推测当成错误。

3. **聚焦可运行性与目标达成**
   - 判断脚本是否能达到预期功能。
   - 重点关注会导致运行异常、逻辑失效、状态错乱的问题。

4. **聚焦严重性能与内存隐患**
   - 关注重复分配、频繁 LINQ、循环内创建对象、非必要的集合重建、事件泄漏、对象生命周期问题。
   - 只报告“严重”或“明显”的风险，不放大微不足道的问题。

5. **输出具体可定位**
   - 给出文件名、方法名、关键行号或代码片段位置。
   - 说明问题原因、影响范围和建议方向。

## Audit Procedure

1. 读取目标脚本和必要上下文。
2. 判断脚本功能目标是否明确。
3. 检查是否存在：
   - 明显的逻辑错误
   - 空引用或越界风险
   - 条件判断错误
   - 状态未同步
   - 生命周期错误
   - 严重性能/内存问题
4. 评估问题是否足以影响运行结果或稳定性。
5. 输出问题清单和完善建议，不要修改原脚本。

## Review Focus

### Correctness

- 条件分支是否遗漏。
- 变量是否可能未初始化。
- 对象是否可能为空。
- 集合访问是否可能越界。
- 状态切换、打开关闭、增删改流程是否对称。

### Runtime Risk

- 可能直接抛异常的位置。
- 递归、循环、协程、事件回调是否可能失控。
- 资源释放、订阅取消、对象销毁是否完整。

### Performance / Memory

- 热路径中是否存在明显重复查找或重复分配。
- 是否存在大对象、临时集合、闭包、装箱、频繁字符串拼接。
- 是否存在明显的对象持有、缓存失效或泄漏风险。

## Safety Rules

- 不要修改任何脚本文件。
- 不要给出伪装成修复后的代码补丁。
- 不要把纯优化风格差异误判为错误。
- 不要为了凑问题数量而扩大解释。
- 如果问题不确定，要明确标注为“需进一步确认”，不要当作已确认错误。

## Output Expectations

When this skill is used, prefer to provide:

- 问题列表，按严重程度排序。
- 每个问题包含：位置、问题类型、原因、潜在后果、建议方向。
- 如果脚本整体没有明显问题，明确说明“未发现明显运行风险”。

## Suggested Output Format

1. **结论**
   - 是否达到目标。

2. **问题清单**
   - `文件/方法/行号`
   - 问题描述
   - 影响
   - 建议

3. **补充建议**
   - 性能、内存、可维护性方面的进一步检查点。

## Checklist

Before finishing, verify that:

- 没有修改脚本内容。
- 所有问题都尽量定位到具体位置。
- 只报告明显或高风险问题。
- 性能/内存风险只保留严重项。
- 建议清晰、可执行、不过度展开。