---
name: git-diff-viewer
overview: 在 git.html 中实现一个交互式可视化 git diff 的 Web 应用，加载并展示由 diff.vb 的 DiffResult 对象序列化而来的 JSON 数据，采用与 startpage.html 一致的 light 主题样式，并用 G:\DevAgent\test\git_diff.json 测试。
design:
  architecture:
    framework: html
  styleKeywords:
    - Light Theme
    - Visual Studio Inspired
    - Clean Minimal
    - Monospace Diff
    - Color-coded Changes
  fontSystem:
    fontFamily: Segoe UI
    heading:
      size: 22px
      weight: 600
    subheading:
      size: 15px
      weight: 600
    body:
      size: 14px
      weight: 400
  colorSystem:
    primary:
      - "#007acc"
      - "#0067b8"
    background:
      - "#ffffff"
      - "#f5f5f5"
      - "#e5f3ff"
    text:
      - "#1e1e1e"
      - "#6a6a6a"
      - "#8a8a8a"
    functional:
      - "#107c10"
      - "#cb2431"
      - "#e6f4e6"
      - "#ffeef0"
todos:
  - id: create-git-css
    content: 创建 css/git.css，定义 light 主题下两栏布局、文件列表、diff 行着色与工具栏样式
    status: completed
  - id: build-html-shell
    content: 搭建 git.html 骨架：topbar、工具栏、左文件列表、右 diff 区、状态栏、页脚并引用 startpage.css
    status: completed
    dependencies:
      - create-git-css
  - id: implement-loader
    content: 实现 JSON 加载：文件选择+拖拽+FileReader+JSON.parse+结构校验+错误提示
    status: completed
    dependencies:
      - build-html-shell
  - id: implement-filelist
    content: 实现左栏文件列表渲染、搜索过滤、变更类型筛选与每文件增删计数
    status: completed
    dependencies:
      - implement-loader
  - id: implement-diff-view
    content: 实现右栏 diff 渲染：hunk 头、行号三列、增删上下文着色、hunk 折叠、隐藏上下文、复制
    status: completed
    dependencies:
      - implement-filelist
  - id: verify-test
    content: 用 git_diff.json 验证：确认 11 文件、统计正确、各交互可用、样式与 startpage 一致
    status: completed
    dependencies:
      - implement-diff-view
---

## 用户需求

在 `git.html` 中实现一个基于 HTML + JavaScript 的 Web 应用，用于交互式可视化由 `git\diff.vb` 的 `DiffResult` 对象序列化而来的 JSON 数据；页面样式需与 `startpage.html` 保持一致的 light 主题风格；完成后使用 `G:\DevAgent\test\git_diff.json` 进行测试验证。

## 产品概述

一个纯前端、零构建依赖的 Git Diff 查看器。用户通过文件选择或拖拽加载 `DiffResult` JSON，页面以可视化方式展示每个文件的变更差异（新增/删除/上下文行着色、行号、hunk 区块），并提供搜索、类型筛选、折叠、复制等交互。

## 核心功能

- 加载 `DiffResult` JSON（文件选择 + 拖拽，兼容 `file://` 直接打开）
- 左栏文件列表：文件名/路径、变更类型徽章（A/M/D/R）、每文件增删行数统计、点击切换
- 右栏差异视图：按 hunk 渲染，hunk 头显示 `@@ -old,count +new,count @@`，每行三列（旧行号/新行号/符号+内容），新增绿底、删除红底、上下文普通
- 交互：文件搜索、按变更类型筛选、隐藏上下文行（仅看增删）、hunk 折叠/展开、复制当前文件 diff 文本
- 状态栏汇总：文件总数、总新增、总删除；加载失败时给出错误提示

## 技术栈

- 纯 HTML + 原生 JavaScript（内联 `<script>`，无需 TS 构建，双击即可运行）
- 样式：复用 `css/startpage.css` 的 light 主题通用类（topbar/statusbar/footer/section），新增 `css/git.css` 承载 diff 查看器专属样式
- 数据加载：`FileReader` + `JSON.parse`，无网络请求（避免 `file://` 跨域限制）

## 实现方案

### 数据模型与映射

`DiffResult` JSON 结构：`{ Files: [ { ChangeKind:int, FilePath:str, Hunks:[ { OldStart,OldCount,NewStart,NewCount:int, Lines:[ { Content:str, Type:int } ] } ] } ] }`。
枚举映射（依据 `diff.vb` 与测试数据双重确认）：

- `DiffLineType`：0=Added（新增）、1=Deleted（删除）、2=Context（上下文）
- `FileChangeKind`：0=Added、1=Modified、2=Deleted、3=Renamed
路径中的 `/` 被序列化为 `\/`，`JSON.parse` 会自动还原为 `/`，无需手动处理。

### 关键决策

- 内联脚本不依赖 `dist/devkit.js`；Launch 按钮退化为指向 `index.html` 的链接（用 `typeof` 守卫避免缺失脚本报错），保证 `file://` 下稳定。
- 加载方式选文件选择 + 拖拽，而非 `fetch` 默认路径：因测试数据在 `code-editor` 目录外，`fetch` 在 `file://` 下会触发 CORS 失败；文件输入/拖拽可 100% 复现用户测试路径。
- 渲染策略：仅渲染当前选中文件的 hunk，左栏仅渲染文件行；用字符串拼接一次性 `innerHTML`，避免逐行 DOM 操作。测试数据中最大文件数百~上千行仍可控，无需虚拟列表。
- 安全：所有 `Content` 渲染前做 HTML 实体转义（`< > & "`），防止 XML/HTML 内容破坏布局或注入。
- 行号填充：新增行旧号为空、删除行新号为空；hunk 内按 `Type` 递增 `OldStart`/`NewStart` 计算行号。

### 性能与可靠性

- 时间复杂度：解析 O(N)（N 为总行数），渲染 O(当前文件行数)；空间 O(N) 仅存解析结果。
- 瓶颈：超大文件（如 `.sln`/`.vbproj`）一次性内联渲染，通过“仅渲染当前文件”已将开销限制在单文件级别；提供“隐藏上下文行”开关进一步减负。
- 错误处理：结构校验（`Files` 为数组、每项含 `Hunks`/`FilePath`），异常时状态栏/toast 显示可读错误，不崩溃页面。

## 实现说明

- 复用 `startpage.css` 的 CSS 变量（`--accent:#007acc`、`--ui-background:#fff`、`--ui-background-alt:#f5f5f5`、`--statusbar-background:#007acc` 等），保证主题一致。
- 新增行配色取 `--card-green:#107c10`（淡绿底），删除行取红 `#cb2431`（淡红底），与既有配色体系一致。
- 顶部导航“Git”项高亮（仿 startpage 当前页态），其余导航项保留 Start/Editor/About。

## 架构设计

```
git.html (页面骨架 + 内联脚本)
  ├─ 加载层：<input type=file> + 拖拽区 → FileReader → JSON.parse → validate
  ├─ 状态：state = { diff, currentFile, filter, search, hideContext }
  ├─ 渲染层：
  │    ├─ renderFileList()   → 左栏（搜索/类型筛选/计数）
  │    └─ renderDiff(file)   → 右栏（hunk + 行号 + 着色 + 折叠）
  └─ 交互：搜索输入 / 类型筛选 / 隐藏上下文 / hunk 折叠 / 复制
CSS: startpage.css (复用) + git.css (新增, diff 专属)
```

## 目录结构

```
g:\DevAgent\code-editor\
├── git.html          # [NEW] 主页面：topbar/toolbar/左栏文件列表/右栏 diff 视图/状态栏/页脚，
│                      #       以及内联 <script> 实现加载、解析、校验、渲染与所有交互逻辑。
│                      #       头部 <link> 引用 css/startpage.css 与 css/git.css。
└── css/
    └── git.css       # [NEW] Diff 查看器专属 light 主题样式：两栏布局(grid)、
                      #       文件列表行与徽章、diff 行三列表格、新增/删除/上下文着色、
                      #       hunk 头、工具栏/开关、状态栏汇总等。复用 startpage.css 变量。
```

## 关键代码结构（行号计算与转义）

```javascript
// 行号递增规则（在渲染单个 hunk 的 Lines 时）
let oldNo = hunk.OldStart, newNo = hunk.NewStart;
for (const line of hunk.Lines) {
  const oldCell = line.Type === 1 ? '' : (oldNo++); // 删除行无新号
  const newCell = line.Type === 0 ? '' : (newNo++); // 新增行无旧号
  // Type 0=Added,1=Deleted,2=Context
}
// HTML 转义
function esc(s){ return String(s).replace(/[&<>"']/g, c => (
  {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
```

## 设计风格

采用与 `startpage.html` 一致的 Visual Studio 风格 light 主题：白色背景、Segoe UI 字体、#007acc 蓝色强调色、半透明毛玻璃顶栏。整个页面为单页应用，分为顶栏、工具栏、左文件列表栏、右 diff 视图区、状态栏、页脚六块，整体简洁专业、留白充足。

## 页面规划（单页：Git Diff Viewer）

- 顶导航栏：品牌图标 + “Code Editor” 文字；导航 Start / Editor / Git(高亮) / About；右侧 Launch Editor 按钮。
- 工具栏块：标题“Git Diff Viewer”与说明文字；右侧操作区含“打开 JSON”按钮、文件搜索框、变更类型下拉筛选、隐藏上下文开关、折叠/展开全部、复制按钮。
- 左文件列表栏：竖向滚动列表，每行含文件名（加粗）、目录路径（等宽小字）、变更类型徽章（A/M/D/R 配色）、`+增/-删` 计数；当前文件高亮；支持搜索/筛选实时过滤；空态提示“未加载数据”。
- 右 diff 视图区：文件头（完整路径 + 变更类型徽章）；按 hunk 分段，hunk 头显示 `@@ -old,count +new,count @@` 并可点击折叠；每行三列等宽栅格（旧行号/新行号/符号+内容），新增行淡绿底、删除行淡红底、上下文行白底灰字；长行横向滚动。
- 状态栏：左侧“Git Diff Viewer”，右侧汇总“N 个文件 · +总增 -总删”。
- 页脚：Start · Editor · About 链接与版本信息，仿 startpage。

## 交互细节

- 点击文件行切换右栏；搜索框输入即时过滤左栏；类型筛选下拉联动；隐藏上下文开关隐藏 Type=2 行；hunk 头点击折叠/展开；复制按钮将当前文件 diff 文本写入剪贴板并 toast 提示；拖拽 JSON 文件到页面任意位置即可加载。