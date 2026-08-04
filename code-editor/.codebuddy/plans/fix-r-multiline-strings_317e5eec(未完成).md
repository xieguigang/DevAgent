---
name: fix-r-multiline-strings
overview: 修复 R 语言语法高亮对多行字符串（单引号 '...'、双引号 "..."）的解析错误。当前 RHighlighter 逐行处理且忽略了跨行状态，导致未闭合字符串的续行被错误高亮。引入与 Xml/Json/VbNet 一致的 inString 状态机，使字符串跨行正确延续。
todos:
  - id: verify-state-chain
    content: 用 [subagent:code-explorer] 核验 Highlighter 的 state 跨行传递与 line 换行符细节
    status: pending
  - id: add-string-state
    content: 修改 RHighlighter.initialState 增加 inString/stringChar/rawClose 状态
    status: pending
    dependencies:
      - verify-state-chain
  - id: impl-continuation
    content: 在 tokenizeLine 起始实现续行字符串扫描与转义、\r 处理
    status: pending
    dependencies:
      - add-string-state
  - id: fix-unclosed
    content: 修改单/双引号与 raw string 扫描使其未闭合时携带状态跨行
    status: pending
    dependencies:
      - add-string-state
  - id: build-verify
    content: 运行 build.cmd 构建并自检多行字符串高亮用例
    status: pending
    dependencies:
      - impl-continuation
      - fix-unclosed
---

## 用户需求

修复代码编辑器 R 语言语法高亮模块（`RHighlighter`）对多行字符串解析错误的 bug，使跨行字符串内容被正确高亮为字符串，且不影响后续代码的高亮。

## 产品概述

代码编辑器内置的 R 语言语法高亮器在渲染包含多行字符串（普通单/双引号字符串、R 原始字符串 `r"(...)"` 等）的 R 代码时，续行内容被错误识别为关键字、注释等普通代码，导致整段高亮错乱。需通过跨行状态机修复该问题。

## 核心功能

- 支持普通双引号字符串 `\"...\"` 跨行延续，续行整段作为字符串高亮。
- 支持普通单引号字符串 `'...'` 跨行延续。
- 支持 R 原始字符串 `r"(...)"` / `r"[...]"` / `r"{...}"` 跨多行延续，直到出现对应 `)"` / `]"` / `}"` 闭合。
- 正确处理字符串内的转义引号（`\""`、`\'`），转义引号不触发跨行闭合。
- 保持注释、关键字、数字、运算符、函数调用检测等现有高亮逻辑不变。

## 技术栈

- 语言：TypeScript（命名空间风格，全局 `CodeEditor.Highlighters` / `Utils`）。
- 构建：esbuild（`build.cmd`、项目 `package.json`）。
- 高亮架构：逐行 tokenizer，接口 `ILanguageHighlighter.tokenizeLine(line, state): TokenizeResult`，`initialState()` 初始化状态。核心调用方 `src/core/Highlighter.ts` 按行缓存 token，并将 state 跨行传递。

## 实现方案

采用与现有兄弟实现（`JsonHighlighter`、`VbNetHighlighter` 的 `inString + stringChar` 状态机，`XmlHighlighter` 的跨行 state 传递）完全一致的「状态驱动」方案，而非引入新架构。

核心思路：让 `state` 携带未闭合字符串的上下文，使字符串跨行延续：

1. `initialState()` 返回 `{ inString: false, stringChar: "", rawClose: "" }`（向后兼容现有空对象，缺失字段按 falsy 处理）。
2. 在 `tokenizeLine` 主循环开始前，若 `state.inString` 为真：

- 将本行视为字符串延续，扫描直到出现与 `stringChar` 匹配的闭合引号（闭合前若是 `\` 转义则跳过下一个字符；`rawClose `非空时须匹配 `rawClose + stringChar` 才算闭合），输出 String token。
- 找到闭合则清除 inString/stringChar/rawClose；否则整行作为 String 输出并保持 inString 状态。

3. 现有单/双引号字符串扫描：若行内扫描到行尾仍未闭合，输出已扫描部分并设 `state.inString=true, stringChar=ch`，返回携带新 state 的结果（而非当作已闭合）。
4. 现有 raw string 扫描：若行内扫描到行尾未发现 `close + '"'`，输出已扫描部分并设 `state.inString=true, stringChar='"', rawClose=close`，下行动态以 `rawClose + '"'` 作为闭合判定（复用第 2 点的逻辑）。
5. 行尾 `\r` 处理：在续行闭合判定与跳过逻辑中，将 `\r` 视为行尾空白忽略，避免 Windows 行尾导致误判。

## 实现要点

- 不改动接口签名、其他 highlighter 或核心调用方。
- 复用现有 `TokenBuilder.push(TokenType.String, ...)`，不新增 token 类型。
- 性能：`tokenizeLine` 仍为 O(n) 单遍扫描，仅增加一个 state 分支，无额外开销；跨行字符串续行按字符线性扫描，复杂度不变。
- 回归防护：保持注释 `#`、roxygen `#'`、关键字/常量/原语、数字、运算符、`[[ ]]`、函数调用检测的已有逻辑完整不变；仅在字符串相关分支插入状态读写。
- 日志/可观测性：无需新增日志，保持现有风格；若调试可临时沿用 `[debug]` 前缀约定，不提交调试日志。

## 架构设计

RHighlighter 为纯函数式逐行 tokenizer，本次仅在其内部引入「跨行字符串状态机」，不改变模块边界与数据流。状态在 `Highlighter.ts` 的逐行缓存中自然传递，与兄弟 highlighter 模式一致。

## 目录结构

```
src/highlighters/
└── RHighlighter.ts   # [MODIFY] 引入跨行字符串状态机：修改 initialState() 返回字符串状态；在 tokenizeLine 起始处处理续行；修改单/双引号与 raw string 扫描逻辑，未闭合时携带 state.inString/stringChar/rawClose 跨行；正确处理转义与 \r。其余高亮逻辑保持不变。其余文件（Tokenizer.ts 接口、Highlighter.ts、其他 highlighter）不改动。
```

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 在实现前对 RHighlighter、Tokenizer 接口及 Highlighter 调用链做最终精确核验，确认 state 传递契约与行尾换行处理细节。
- Expected outcome: 明确 tokenizeLine 的 state 跨行传递路径与 line 参数是否含 `\r`，为状态机实现提供精确依据，避免回归。