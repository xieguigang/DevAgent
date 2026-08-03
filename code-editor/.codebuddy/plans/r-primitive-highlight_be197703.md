---
name: r-primitive-highlight
overview: 为 R 语言高亮器增加 R 内置 primitive 函数（含所有中缀运算符和普通前缀函数）的语法高亮支持，使用「不同颜色 + 加粗」样式与用户第三方函数区分。
todos:
  - id: add-tokentype
    content: 在 Tokenizer.ts 的 TokenType 枚举新增 PrimitiveFunction 成员
    status: completed
  - id: update-rhighlighter
    content: 在 RHighlighter.ts 新增 PRIMITIVES/PRIMITIVE_OPS 集合，并在标识符与运算符分支输出 PrimitiveFunction
    status: completed
    dependencies:
      - add-tokentype
  - id: map-tokenclass
    content: 在 Editor.ts 的 tokenClass 新增 PrimitiveFunction 映射为 tok-primitive
    status: completed
    dependencies:
      - add-tokentype
  - id: theme-styles
    content: 在 theme-dark.css 与 theme-light.css 新增 tok-primitive 颜色加粗样式
    status: completed
    dependencies:
      - map-tokenclass
  - id: build-verify
    content: 运行 build.cmd 确认编译通过并验证 primitive 高亮效果
    status: completed
    dependencies:
      - update-rhighlighter
      - theme-styles
---

## 用户需求

在现有代码编辑器中增强 R 语言语法高亮，新增对 R 内部 primitive 函数的语法高亮支持，将内置 primitive 函数与外部用户编写的第三方函数明确区分开。

## 产品概述

当前编辑器已支持 R 语言的基础高亮（关键字、常量、字符串、注释、普通函数调用等）。本次更新需识别 R 的内置 primitive（含中缀运算符与普通前缀函数）并以专属样式着色，使用户一眼区分「R 内置原语」与「第三方/自定义函数」。

## 核心功能

- 新增 primitive 函数识别：覆盖 R 全部 primitive，包括普通前缀函数（如 `c`、`sum`、`list`、`length`）与标识符型/符号型运算符（如 `+ - * / : [[ $ @`）。
- 新增专属 token 类型 `PrimitiveFunction`，与普通 `Function`（第三方函数）区分。
- 视觉样式：primitive 使用区别于 Function 的颜色并加粗显示；第三方函数 `myfun()` 仍保持原有 Function 样式。
- 双主题适配：在暗色与亮色主题下分别为 primitive 定义颜色与加粗规则。

## 技术栈

- 语言：TypeScript（项目现有，编译至 `dist/` 由 `build.cmd` 调用 tsc）
- 高亮架构：`ILanguageHighlighter` 接口 + 基于单行的 `tokenizeLine` + `TokenType` 枚举 + `TokenBuilder`
- 渲染层：`Editor.ts` 的 `tokenClass` 将 `TokenType` 映射为 CSS class
- 样式层：`css/theme-dark.css` / `css/theme-light.css` 的 CSS 变量与 class 规则

## 实现方案

### 总体策略

在 `TokenType` 枚举中新增 `PrimitiveFunction` 成员；在 `RHighlighter` 中建立完整的 R primitive 名称集合，并在「标识符分支」「标识符型运算符分支」「符号运算符分支」命中 primitive 时输出 `PrimitiveFunction` token；在 `Editor.tokenClass` 中映射为 `tok-primitive`；在两套主题 CSS 中新增 `tok-primitive` 的「颜色 + 加粗」样式。

### 关键技术决策

1. **primitive 列表来源与去重**：采用 R 官方 `get("__Primitives__", baseenv())` / `base:::primaries` 导出的完整 primitive 名称集合（约 200+ 项）。与现有 `CONTROL_KEYWORDS`、`CONSTANTS` 存在重叠（如 `if`、`NULL`、`:` 等），处理方式为：在 `PRIMITIVES` 集合中包含全部名称，并在分支判断时「primitive 优先」——若某词既是关键字/常量又是 primitive，以 `PrimitiveFunction` 着色，保证 primitive 视觉统一。但对现有 CONTROL_KEYWORDS（`if/for/while/function/...`）保持关键字语义更合理，故仅对「被归类为函数/运算符的 primitive」做 primitive 着色，关键字与常量分支维持不变（避免破坏控制流着色）；其余 primitive（前缀函数 + 运算符）走新类型。
2. **符号型运算符分支改造**：现有 176-182 行将连续 `[+\-*/^<>=!&|~$@?:]` 整体捕获为 `Operator`。改为：先取连续片段 `op = line.substring(i,j)`，若 `PRIMITIVE_OPS`（如 `+ - * / : %% %/% ^ & | < > == != <= >= <- <<- -> ->> $ @ [[ ]] $<` 等运算符型 primitive）包含该片段，则标 `PrimitiveFunction`，否则保持 `Operator`。
3. **标识符型运算符**（`[[`、`、`@`）：这些在标识符分支被 `[A-Za-z_.]` 命中，需扩展标识符起始字符或在运算符分支单独处理。最简单稳妥做法：在符号运算符分支的字符集之外，单独处理 `` ` ``、`` [[ ``、`$`、`@ `作为 primitive 运算符（已在 backtick 与标识符逻辑附近添加判断），命中集合则标 `PrimitiveFunction`。
4. **优先级**：中缀 `%...%` 维持 `Operator`（非 primitive 范畴），不改动。

### 性能与可靠性

- primitive 检测为 O(1) 的 `Set.has` 查表，单行 tokenize 内无跨行状态、无正则回溯，时间与现有实现同量级，无性能瓶颈。
- `TokenType` 为数字枚举，新增成员不影响既有数值与序列化。
- 向后兼容：未命中 primitive 的标识符/运算符仍走 `Function`/`Operator`/`Identifier`，第三方函数样式不变。

## 实现注意事项

- 复用现有 `TokenBuilder.push` 与 `TokenType` 机制，不引入新解析范式。
- 主题颜色选择：暗色建议紫色 `#c586c0`（与黄色 Function 明显区分）；亮色建议紫色 `#af00db` 或深青，确保对比度。
- 编译验证：修改后用 `build.cmd` 确认 tsc 无报错。
- 避免误伤：`、`@`、`[[ `仅在「作为运算符使用」时着色，不在变量名中间（如 `a$b` 中 ` 是运算符，符合预期）。

## 架构设计

```
RHighlighter.tokenizeLine
  ├─ 标识符分支: word ∈ PRIMITIVES 且后跟 '(' → PrimitiveFunction
  ├─ 符号运算符分支: op ∈ PRIMITIVE_OPS → PrimitiveFunction (否则 Operator)
  └─ 标识符型运算符 ($ @ [[): ∈ PRIMITIVE_OPS → PrimitiveFunction
Editor.tokenClass(PrimitiveFunction) → "tok-primitive"
CSS theme-*.css: .tok-primitive { color: var(--tok-primitive); font-weight: bold; }
```

## 目录结构（受影响文件）

```
src/utils/Tokenizer.ts            # [MODIFY] 在 enum TokenType 中新增 PrimitiveFunction 成员
src/highlighters/RHighlighter.ts  # [MODIFY] 新增 PRIMITIVES / PRIMITIVE_OPS 集合；在标识符、符号运算符、标识符型运算符分支命中时输出 PrimitiveFunction
src/core/Editor.ts                # [MODIFY] tokenClass 新增 case TokenType.PrimitiveFunction → "tok-primitive"
css/theme-dark.css                # [MODIFY] 新增 --tok-primitive 变量与 body[data-theme="dark"] .tok-primitive 规则（颜色+加粗）
css/theme-light.css               # [MODIFY] 同步新增 --tok-primitive 变量与 .tok-primitive 规则（颜色+加粗）
```

## 关键代码结构

```ts
// src/utils/Tokenizer.ts
export enum TokenType {
    // ... 现有成员 ...
    Function,
    Constant,
    Annotation,
    DocComment,
    Error,
    PrimitiveFunction   // 新增：R 内置 primitive 函数/运算符
}

// src/highlighters/RHighlighter.ts（示意集合，实际为完整 R primitive 列表）
private static PRIMITIVES = new Set<string>([
    "c","list","sum","length","print","rep","seq","matrix","attr","attributes",
    "class","unclass","names","dim","dimnames","typeof","storage.mode","mode",
    "as.character","as.numeric","as.integer","as.logical","as.complex","as.double",
    "is.null","is.na","is.numeric","is.character","is.logical","is.list","is.function",
    "vector","numeric","character","logical","integer","double","complex","raw",
    "structure","substitute","quote","eval","call","expression","force","on.exit",
    "environment","globalenv","baseenv","parent.frame","Recall","UseMethod",
    "standardGeneric","body","formals","args","invisible","withVisible","delayedAssign",
    "bindenv","emptyenv","new.env","parent.env","lockBinding","unlockBinding",
    "quote","missing","nargs","sys.call","sys.function","sys.frame","sys.parent",
    "proc.time","gc","memory.profile","tracemem","retracemem","untracemem",
    "raw","intToBits","rawConnection","file","textConnection","gzfile","bzfile",
    "pipe","socketConnection","url","stdin","stdout","stderr","readLines",
    "writeLines","cat","print","format","paste","sprintf","strsplit","sub",
    "gsub","match","pmatch","charmatch","startsWith","endsWith","grep","grepl",
    "regexpr","gregexpr","agrep","tolower","toupper","chartr","abbreviate",
    "nchar","nzchar","substr","substring","strtrim","make.names","make.unique",
    "all","any","sum","prod","min","max","range","mean","median","var","sd",
    "cov","cor","sum","diff","cumsum","cumprod","cummax","cummin","round",
    "signif","trunc","floor","ceiling","abs","sign","sqrt","exp","log","expm1",
    "log1p","cos","sin","tan","acos","asin","atan","cosh","sinh","tanh",
    "gamma","lgamma","digamma","trigamma","choose","factorial","beta","lbeta",
    "rowSums","colSums","rowMeans","colMeans","apply","lapply","sapply","vapply",
    "tapply","mapply","Map","Reduce","Filter","Find","Position","Negate","eapply",
    "rapply","outer","kronecker","sweep","scale","rowsum","aggregate","by",
    "split","unsplit","rbind","cbind","data.frame","as.data.frame","expand.grid",
    "order","sort","rank","unique","duplicated","union","intersect","setdiff",
    "setequal","is.element","which","which.min","which.max","array","matrix",
    "diag","upper.tri","lower.tri","t","crossprod","tcrossprod","solve",
    "eigen","svd","qr","det","determinant","fft","nextn","convolve","filter",
    "poly","lm","glm","optim","nlm","nlminb","uniroot","polyroot","integrate",
    "sample","rnorm","runif","rpois","rexp","rbinom","rbeta","rgamma","rchisq",
    "rt","rf","set.seed","Random.seed","date","Sys.time","Sys.Date","format.POSIXct",
    "as.POSIXct","as.Date","difftime","julian","months","quarters","weekdays",
    "stop","warning","message","geterrmessage","conditionCall","conditionMessage",
    "try","tryCatch","withCallingHandlers","signalCondition","simpleCondition",
    "errorCondition","warningCondition","restart","withRestarts","invokeRestart",
    "computeRestarts","findRestart","browser","recover","trace","untrace",
    "options","getOption","par","dev.off","plot","hist","boxplot","points",
    "lines","abline","title","axis","legend","text","arrows","segments","polygon",
    "curve","pairs","coplot","image","contour","persp","barplot","dotchart",
    "identify","locator","stem","qqnorm","qqline"
]);
```