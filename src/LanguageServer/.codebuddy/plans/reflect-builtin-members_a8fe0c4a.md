---
name: reflect-builtin-members
overview: 将 VisualBasicLanguageService.vb 中 BuildBuiltinTypeMembers 的硬编码成员列表，改为调用 TypeReflection.vb 的 ParseTypeMembers 进行基于反射的自动提取；并实现一套可按 CLR 全名（namespace+type.name）解析任意类型、带结果缓存的扩展机制。
todos:
  - id: impl-parse-type-members
    content: 实现 TypeReflection.vb 的 ParseTypeMembers 及 FormatTypeName/FormatParameters/ResolveType 辅助函数
    status: completed
  - id: rewrite-build
    content: 重写 BuildBuiltinTypeMembers 用 ParseTypeMembers 反射提取 12 个内置类型
    status: completed
    dependencies:
      - impl-parse-type-members
  - id: extend-lookup
    content: 在 GetBuiltinTypeMembersList 增加 CLR 类型解析回退与 ConcurrentDictionary 缓存
    status: completed
    dependencies:
      - impl-parse-type-members
  - id: verify-build
    content: 编译项目并验证反射生成的成员补全与既有逻辑一致
    status: completed
    dependencies:
      - rewrite-build
      - extend-lookup
---

## 用户需求

将 VB.NET 语言服务中 `VisualBasic\VisualBasicLanguageService.vb` 的 `BuildBuiltinTypeMembers()` 函数里硬编码的内置类型成员，改为基于 `VisualBasic\TypeReflection.vb` 的 `ParseTypeMembers(type As Type)` 通过反射自动提取。

## 产品概述

底层反射机制需具备通用性与可扩展性：不再局限于现有 12 个类型，而是能够依据任意 CLR 类型的 `命名空间 + 类型名` 进行解析并自动生成成员补全信息，实现“输入任意 CLR 类型即可获得成员列表”。

## 核心特性

- 反射提取成员：仅提取 `public` 的实例与共享成员，自动排除属性访问器（get_/set_）、事件访问器、运算符等特殊名称成员。
- 成员信息自动生成：为属性、方法、字段（含常量）自动生成 `Label/Kind/Detail/InsertText`，签名风格与现有硬编码保持一致（如 `Function Substring(startIndex As Integer) As String` / `Substring($1)`）。
- 泛型支持：对 `List`/`Dictionary` 等采用开放泛型定义（`GetType(List(Of))` / `GetType(Dictionary(Of,))`），参数呈现为 `T` / `TKey` / `TValue`。
- 任意类型扩展：内置字典预置常用类型；对未命中字典的类型名，按候选命名空间尝试解析 CLR 类型并反射，结果带缓存，从而支持任意 CLR 类型。
- CLR 类型名美化：在生成签名时将 `System.Int32` 等映射回 VB 别名（`Integer`、`Long`、`Date` 等），数组显示为 `Type()`，提升补全可读性。

## 技术栈选择

- 语言/框架：VB.NET（.NET，Microsoft.VisualBasic 语言服务后台项目，项目文件 `LanguageServer.vbproj`）
- 核心 API：.NET 反射 `System.Reflection`（PropertyInfo / MethodInfo / FieldInfo / ParameterInfo）、`Type.GetType`
- 数据结构：`LanguageMemberInfo`（已有，含 `Label/Kind/Detail/InsertText`）、`Dictionary(Of String, LanguageMemberInfo())`、`ConcurrentDictionary`（运行时解析缓存）
- 辅助：`System.Linq`（泛型参数投影）、`System.Collections.Concurrent`（线程安全缓存）

## 实现方案

总体策略：在 `TypeReflection.vb` 中把现有空壳 `ParseTypeMembers` 补全为通用的反射提取器，并新增 `FormatTypeName`/`FormatParameters`/`ResolveType` 等纯函数；在 `VisualBasicLanguageService.vb` 中将 `BuildBuiltinTypeMembers()` 改写为调用 `ParseTypeMembers` 填充 12 个常用类型，并在 `GetBuiltinTypeMembersList` 增加“未命中字典 → 解析 CLR 类型 → 反射 → 缓存”的回退分支，从而实现任意 CLR 类型支持。

关键技术决策与权衡：

- **通用反射而非按类型特判**：`ParseTypeMembers(type As Type)` 不依赖类型具体信息，任何 `Type` 都可处理，满足“无限扩展”。
- **排除 `IsSpecialName` 成员**：避免属性/事件访问器、运算符（`op_*`、`get_*`、`set_*`、`add_*`、`remove_*`）造成的重复与噪声，保持结果整洁，符合用户筛选要求。
- **开放泛型定义**：`GetType(List(Of))` 的 `GetMethods/GetProperties` 返回的参数类型即泛型参数 `T`/`TKey`，`Name` 自然为 `T`/`TKey`，无需额外映射即可呈现与现有硬编码一致的签名。
- **解析缓存用 `ConcurrentDictionary`**：`GetCompletions` 可能在并发请求下触发运行时解析，使用 `ConcurrentDictionary(Of String, LanguageMemberInfo())`（大小写不敏感）避免重复反射与加锁开销。
- **`ResolveType` 候选命名空间**：`Type.GetType(简单名)` 通常失败，故对 `System`、`System.Text`、`System.Collections.Generic`、`System.Linq`、`System.IO`、`System.Diagnostics`、`Microsoft.VisualBasic` 等常用命名空间做候选解析，兼顾“任意类型”与实用性；全名（含命名空间）可直接命中。

性能与可靠性：`BuildBuiltinTypeMembers` 在静态只读字段初始化时只执行一次反射，开销可忽略；运行时解析走缓存，复杂度 O(1) 命中、首次 O(成员数) 反射，无 N+1 或重复遍历问题。错误通过 `TryGetValue`/`Nothing` 安全回退到 `Object` 成员，不会抛异常中断补全。

## 实现要点（防止回归）

- **保持既有契约**：`LanguageMemberInfo` 构造函数与 `Kind` 取值（`property`/`function`/`field`/`constant`）不变；新增 `kind_const = "constant"` 用于字面量字段，与现有补全 kind 词表一致。
- **签名格式对齐**：`detail` 形如 `ReadOnly Property Length As Integer`、`Shared Function Parse(s As String) As Integer`、`Const PI As Double`；`insertText` 对索引属性/方法生成 `$1`/`$1, $2` 占位符，与既有补全插入体验一致。
- **`CleanTypeName` 不受影响**：点号补全流程只改“成员来源”，不改类型名解析逻辑，避免影响现有 `Me`/`MyBase`/文档符号等分支。
- **避免 log 噪声**：反射解析失败静默回退，不写日志；不输出大对象。
- **向后兼容**：原有的 12 个类型键（String/Integer/.../Convert）保持作为字典预置；`ObjectMembers`（ToString/Equals/GetHashCode/GetType）保持不变，仅在 `GetBuiltinTypeMembersList` 的 `Object` 特殊分支中复用。

## 架构设计

数据/控制流（仅改动高亮部分）：

1. 静态初始化：`BuiltinTypeMembers = BuildBuiltinTypeMembers()`（`ParseTypeMembers` 反射 12 类型）
2. 点号补全：`GetMemberCompletions` → `GetBuiltinTypeMembersList(typeName)`

- 命中 `BuiltinTypeMembers` 字典 → 直接返回
- 未命中 → `ReflectionCache` 命中 → 返回
- 仍未命中 → `TypeReflection.ResolveType(typeName)`（候选命名空间解析）
    - 解析成功 → `ParseTypeMembers(resolved)` → 写入 `ReflectionCache` → 返回
    - 解析失败 → 回退 `Object` 成员（既有行为）

各模块职责保持单一：`TypeReflection` 只负责“类型 → 成员信息”的纯反射逻辑；`VisualBasicLanguageService` 负责“类型名 → 补全项”的查询与缓存编排，符合现有分层与 SoC。

## 目录结构

```
VisualBasic/
├── TypeReflection.vb                 # [MODIFY] 补全 ParseTypeMembers 实现，新增反射辅助函数
└── VisualBasicLanguageService.vb    # [MODIFY] 改写 BuildBuiltinTypeMembers 与 GetBuiltinTypeMembersList
```

### VisualBasic/TypeReflection.vb（[MODIFY]）

目的：将现有空壳 `ParseTypeMembers` 变为通用反射提取器，并补充类型名格式化与解析辅助。
功能与实现要求：

- `ParseTypeMembers(type As Type) As IEnumerable(Of LanguageMemberInfo)`：使用 `BindingFlags.Public Or Instance Or Static` 遍历属性/方法/字段；跳过 `IsSpecialName`；为属性按读/写生成 `ReadOnly/WriteOnly/Property` 及索引占位符；方法按 `ReturnType=Void` 区分 `Sub`/`Function`；字段按 `IsLiteral` 使用 `constant` 否则 `field`；通过 `FormatTypeName` 生成 VB 风格类型名。
- `FormatTypeName(t As Type) As String`：处理数组(`()`)、`Nullable(Of T)`(`?`)、泛型参数(`T`/`TKey`)、开放泛型(`List(Of T)`)，并通过别名表将 `System.Int32→Integer`、`System.DateTime→Date` 等映射回 VB 别名。
- `FormatParameters(pars As ParameterInfo()) As String`：生成 `name As Type` 列表，`Optional` 参数加前缀。
- `MakePlaceholders(n As Integer) As String`：生成 `$1, $2...` 占位符。
- `ResolveType(name As String) As Type`：先用 `Type.GetType(name)`；失败再遍历候选命名空间拼接解析；返回 `Nothing` 表示无法解析。
- 新增 `Imports System.Linq`、`Imports System.Reflection`、常量 `kind_const = "constant"`。

### VisualBasic/VisualBasicLanguageService.vb（[MODIFY]）

目的：用反射替换硬编码成员数据，并开放任意 CLR 类型的运行时解析。
功能与实现要求：

- `BuildBuiltinTypeMembers()`（第 86–255 行）：删除全部硬编码数组，改为对 12 个类型分别调用 `TypeReflection.ParseTypeMembers(GetType(...)).ToArray()` 并写入字典（键保持 `String`/`Integer`/`Double`/`Decimal`/`Boolean`/`Date`/`Array`/`List`/`Dictionary`/`Math`/`Console`/`Convert`；泛型用 `GetType(List(Of))` / `GetType(Dictionary(Of,))`）。
- `GetBuiltinTypeMembersList(typeName)`（第 641–659 行）：在既有 `BuiltinTypeMembers.TryGetValue` 之后、回退 `Object` 之前，增加 `ReflectionCache`（`ConcurrentDictionary`，大小写不敏感）查找；未命中则调用 `TypeReflection.ResolveType` 解析，成功则 `ParseTypeMembers` 并写入缓存后返回；失败保持原有 `Object` 回退。
- 新增 `Imports System.Collections.Concurrent`（如未导入）；`ObjectMembers` 与 `GetMemberCompletions` 其它分支保持不变。

## 关键代码结构

```
' TypeReflection.vb
Public Iterator Function ParseTypeMembers(type As Type) As IEnumerable(Of LanguageMemberInfo)
'   properties: BindingFlags.Public Or Instance Or Static, skip special names
'   methods:    skip IsSpecialName, Sub/Function by ReturnType=Void
'   fields:     literal -> kind_const, else kind_field
End Function

Public Function ResolveType(name As String) As Type
'   Type.GetType(name) -> else try CandidateNamespaces & "." & name
End Function

Private Function FormatTypeName(t As Type) As String
'   array/Nullable/generic-param/open-generic + VB alias map
End Function
```