---
name: write-doc
argument-hint: '<file-or-symbol> [target-audience]'
description: 'Use when: 生成中文代码文档、编写功能说明、API文档、模块文档、Unity/C#代码说明。 writing Chinese Markdown documentation for code features, APIs, modules, components, systems, or gameplay logic. Produces Chinese feature explanations, usage notes, code examples, edge cases, and integration guidance from source code.'
user-invocable: true
disable-model-invocation: false
---

# Code Feature Documentation

## Goal

生成中文 Markdown 文档，说明现有代码功能，包括功能作用、使用场景、工作原理和实际代码示例。
生成的文档行文和排版风格应与现有项目文档保持一致。
Create clear Chinese Markdown documentation for an existing code feature, including what it does, when to use it, how it works, and practical code examples.

Use this skill when the user asks to:
- Write docs for a class, struct, interface, method, component, system, module, or feature.
- Explain code behavior in Markdown.
- Generate usage examples for existing code.
- Create developer-facing documentation from source code.
- Document Unity, C#, ECS, gameplay, networking, asset, config, or tool code.

## Required Output

Generate a Chinese `.md` document with this structure unless the user requests a different format:

```markdown
# <Feature Name>

## Overview

Briefly describe what the feature does and why it exists.

## Core Concepts

Explain the important concepts, data structures, dependencies, and execution flow.

## API / Fields / Methods

| Name | Type | Description |
|---|---|---|
| `<name>` | `<type>` | `<meaning and usage>` |

## How It Works

Describe the runtime behavior step by step.

## Usage Example

Provide a realistic, compilable or near-compilable code example.

## Common Use Cases

- Use case 1
- Use case 2

## Edge Cases and Notes

- Important constraints
- Null/default handling
- Performance notes
- Threading/ECS/world lifecycle notes if relevant

## Procedure

1. Identify the feature target from the user request, current editor file, selected code, or mentioned symbol.
2. Read the source code and nearby related files if needed.
3. Determine:
   - Feature purpose
   - Public API or important fields
   - Data flow and control flow
   - Dependencies and related systems
   - Expected usage pattern
4. Write the documentation in concise technical Chinese. The final `.md` content must be Chinese unless the user explicitly requests another language.
5. Keep symbol names, file names, namespaces, and code identifiers unchanged.
6. Prefer examples that match the existing project style instead of generic examples.
7. If code behavior is uncertain, state the uncertainty explicitly instead of inventing details.
8. If the user asks to save the document, create or update a Markdown file in the requested path. If no path is given, suggest a path before editing unless the intended location is obvious.

## Code Example Rules

- Use fenced code blocks with the correct language tag, for example `csharp`.
- Examples should be minimal but realistic.
- Do not introduce APIs that do not exist in the project unless clearly marked as pseudo-code.
- Prefer existing constructors, factories, systems, components, and naming conventions.
- For Unity/C# code:
  - Mention Unity lifecycle constraints where relevant.
  - Mention ECS/world/component ownership where relevant.
  - Avoid unnecessary allocations in examples when documenting performance-sensitive systems.

## Documentation Style

- Use Chinese for headings, paragraphs, tables, notes, and explanations by default.
- Keep code identifiers, file paths, namespaces, class names, method names, field names, and enum values in their original language.
- Use Markdown headings and tables.
- Prefer short paragraphs and bullet lists.
- Be accurate over exhaustive.
- Explain intent before implementation details.
- Include warnings only when they are directly supported by the code.
- Do not expose secrets, local machine paths, or unrelated implementation details.

## Quality Checklist

Before finishing, verify that:
- The documented feature name matches the code.
- All referenced symbols exist or are clearly marked as pseudo-code.
- The usage example is consistent with the source code.
- Important edge cases are covered.
- The Markdown is readable and can be copied directly into project docs.
