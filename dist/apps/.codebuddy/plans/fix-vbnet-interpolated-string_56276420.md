---
name: fix-vbnet-interpolated-string
overview: 修复 VB.NET 语法高亮器（VbNetHighlighter）对字符串插值语法 $"..." 的解析错误：当前扫描逻辑只寻找闭合引号、不跟踪 { } 嵌套深度，导致插值表达式内的引号误触发字符串提前闭合或后续内容被吞入字符串。引入花括号深度跟踪，仅在深度为 0 时遇到的非转义引号才视作插值字符串真正闭合符，并同步修正跨行续行逻辑（区分 interp 与普通字符串）。
todos:
  - id: vb-inspect-interp
    content: 审查 VbNetHighlighter 插值/续行/普通字符串三处分支并定位误闭合点
    status: completed
  - id: vb-add-scan-helper
    content: 抽取带 interp 与花括号深度感知的字符串扫描辅助逻辑
    status: completed
    dependencies:
      - vb-inspect-interp
  - id: vb-fix-interp-branch
    content: 用辅助逻辑重写插值字符串分支，支持 {expr} 与 {{}} 字面量
    status: completed
    dependencies:
      - vb-add-scan-helper
  - id: vb-fix-continuation
    content: 修正跨行续行块在 state.interp 时启用深度感知扫描
    status: completed
    dependencies:
      - vb-add-scan-helper
  - id: vb-build-verify
    content: tsc 类型检查并以自测脚本验证插值用例，清理临时文件
    status: completed
    dependencies:
      - vb-fix-interp-branch
      - vb-fix-continuation
---

## 用户需求

上一轮已修复 VB.NET 多行普通字符串常量高亮，但字符串插值语法（`$"..."` / `$@"..."`）仍解析错误：当插值字符串内部含有嵌套的结构（如表达式中的字符串字面量 `"..."`、字面量花括号 `{{`/`}}`、大括号嵌套）时，扫描逻辑无法正确识别，导致字符串提前或错位闭合，使闭合符之后的全部内容被错误地解析为字符串的一部分。要求审查 `src/highlighters/VbNetHighlighter.ts` 并修复该问题。

## 产品概述

代码编辑器 VB.NET 语法高亮器在渲染包含插值表达式的插值字符串时，因扫描逻辑未跟踪花括号嵌套深度与大括号转义，将表达式内部（或字面量花括号之后的）引号误判为字符串闭合符，造成后续整段代码被吞入字符串高亮。需引入"插值字符串花括号深度感知"扫描逻辑修复。

## 核心功能

- 插值字符串 `$"..."` / `$@"..."` 内部正确识别 `{expr}` 表达式占位，表达式内出现的引号不触发字符串闭合。
- 正确识别字面量花括号转义 `{{` 与 `}}`（视作字面量，不增减深度）。
- 支持表达式内再嵌字符串字面量（如 `$"a { "inner" } b"`），不误闭合。
- 多行插值字符串跨行延续时，续行扫描同样具备花括号深度感知；闭合后后续代码恢复为普通高亮。
- 保持普通字符串、char literal `"a"c`、注释、关键字、数字、运算符等现有高亮逻辑完全不变。

## 技术栈

- 语言：TypeScript（命名空间风格，全局 `CodeEditor.Highlighters` / `Utils`）。
- 构建：esbuild（`build.cmd`、项目 `package.json`）；类型检查用 `tsc --noEmit`。
- 高亮架构：逐行 tokenizer，接口 `ILanguageHighlighter.tokenizeLine(line, state): TokenizeResult`，`initialState()` 初始化状态；`src/core/Highlighter.ts` 已正确按行缓存并跨行传递 `state`（无需改动）。

## 实现方案

采用"统一字符串扫描 + 插值花括号深度感知"方案，复用现有 `state.inString/interp` 状态机（上一轮已建立），不引入新架构、不改接口与其他模块。

核心思路：将"在字符串内扫描到闭合引号"的逻辑抽象为一个带 `interp` 标志的本地辅助函数（方法或闭包内 helper），使普通字符串与插值字符串、单行与跨行续行共用同一套闭合判定规则：

1. **辅助扫描逻辑 `scanString(i, interp)`**：从位置 `i` 开始扫描到 `n`，返回闭合位置 `j` 与是否闭合 `closed`：

- 若 `interp` 为真：维护花括号深度 `depth`；遇 `{{` 或 `}}` 跳两字符（字面量花括号）；遇 `{`（非 `{{`）`depth++`；遇 `}`（非 `}}`）`depth--`；仅当 `depth===0` 且遇单个 `"`（且下一字符非 `"`，即非 `""` 转义）才 `j++; closed=true; break`；遇 `""` 转义跳两字符。
- 若 `interp` 为假（普通字符串）：遇 `""` 跳两字符，遇单个 `"` 闭合。

2. **插值字符串分支（第 174-195 行）**：改用 `scanString(i+1+startChar, true)`；行尾未闭合则携带 `inString=true, interp=true` 跨行返回。
3. **跨行续行块（第 74-97 行）**：若 `state.interp` 为真，用 `scanString(0, true)`（带深度跟踪）；否则用 `scanString(0, false)`（原普通规则）；闭合则清空状态返回，否则整行作 String 保持状态返回。
4. **普通字符串分支（第 135-172 行）**：保持 `interp=false`，使用 `scanString` 的普通规则（逻辑等价，仅 `"`/`""`），确保 char literal `"a"c` 判定不变。
5. **转义约定**：VB.NET 字符串转义仍为 `""`（双写引号）；逐字插值 `$@"` 转义规则与 `$"` 一致；`\r` 行尾在续行扫描中自然包容（扫描按字符线性推进，遇行尾即停）。
6. 其余分支（注释 `'`、REM、预处理 `#`、数字、关键字、运算符）完全不变。

## 实现要点

- 不改动接口签名、`Tokenizer.ts`、`Highlighter.ts`、其他 highlighter。
- 复用 `TokenBuilder.push(TokenType.String, ...)`，不新增 `TokenType`。
- 性能：扫描仍为 O(n) 单遍；新增的 `depth` 计数仅在内层按需增减，无额外开销；跨行续行按字符线性扫描。
- 回归防护：普通字符串与 char literal 行为与原实现保持一致（仅重构为共用 helper）；仅在插值与续行分支插入 `interp`/深度逻辑；可借助上一轮的逐行状态传递自测脚本回归验证。
- 日志：不新增日志，保持现有风格。

## 架构设计

`VbNetHighlighter` 为纯函数式逐行 tokenizer，本次仅在其内部引入"插值字符串花括号深度感知扫描"并与既有跨行状态机整合，不改变模块边界与数据流。状态在 `Highlighter.ts` 逐行缓存中自然传递，与兄弟 highlighter 模式一致。

## 目录结构

```
src/highlighters/
└── VbNetHighlighter.ts   # [MODIFY] 抽取带 interp/花括号深度感知的字符串扫描辅助逻辑；修正插值字符串分支使用 interp=true 扫描；修正跨行续行块在 state.interp 为真时用深度感知扫描；普通字符串与 char literal 逻辑保持不变。其余文件不改动。
```

## 关键代码结构（可选）

辅助扫描逻辑（方法内闭包或私有静态辅助），语义约定如下（仅接口级，不写实现体）：

```ts
// 返回 [endIndex, closed]
// interp=true 时跟踪花括号深度：depth===0 且仅单引号 " 才闭合；{{/}} 为字面量；"" 为转义
// interp=false 时仅按 "" 转义与单引号 " 闭合
private scanStringBody(line: string, start: number, interp: boolean): [number, boolean]
```

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 在修复实现前对 VbNetHighlighter 插值字符串、跨行续行块与普通字符串分支做最终精确核验，确认 `interp` 状态字段、花括号深度跟踪插入点与现有 char literal 判定互不冲突。
- Expected outcome: 明确插值字符串在单行/跨行场景下的精确闭合规则与 `{{`/`}}` 转义处理，为统一扫描 helper 的正确实现提供依据，避免回归。