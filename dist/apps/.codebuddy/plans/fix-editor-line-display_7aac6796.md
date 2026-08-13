---
name: fix-editor-line-display
overview: 修复代码编辑器在文件行数超过视口可显示高度时，超出部分（如第 60 行之后）无法显示的问题。根因是 `.editor-codeview` 被 CSS 的 `bottom:0` + `overflow:hidden` 固定为视口高度并裁剪内容，导致它不随滚动容器一起滚动，而只有行号栏(gutter)能滚动。修复方式：让代码区按完整内容高度随滚动容器一起滚动。
todos:
  - id: fix-codeview-css
    content: 修改 css/layout.css 中 .editor-codeview，移除 bottom:0/right:0/overflow:hidden 使其随滚动容器完整滚动
    status: completed
  - id: fix-completion-position
    content: 修正 src/core/Editor.ts 的 renderCompletion() 弹窗定位为绝对行坐标，避免滚动后偏移
    status: completed
    dependencies:
      - fix-codeview-css
---

## 用户需求

代码编辑器在加载的代码行数超过浏览器可视高度（例如超过 60 行）时，超出视口的代码内容无法显示出来，用户滚动后仍然看不到后面的行。需要定位并修复该渲染显示问题。

## 产品概述

这是一个基于 HTML + TypeScript 的轻量级代码编辑器，通过 `tsconfig.json` 将 `src/` 编译为 `dist/editor.bundle.js`，在 `index.html` 中引用并渲染代码、行号、语法高亮与光标。当前 bug 表现为：文件行数多于可视区域时，第 60 行之后的代码始终不可见，而行号栏却能随滚动移动。

## 核心特性（待修复）

- 代码区应随滚动容器整体滚动，文件行数超过视口时可通过滚动查看全部代码。
- 滚动后光标（textarea）与代码行保持对齐。
- 滚动后代码补全弹窗（completion popup）定位保持正确。
- 行号栏与代码区滚动保持同步。

## 技术栈

- 前端：原生 HTML + TypeScript（命名空间模式，`module: none`，单一 `outFile` 打包）
- 样式：纯 CSS（`css/layout.css` + `css/theme-light.css` / `css/theme-dark.css`，通过 CSS 变量切换主题）
- 构建：`tsc` 按 `tsconfig.json` 编译为 `dist/editor.bundle.js`
- 渲染架构：分层 DOM —— `.editor-scroll`（滚动容器）内含 `.editor-gutter`（行号/折叠标记）、`.editor-codeview`（高亮代码行）、`.editor-input`（透明 textarea，承载光标与输入）、`.completion-popup`（补全弹窗）

## 实现方案

### 根因分析（已验证）

`src/core/Editor.ts` 的设计意图是：**`renderCodeView()` 一次性渲染全部行（非虚拟化）**，并按"绝对行坐标"随 `.editor-scroll` 一起滚动：

- `renderCaret()` 用 `textarea.style.transform = translate(col*charWidth, line*lineHeight)` 把 textarea 定位到光标的绝对行位置；
- `scrollContainer` 的 scroll 事件重算 `firstVisibleLine` 并 `render()`；
- `.editor-gutter` 为 `position:absolute; top:0`，无 `height/bottom` 限制，高度由全部行号撑开，因此能正常滚动（证明滚动机制本身可用）。

但 `css/layout.css` 中 `.editor-codeview` 设置为：

```
position:absolute; left:80px; top:0; right:0; bottom:0; overflow:hidden;
```

`bottom:0` 把代码区高度钉死为视口高度，`overflow:hidden` 把超出视口的内容裁掉。于是代码区不随滚动容器滚动，第 60 行之后被永久裁剪 —— 这正是"行号能滚、代码不显示超出部分"的直接原因。

### 关键决策

1. **最小且正确的修复（推荐）**：让 `.editor-codeview` 按完整内容高度随滚动容器滚动。移除 `bottom:0` 与 `overflow:hidden`，使其高度由全部行内容自动撑开，与 gutter 同步滚动。因 `.editor-codeview` 未单独设置背景色（继承 `.editor-root` 的 `--editor-background`），移除 `right:0` 也不会在右侧产生视觉空隙；长行可通过 `.editor-scroll` 的水平滚动查看。
2. **滚动后补全弹窗修正**：`.completion-popup` 是 `.editor-scroll` 的子元素，会随容器滚动，因此其定位应使用绝对行坐标而非"视口相对坐标"。当前 `renderCompletion()` 用 `(pos.line - this.firstVisibleLine + 1) * this.lineHeight`，滚动后会发生偏移，需改为 `pos.line * this.lineHeight`（left 用 `pos.column * this.charWidth`）。
3. **不采用虚拟滚动**：当前渲染逻辑已渲染全部行且依赖绝对坐标，虚拟化改动面大、风险高，超出本次 bug 修复范围（YAGNI）。

### 性能与可靠性

- 渲染逻辑不变：仍为全量重建 innerHTML（仅修复高度/裁剪与补全定位），无新增 N+1 或额外遍历。
- 滚动无新增性能开销；`render()` 重建成本与现有一致，对于本次"显示完整性"目标可接受。
- 向后兼容：仅调整 CSS 高度/溢出属性与一处弹窗定位计算，不影响输入、高亮、折叠、diff、符号导航等逻辑。

## 实现要点（防回归）

- 修改 `css/layout.css` 的 `.editor-codeview`：删除 `right:0; bottom:0;` 与 `overflow:hidden;`，保留 `position:absolute; left:80px; top:0;`，改用 `overflow:visible`，让高度由内容撑开。
- 修改 `src/core/Editor.ts` 的 `renderCompletion()`：`top` 改为 `pos.line * this.lineHeight`，`left` 改为 `pos.column * this.charWidth`（弹窗作为滚动容器子元素，使用绝对坐标）。
- 复用现有 `this.lineHeight` / `this.charWidth` 测量值，不新增字段。
- 保持 `renderCaret()` 不变：textarea 作为滚动容器绝对定位子元素会随滚动一起移动，仍与代码行对齐。

## 架构影响

- 仅涉及表现层 CSS 与一处 UI 定位计算，不改变数据层（`TextBuffer`）、光标层（`Cursor`）、高亮层（`Highlighter`）或事件流。
- 滚动容器、gutter、codeView、textarea 四者均为 `.editor-scroll` 的绝对定位子元素，统一随容器滚动，结构一致。

## 目录结构（受影响文件）

```
code-editor/
├── css/
│   └── layout.css          # [MODIFY] 修正 .editor-codeview：移除 bottom:0 / right:0 / overflow:hidden，改为随内容撑开高度并随滚动容器滚动
└── src/
    └── core/
        └── Editor.ts       # [MODIFY] 修正 renderCompletion() 弹窗定位：改用绝对行坐标 (pos.line * lineHeight)，避免滚动后偏移
```