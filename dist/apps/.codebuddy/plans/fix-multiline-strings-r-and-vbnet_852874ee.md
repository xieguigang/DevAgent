---
name: fix-multiline-strings-r-and-vbnet
overview: 修复 R 语言与 VB.NET 语言语法高亮对多行字符串的解析错误。两者都因 tokenizeLine 未利用跨行 state，导致未闭合字符串的续行被当作普通代码错误高亮。已确认 Highlighter.ts 框架正确传递 state，问题在 highlighter 内部。将分别为两者引入状态驱动的跨行字符串状态机（R：单/双引号与 raw string r"(...)"; VB.NET：双引号字符串 "" 转义、插值 $"..."、char literal "a"c）。仅修改对应 highlighter 文件。
todos:
  - id: verify-state-chain
    content: 用 [subagent:code-explorer] 核验 Highlighter 的 state 跨行传递与 line 换行符细节
    status: completed
  - id: r-add-string-state
    content: 修改 RHighlighter.initialState 增加 inString/stringChar/rawClose 状态
    status: completed
    dependencies:
      - verify-state-chain
  - id: r-impl-continuation
    content: 在 R tokenizeLine 起始实现续行字符串扫描与转义、\r 处理
    status: completed
    dependencies:
      - r-add-string-state
  - id: r-fix-unclosed
    content: 修改 R 单/双引号与 raw string 扫描使其未闭合时携带状态跨行
    status: completed
    dependencies:
      - r-add-string-state
  - id: vb-add-string-state
    content: 复用 VbNetHighlighter inString 并增加 stringChar/interp 状态
    status: completed
    dependencies:
      - verify-state-chain
  - id: vb-impl-continuation
    content: 在 VB tokenizeLine 起始实现字符串续行扫描与 "" 转义、\r 处理
    status: completed
    dependencies:
      - vb-add-string-state
  - id: vb-fix-unclosed
    content: 修改 VB 普通与插值字符串扫描使其未闭合时携带状态跨行，完善 char literal
    status: completed
    dependencies:
      - vb-add-string-state
  - id: build-verify
    content: 运行 build.cmd 构建并自检 R 与 VB.NET 多行字符串高亮用例
    status: completed
    dependencies:
      - r-impl-continuation
      - r-fix-unclosed
      - vb-impl-continuation
      - vb-fix-unclosed
---

## 用户需求

修复代码编辑器 R 语言与 VB.NET 语言语法高亮模块对多行字符串解析错误的 bug，使跨行字符串内容被正确高亮为字符串，且不影响后续代码高亮。

## 产品概述

代码编辑器内置的 R 与 VB.NET 语法高亮器在渲染包含多行字符串的源码时，续行内容被错误识别为关键字、注释等普通代码，导致整段高亮错乱。需通过跨行状态机修复该问题。

## 核心功能

- R：支持普通双引号、单引号字符串跨行延续，续行整段作为字符串高亮。
- R：支持原始字符串 r"(...)" / r"[...]" / r"{...}" 跨多行延续，直到出现对应 )" / ]" / }" 闭合。
- R：正确处理字符串内转义引号（\"、\'），转义引号不触发跨行闭合。
- VB.NET：支持普通双引号字符串跨行延续，转义为双写 "" 时正确跳过。
- VB.NET：支持插值字符串 $"..." 跨行延续。
- VB.NET：完善 char literal "a"c 的单行判定，不与多行字符串混淆。
- 两者均保持注释、关键字、数字、运算符、函数调用检测等现有高亮逻辑不变。

## 技术栈

- 语言：TypeScript（命名空间风格，全局 CodeEditor.Highlighters / Utils）。
- 构建：esbuild（build.cmd、项目 package.json）。
- 高亮架构：逐行 tokenizer，接口 ILanguageHighlighter.tokenizeLine(line, state): TokenizeResult，initialState() 初始化状态。核心调用方 src/core/Highlighter.ts 按行缓存 token 并将 state 跨行传递。

## 实现方案

采用与现有兄弟实现（JsonHighlighter、VbNetHighlighter 的 inString + stringChar 状态机，XmlHighlighter 的跨行 state 传递）一致的「状态驱动」方案，不引入新架构。

核心思路：让 state 携带未闭合字符串的上下文，使字符串跨行延续。已核验 Highlighter.retokenize（第 80-82 行）正确将 result.state 传给下一行 tokenizeLine 并缓存——框架跨行能力可用，问题纯在 highlighter 内部未使用 state。

### RHighlighter 修复（src/highlighters/RHighlighter.ts）

1. initialState() 返回 { inString: false, stringChar: "", rawClose: "" }（缺失字段按 falsy 处理，向后兼容）。
2. tokenizeLine 起始：若 state.inString 为真，本行视为字符串延续，扫描直到匹配 stringChar 的闭合引号（rawClose 非空时须匹配 rawClose+stringChar 才算闭合；闭合前若为 \ 转义则跳过下一字符，兼容 \r 行尾），输出 String token；找到则清状态，否则整行作 String 并保持 inString。
3. 单/双引号字符串：行内扫描到行尾仍未闭合，输出已扫描部分并设 state.inString=true, stringChar=ch，返回携带新 state（而非当已闭合）。
4. raw string r"(...)/r"[...]/r"{...}：行内未出现 close+'"'，输出已扫部分并设 state.inString=true, stringChar='"', rawClose=close，续行复用第 2 点。
5. 其余高亮逻辑（注释、roxygen、关键字/常量/原语、数字、运算符、[[ ]]、函数调用检测）完全不变。

### VbNetHighlighter 修复（src/highlighters/VbNetHighlighter.ts）

1. 复用已声明的 inString/stringDepth 状态，新增 stringChar/interp 字段区分普通与插值字符串（向后兼容现有 initialState 结构）。
2. tokenizeLine 起始：若 state.inString 为真，本行视为字符串延续。VB.NET 转义为双写 ""，扫描直到出现非转义（奇数收尾）的单个 " 闭合，正确跳过 "" 转义对，输出 String token；找到则清状态，否则整行作 String 并保持 inString（兼容 \r 行尾）。
3. 普通字符串块（111-132）：行内扫描到行尾未闭合，输出已扫部分并设 state.inString=true, stringChar='"'，返回携带新 state。
4. 插值字符串 $"..."（135-153）：同样支持多行——未闭合时携带 inString 状态，interp 标记；仍输出 TokenType.String。
5. char literal "a"c：完善判定（紧跟非空白 " 后的 c 作为 char literal，单行内闭合，不影响跨行）。
6. 其余（注释 '、REM、预处理 #、数字、关键字、运算符等）逻辑不变。

## 实现要点

- 不改动接口签名、Tokenizer.ts、Highlighter.ts、其他 highlighter。
- 复用现有 TokenBuilder.push(TokenType.String, ...)，不新增 TokenType。
- 性能：tokenizeLine 仍为 O(n) 单遍扫描，仅增加 state 分支与必要续行扫描，无额外开销。
- 回归防护：保持所有非字符串分支逻辑完整不变；仅在字符串相关分支插入状态读写；R 与 VB.NET 修复相互独立。
- 日志：无需新增日志，保持现有风格。

## 架构设计

RHighlighter 与 VbNetHighlighter 均为纯函数式逐行 tokenizer，本次仅在其内部引入「跨行字符串状态机」，不改变模块边界与数据流。状态在 Highlighter.ts 的逐行缓存中自然传递，与兄弟 highlighter 模式一致。

## 目录结构

```
src/highlighters/
├── RHighlighter.ts        # [MODIFY] 引入跨行字符串状态机：修改 initialState 增加 inString/stringChar/rawClose；tokenizeLine 起始处理续行；修改单/双引号与 raw string 扫描，未闭合时携带状态跨行；处理转义与 \r。其余逻辑不变。
└── VbNetHighlighter.ts    # [MODIFY] 复用 inString/stringDepth 并增加 stringChar/interp；tokenizeLine 起始处理字符串续行（"" 转义跳过）；普通与插值字符串未闭合时携带状态跨行；完善 char literal "a"c 单行判定；处理 \r。其余逻辑不变。
```

（其余文件 Tokenizer.ts、Highlighter.ts、其他 highlighter 不改动）

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 在实现 R 与 VB.NET 跨行字符串状态机前，对 VbNetHighlighter、RHighlighter 与 Highlighter 调用链做最终精确核验，确认 state 跨行传递契约与 line 参数是否含 \r。
- Expected outcome: 明确 tokenizeLine 的 state 跨行传递路径与换行符细节，为两个状态机实现提供精确依据，避免回归。