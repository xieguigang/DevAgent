---
name: 修复编辑器点击代码上下乱跳的bug
overview: 修复鼠标点击代码时编辑器上下乱跳的 bug。根因是 textarea 同时被 transform 移到光标行，又通过 selectionStart 把 caret 移到文本第 N 行，导致浏览器 caret 自动滚动把滚动容器滚到 2N 错误位置并触发 scroll→render 循环。方案：封装统一的 selection 设置方法抑制 caret 自动滚动，并阻断 scroll 事件的 render 循环。
todos:
  - id: add-suppress-flag-and-method
    content: 在 Editor.ts 中新增 _suppressCaretScroll 标志位和 setTextareaSelection 统一方法
    status: completed
  - id: fix-scroll-handler
    content: 修改 scroll 事件处理器加入标志位检查阻断 render 循环
    status: completed
    dependencies:
      - add-suppress-flag-and-method
  - id: refactor-all-call-sites
    content: 改造 handleCodeViewClick、handleGutterClick、goToSymbol、goToLineNumber、acceptCompletion、updateCaretFromTextarea、setText 共 7 处 textarea selection 调用点
    status: completed
    dependencies:
      - fix-scroll-handler
  - id: build-and-verify
    content: 运行 build.cmd 重新编译并使用 [subagent:code-explorer] 验证修复完整性
    status: completed
    dependencies:
      - refactor-all-call-sites
---

## 用户需求

用户报告代码编辑器项目存在一个致命的 UI 交互 bug：当用户使用鼠标点击编辑器中的代码时，编辑器会上下乱跳，导致无法正常使用。需要分析 `src/` 文件夹中的源代码，定位 bug 根因并进行修复。

## 产品概述

这是一个基于 TypeScript + DOM 的自定义代码编辑器组件，使用分层渲染架构（gutter 行号区、codeView 代码高亮区、textarea 隐藏输入层、completion 弹窗），嵌入在 WebView2 宿主环境中运行。编辑器通过 textarea 接收键盘输入并原生渲染光标，通过 transform 将 textarea 视觉定位到光标所在行。

## 核心功能（修复范围）

- 修复鼠标点击代码区时编辑器上下乱跳的 bug
- 修复所有涉及设置 textarea selection 时可能触发同样问题的入口点
- 保持键盘输入、符号跳转、行号跳转、补全接受等功能的正常滚动行为

## 技术栈

- 语言：TypeScript（target ES2019，module=none，tsc 编译）
- 构建：tsc 直接编译到 `dist/editor.bundle.js`，通过 `build.cmd` 执行
- 架构：基于命名空间（namespace）的非模块化设计，DOM 分层渲染

## 根因分析

编辑器使用 textarea 作为隐藏输入层，通过 `transform: translate(left, pos.line * lineHeight)` 将其视觉移到光标行（`renderCaret`，Editor.ts:880-887）。当用户点击代码区时，`handleCodeViewClick` 设置 `textarea.selectionStart` 到文本第 N 行位置，触发浏览器原生 caret 自动滚动机制。由于 textarea 已被 transform 移到第 N 行（视觉偏移 N*lineHeight），caret 在文本内又在第 N 行（偏移 N*lineHeight），浏览器计算 caret 视觉位置约为 2N*lineHeight，将 scrollContainer 滚到错误位置。错误滚动触发 scroll 事件 → `render()` → `renderCaret()` 更新 transform → 与异步 caret scrolling 互相干扰 → 形成上下乱跳。

所有设置 `textarea.selectionStart` 的入口点都存在此隐患：`handleCodeViewClick`、`handleGutterClick`、`goToSymbol`、`goToLineNumber`、`acceptCompletion`、`updateCaretFromTextarea`、`setText`。

## 实现方案

### 核心策略：抑制 caret 自动滚动 + 阻断 scroll-render 循环

1. **新增 `_suppressCaretScroll` 标志位**：在设置 textarea selection 期间置为 true
2. **新增 `setTextareaSelection` 统一方法**：保存当前 scrollTop/scrollLeft → 置标志位 → `focus({ preventScroll: true })` + 设置 selectionStart → 立即恢复 scrollTop → `requestAnimationFrame` 内再次恢复并解除标志位
3. **修改 scroll 事件处理器**：检查标志位，若为 true 则跳过 `render()`，阻断循环
4. **改造所有调用点**：将分散的 `this.textarea.focus(); this.textarea.selectionStart = ...` 替换为统一的 `this.setTextareaSelection(pos)`
5. `focus({ preventScroll: true })` 的 FocusOptions 类型在 ES2019 lib 下可能缺失定义，需用 `as any` 或 `as FocusOptions` 类型断言处理

### 性能说明

- `requestAnimationFrame` 确保在浏览器异步 caret scrolling 完成后恢复滚动位置，仅一帧延迟，无性能影响
- scroll 事件处理增加一个布尔判断，开销可忽略
- `render()` 调用频率不变，仅在 caret scrolling 期间被跳过

## 目录结构

```
g:\DevAgent\code-editor\
├── src\
│   └── core\
│       └── Editor.ts    # [MODIFY] 核心修复：新增标志位、统一方法、改造所有 textarea selection 调用点、修改 scroll 事件处理器
└── build.cmd             # 无需修改，修复后运行 tsc 重新编译
```

### Editor.ts 修改详情

- **新增字段**：`private _suppressCaretScroll: boolean = false;`（约第 86 行附近，与其他标志位并列）
- **新增方法**：`private setTextareaSelection(pos: number): void`（约第 267 行前，updateCaretFromTextarea 之前）
- **修改 scroll 处理器**（第 168-171 行）：加入 `if (this._suppressCaretScroll) return;`
- **改造调用点**（7 处）：
- `handleCodeViewClick`（第 353-354 行）
- `handleGutterClick`（第 375-377 行）
- `goToSymbol`（第 502-503 行）
- `goToLineNumber`（第 668-669 行）
- `acceptCompletion`（第 646-647 行）
- `updateCaretFromTextarea`（第 268-269 行）
- `setText`（第 399 行）

## 实现注意事项

- **TypeScript 类型兼容**：`FocusOptions` 接口（含 `preventScroll` 属性）在 ES2019 DOM lib 中已存在，直接使用 `this.textarea.focus({ preventScroll: true })` 即可；若编译报错则降级为 `as any` 断言
- **rAF 恢复顺序**：在 `requestAnimationFrame` 回调中先恢复 `scrollTop`（此时标志位仍为 true，scroll 处理器跳过），再置 `_suppressCaretScroll = false`，避免恢复触发的 scroll 事件引发额外 render
- **功能保留**：`goToSymbol` 和 `goToLineNumber` 中调用 `scrollToLine()` 的逻辑保持不变，因为那是有意的、正确的滚动行为
- **构建**：修改完成后需运行 `build.cmd`（即 `tsc`）重新编译，输出到 `dist/editor.bundle.js`

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 对修复后的 Editor.ts 进行交叉验证，确认所有 textarea selection 设置点已统一改造，无遗漏
- Expected outcome: 生成完整的调用点覆盖报告，确保修复无死角