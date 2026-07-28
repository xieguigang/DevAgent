---
name: vbproj_model_enhancement
overview: 增强 VBDocument.vb 中的 VBProject 类，从 vbproj(SDK 风格)解析出项目元数据、NuGet 元数据、编译配置、外部项目引用、NuGet 程序包引用，并新增从 VBProject 对象生成/保存 SDK 风格 vbproj 文件的函数。
todos:
  - id: add-models
    content: 在 VBDocument.vb 新增 VBProjectReference、VBPackageReference、VBBuildConfiguration、VBProjectMetadata、VBNuGetMetadata 模型类及 VBProject 新属性
    status: completed
  - id: extend-load
    content: 扩展 Load 解析 Sdk 属性、全部 PropertyGroup（含条件编译配置）、ProjectReference、PackageReference 与 Compile Remove 规则
    status: completed
    dependencies:
      - add-models
  - id: implement-generate
    content: 实现 Generate() 返回 SDK 风格 XDocument 与 Save(path) 写盘保存 vbproj
    status: completed
    dependencies:
      - add-models
  - id: update-tests
    content: 更新 Program.vb 的 TestProject() 验证新增元数据、引用、配置及生成产物
    status: completed
    dependencies:
      - extend-load
      - implement-generate
---

## 用户需求

完善 `VBLang\VBDocument.vb` 中的 `VBProject` 对象，使其能从 SDK 风格 vbproj 文件中解析出更完善的信息，并新增从 `VBProject` 对象反向生成 vbproj 文件的函数。

## 产品概述

对现有 `VBProject` 模型进行扩展，将 vbproj 的关键信息（项目元数据、NuGet 包元数据、条件编译配置、外部项目引用、NuGet 程序包引用、源文件 Include/Remove 规则）结构化建模，并提供干净规范的 SDK 风格 vbproj 生成/保存能力。

## 核心特性

- 解析并建模项目元数据：TargetFramework(s)、Platforms、Nullable、LangVersion、EnableDefaultCompileItems、SignAssembly 等。
- 解析并建模 NuGet 包元数据：PackageId、Version、Authors、Company、PackageLicenseExpression、RepositoryUrl、GeneratePackageOnBuild 等。
- 解析项目编译配置：根据带 `$(Configuration)|$(Platform)` 条件的 PropertyGroup 生成 Debug/Release × Platform 编译配置（DefineConstants、Optimize、OutputPath 等）。
- 解析外部项目引用 `ProjectReference`（Include 路径、Private/Condition）。
- 解析 NuGet 程序包引用 `PackageReference`（Include 包名、Version、IncludeAssets/PrivateAssets/ExcludeAssets）。
- 从 `VBProject` 生成并返回/保存干净规范的 SDK 风格 vbproj 文件（含 Compile Include 与 Remove 规则）。
- 保持向后兼容：现有 `RootNamespace`、`AssemblyName`、`OutputType`、`CompileFiles` 及 `Load`/`GetType` 行为不变。

## 技术栈选择

- 语言/运行时：VB.NET / .NET 10（与现有 `VBLang.vbproj` 一致）
- XML 处理：复用现有 `System.Xml.Linq.XDocument`（已在 `Load` 中使用）
- 测试：复用现有 `VBLang.Test` 控制台项目（修改 `Program.vb` 的 `TestProject()`）
- 解析范围：仅 SDK 风格 `<Project Sdk="...">`（维持现有 `Load` 解析方式）

## 实现方案

### 总体策略

在 `VBDocument.vb` 同文件中为 `VBProject` 新增一组强类型模型类，并扩展 `Load` 的解析逻辑以遍历全部 `PropertyGroup`/`ItemGroup`；新增 `Generate()`（返回 `XDocument`）与 `Save(path)`（写盘）完成反向生成。整体沿用现有 `ReadProperty`/`CollectCompileFiles` 的命名空间与相对路径处理习惯，避免引入新依赖。

### 关键技术决策

1. **模型类定义**：新增 `VBProjectReference`、`VBPackageReference`、`VBBuildConfiguration`、`VBProjectMetadata`、`VBNuGetMetadata` 五个模型类，全部置于 `VBDocument.vb` 内，与 `VBProject` 紧耦合，符合“完善 VBDocument.vb”的意图，避免文件碎片化和额外 `Imports`。
2. **PropertyGroup 归类**：遍历所有 `PropertyGroup`，无 `Condition` 的普通属性按元素名归类到 `VBProjectMetadata`（如 Platforms/Nullable/LangVersion）或 `VBNuGetMetadata`（元素名以 `Package*`/`Repository*`/`Authors`/`Company`/`Product`/`Copyright`/`GeneratePackageOnBuild` 等匹配）；带 `$(Configuration)|$(Platform)` 条件的归并为 `VBBuildConfiguration`（保存原始 `Condition` 字符串 + 强类型常用字段 + 通用属性字典）。
3. **Compile Remove 保留**：扩展 `CollectCompileFiles` 使其同时返回被 `Remove` 的模式列表，新增 `VBProject.CompileExcludes As String()`，供生成时还原 `<Compile Remove="..." />`。
4. **生成顺序与规范**：生成时按 `Sdk` 属性根 → 主 `PropertyGroup`（TargetFramework(s)/RootNamespace/AssemblyName/OutputType/Platforms 等）→ NuGet `PropertyGroup` → 各配置条件 `PropertyGroup` → `Compile` 的 `ItemGroup`（先 Remove 后 Include）→ `ProjectReference`/`PackageReference` 的 `ItemGroup` 组织，输出带 XML 声明与缩进的规范文件。
5. **向后兼容**：`RootNamespace`/`AssemblyName`/`OutputType`/`CompileFiles` 仍由 `Load` 填充（取自同一数据源），`GetType`/`LoadAssembly` 逻辑不变，测试现有断言不受影响。

### 性能与可靠性

- `Load` 为一次性解析，复杂度为 O(节点数)；`Generate` 为 O(属性数)，均在小规模 XML 上开销可忽略。
- 沿用现有 `Try/Catch` 容错习惯：源文件读取失败跳过、XML 缺失节点返回空值，不因个别属性缺失中断整体解析。
- 生成时对 `Nothing`/空集合做跳过处理，避免写出空元素或空 `ItemGroup`。

### 避免技术债务

- 复用现有 `ReadProperty`、`NormalizePath`、`GetRelativePath`、`IsExcludedByDefault`、`GlobMatch` 等私有辅助方法，不重复实现。
- 不改动 `VBParser`/`VBScanner`/反射加载等无关逻辑，控制改动面在 `VBDocument.vb` 与测试内。

## 实现说明

- 在 `Load` 中读取根元素 `Sdk` 属性（如 `Microsoft.NET.Sdk`），存入 `VBProject.Sdk`。
- `ReadProperty` 保留用于兼容字段；新增 `ReadAllProperties` 收集每个 `PropertyGroup` 的全部子元素（名称→值+Condition）。
- `ProjectReference`/`PackageReference` 的子元素（如 `Version`、`PrivateAssets`）以属性或子元素形式读取。
- `Generate()` 使用 `XDocument` + `XElement` 构建；`Save(path)` 调用 `Save(Path, SaveOptions.None)` 并保留声明。
- 测试 `TestProject()` 增加打印/断言：Sdk、Metadata.TargetFramework、NuGet 相关字段、Configurations 数量、ProjectReferences/PackageReferences 条目，以及调用 `Generate().ToString()` 验证产物非空且含关键节点。

## 架构设计

维持现有单文件分层：`VBDocument`（文档）+ `VBProject`（项目聚合根，承担解析与生成职责）+ 配套模型类。无新增架构模式，符合项目现有简洁风格。`VBProject` 作为聚合根同时持有解析（`Load`/`LoadAssembly`）与生成（`Generate`/`Save`）能力。

## 目录结构

```
VBLang/
└── VBDocument.vb        # [MODIFY] 在 VBProject 类同文件新增 VBProjectReference、VBPackageReference、
                         #   VBBuildConfiguration、VBProjectMetadata、VBNuGetMetadata 五个模型类；
                         #   扩展 VBProject 属性（Sdk、Metadata、NuGet、Configurations、
                         #   ProjectReferences、PackageReferences、CompileExcludes），保留 RootNamespace/
                         #   AssemblyName/OutputType/CompileFiles 兼容字段；扩展 Load 解析 Sdk、全部
                         #   PropertyGroup（含条件配置）、ItemGroup 中的 ProjectReference/PackageReference
                         #   与 Compile Remove；新增 Generate()/Save() 生成 SDK 风格 vbproj。
VBLang.Test/
└── Program.vb           # [MODIFY] 扩展 TestProject()：打印并验证新增的 Sdk、项目元数据、NuGet 元数据、
                         #   编译配置、项目引用、程序包引用，并调用 Generate() 验证产物包含关键节点；
                         #   不破坏现有 RootNamespace/AssemblyName/OutputType/CompileFiles 断言。
```

## 关键代码结构

```
Public Class VBProject
    ' 兼容字段（Load 时同步填充）
    Public Property RootNamespace As String
    Public Property AssemblyName As String
    Public Property OutputType As String
    Public Property CompileFiles As VBDocument()

    ' 新增字段
    Public Property Sdk As String
    Public Property Metadata As VBProjectMetadata
    Public Property NuGet As VBNuGetMetadata
    Public Property Configurations As VBBuildConfiguration()
    Public Property ProjectReferences As VBProjectReference()
    Public Property PackageReferences As VBPackageReference()
    Public Property CompileExcludes As String()

    Public Shared Function Load(vbproj As String) As VBProject
    Public Function Generate() As XDocument
    Public Sub Save(path As String)
End Class

Public Class VBBuildConfiguration
    Public Property Condition As String          ' 原始条件，如 '$(Configuration)|$(Platform)' == 'Debug|AnyCPU'
    Public Property Configuration As String       ' Debug / Release
    Public Property Platform As String            ' AnyCPU / x64
    Public Property DefineConstants As String
    Public Property Optimize As Boolean
    Public Property OutputPath As String
    Public Property Extra As Dictionary(Of String, String)   ' 其它未知属性
End Class
```