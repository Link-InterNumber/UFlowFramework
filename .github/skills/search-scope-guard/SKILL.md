---
name: search-scope-guard
argument-hint: '<task>'
description: 'Use when: 约束 agent 查找功能代码的范围。Default to the current open file and user-provided context; search the whole project only when the user explicitly requests whole-project scope.'
user-invocable: true
disable-model-invocation: false
---

# Search Scope Guard

## Goal

规范 agent 在查找功能代码、理解行为、定位调用链时的检索范围。

默认规则：
- 只使用当前打开的文件、用户附带的上下文、以及用户在问题中直接提供的代码片段。
- 不主动检索整个项目。
- 不因为“可能相关”就扩大到全局搜索。

仅当用户明确要求以下内容之一时，才允许对整个项目进行检索：
- “搜索整个项目”
- “全局查找”
- “在所有文件里找”
- “请扫描仓库”
- 等价的明确全项目范围要求

## Scope Rules

### Default Scope

When the user asks to locate or understand feature code, the agent must:
1. Inspect the currently open file first.
2. Read the user-provided attachments and context first.
3. Search only within those materials unless they are insufficient.
4. Prefer exact symbol lookup inside the open file or attached snippets before any broader search.

### Escalation Rules

The agent may expand scope only in this order:
1. Current open file
2. User attachments and quoted code
3. Nearby related files explicitly mentioned by the user or clearly referenced in the current file
4. Whole-project search only if the user explicitly requests it

### Do Not

- Do not start with a workspace-wide search when the user did not ask for it.
- Do not use semantic search across the whole repository as the first step.
- Do not infer that “find the implementation” means “search the entire project”.
- Do not browse unrelated modules just because the codebase is large.

## Procedure

1. Read the current file or the user-provided code context first.
2. Extract the exact symbol, function name, class name, enum value, or config key from the request.
3. Search only the currently visible context for that symbol.
4. If the answer is not present, check only directly related nearby files.
5. If and only if the user explicitly asked for full-project search, expand to the whole workspace.
6. Report the result with the scope used so the user knows whether the answer is local or global.

## Examples

### Correct behavior

- User: “这个函数为什么返回 false？”
  - First inspect the open file.
  - Use attached code to infer behavior.
  - Avoid whole-project search unless needed.

- User: “请在整个项目里找所有 `GetQuestSetState` 的调用。”
  - Whole-project search is allowed.

### Incorrect behavior

- User only provides one file and asks about a method.
  - Wrong: scanning the whole repo immediately.

## Output Guidance

When following this skill, keep the investigation concise:
- Prefer local evidence over global evidence.
- State when the answer comes only from the current file or attachments.
- If global search was required, explain why it was necessary.

## Quality Checklist

Before finishing, verify that:
- The search scope matched the user’s request.
- Whole-project search was only used when explicitly requested.
- The answer is grounded in the current file or provided context whenever possible.
- Unnecessary repository-wide exploration was avoided.

