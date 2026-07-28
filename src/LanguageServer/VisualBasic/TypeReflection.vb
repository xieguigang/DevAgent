Imports System.Reflection

Module TypeReflection

    Const kind_prop As String = "property"
    Const kind_func As String = "function"
    Const kind_field As String = "field"
    Const kind_const As String = "constant"

    ' CLR 全名 -> VB 别名（用于让补全签名更贴近 VB 源码风格）
    Private ReadOnly TypeAlias As New Dictionary(Of String, String) From {
        {"System.Int32", "Integer"},
        {"System.Int64", "Long"},
        {"System.Int16", "Short"},
        {"System.Byte", "Byte"},
        {"System.SByte", "SByte"},
        {"System.UInt32", "UInteger"},
        {"System.UInt64", "ULong"},
        {"System.UInt16", "UShort"},
        {"System.Single", "Single"},
        {"System.Double", "Double"},
        {"System.Decimal", "Decimal"},
        {"System.Boolean", "Boolean"},
        {"System.Char", "Char"},
        {"System.String", "String"},
        {"System.Object", "Object"},
        {"System.DateTime", "Date"},
        {"System.IntPtr", "IntPtr"},
        {"System.UIntPtr", "UIntPtr"},
        {"System.Void", "Void"},
        {"System.Threading.Tasks.Task", "Task"}
    }

    ' 候选命名空间：Type.GetType(简单名) 通常失败，按常用命名空间尝试解析
    Private ReadOnly CandidateNamespaces As String() = {
        "", "System", "System.Text", "System.Text.RegularExpressions",
        "System.Collections", "System.Collections.Generic",
        "System.Collections.ObjectModel", "System.Linq", "System.IO",
        "System.Diagnostics", "System.Globalization", "System.Numerics",
        "System.Threading", "System.Threading.Tasks",
        "System.Reflection", "Microsoft.VisualBasic"
    }

    ''' <summary>
    ''' 基于反射自动提取给定 CLR 类型的成员（属性、方法、字段/常量）。
    ''' 仅包含 public 的实例与共享成员，并排除属性/事件访问器、运算符等特殊名称成员。
    ''' </summary>
    Public Iterator Function ParseTypeMembers(type As Type) As IEnumerable(Of LanguageMemberInfo)
        If type Is Nothing Then Return

        Dim flags As BindingFlags = BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.Static

        ' 属性
        For Each prop As PropertyInfo In type.GetProperties(flags)
            Dim canRead = prop.CanRead
            Dim canWrite = prop.CanWrite
            If Not canRead AndAlso Not canWrite Then Continue For

            ' 仅公共访问器可决定可见性；setter 为私有时 GetSetMethod() 返回 Nothing，需判空
            Dim isShared = False
            Dim g = prop.GetGetMethod()
            Dim s = prop.GetSetMethod()
            If g IsNot Nothing Then isShared = g.IsStatic
            If Not isShared AndAlso s IsNot Nothing Then isShared = s.IsStatic

            Dim accessor = If(isShared, "Shared ", "")
            Dim rw = ""
            If canRead AndAlso Not canWrite Then rw = "ReadOnly "
            If canWrite AndAlso Not canRead Then rw = "WriteOnly "

            Dim detail = $"{accessor}{rw}Property {prop.Name} As {FormatTypeName(prop.PropertyType)}"

            Dim idxps = prop.GetIndexParameters()
            Dim insert = If(idxps.Length > 0, $"{prop.Name}({MakePlaceholders(idxps.Length)})", prop.Name)

            Yield New LanguageMemberInfo(prop.Name, kind_prop, detail, insert)
        Next

        ' 方法
        For Each func As MethodInfo In type.GetMethods(flags)
            If func.IsSpecialName Then Continue For

            Dim isSub = func.ReturnType Is GetType(Void)
            Dim kw = If(isSub, "Sub", "Function")
            Dim accessor = If(func.IsStatic, "Shared ", "")
            Dim ps = FormatParameters(func.GetParameters())
            Dim paramStr = If(ps.Length > 0, "(" & ps & ")", "()")
            Dim ret = If(isSub, "", " As " & FormatTypeName(func.ReturnType))
            Dim detail = $"{accessor}{kw} {func.Name}{paramStr}{ret}"

            Dim n = func.GetParameters().Length
            Dim insert = $"{func.Name}({MakePlaceholders(n)})"

            Yield New LanguageMemberInfo(func.Name, kind_func, detail, insert)
        Next

        ' 字段 / 常量
        For Each field As FieldInfo In type.GetFields(flags)
            If field.IsSpecialName Then Continue For

            If field.IsLiteral Then
                Dim detail = $"Const {field.Name} As {FormatTypeName(field.FieldType)}"
                Yield New LanguageMemberInfo(field.Name, kind_const, detail, field.Name)
            Else
                Dim accessor = If(field.IsStatic, "Shared ", "")
                Dim rw = If(field.IsInitOnly, "ReadOnly ", "")
                Dim detail = $"{accessor}{rw}{field.Name} As {FormatTypeName(field.FieldType)}"
                Yield New LanguageMemberInfo(field.Name, kind_field, detail, field.Name)
            End If
        Next
    End Function

    ''' <summary>将 CLR 类型格式化为 VB 风格的类型名（含数组、可空、泛型与别名映射）。</summary>
    Private Function FormatTypeName(t As Type) As String
        If t Is Nothing Then Return ""

        ' 数组
        If t.IsArray Then
            Return FormatTypeName(t.GetElementType()) & "()"
        End If

        ' 按引用 / 指针（参数 ByRef 场景在 FormatParameters 中处理，这里兜底）
        If t.IsByRef OrElse t.IsPointer Then
            Return FormatTypeName(t.GetElementType())
        End If

        ' 泛型参数（开放泛型中的 T / TKey 等）
        If t.IsGenericParameter Then
            Return t.Name
        End If

        ' 可空类型 Nullable(Of T) -> T?
        If t.IsGenericType AndAlso t.GetGenericTypeDefinition() Is GetType(Nullable(Of )) Then
            Return FormatTypeName(t.GetGenericArguments()(0)) & "?"
        End If

        ' 泛型类型 List(Of T) / Dictionary(Of TKey, TValue)
        If t.IsGenericType Then
            Dim name = t.Name
            Dim bt = name.IndexOf("`"c)
            If bt >= 0 Then name = name.Substring(0, bt)
            Dim args = t.GetGenericArguments().[Select](Function(a) FormatTypeName(a)).ToArray()
            Return $"{name}(Of {String.Join(", ", args)})"
        End If

        ' CLR 全名 -> VB 别名
        If t.FullName IsNot Nothing AndAlso TypeAlias.ContainsKey(t.FullName) Then
            Return TypeAlias(t.FullName)
        End If

        Return t.Name
    End Function

    ''' <summary>将参数列表格式化为 "name As Type" 形式，按引用参数加 ByRef 前缀，可选参数加 Optional 前缀。</summary>
    Private Function FormatParameters(pars As ParameterInfo()) As String
        If pars Is Nothing OrElse pars.Length = 0 Then Return ""

        Dim parts(pars.Length - 1) As String
        For i = 0 To pars.Length - 1
            Dim p = pars(i)
            Dim pt = p.ParameterType
            Dim mods = ""
            If pt.IsByRef Then
                mods = "ByRef "
                pt = pt.GetElementType()
            End If
            If p.IsOptional Then mods = mods & "Optional "

            parts(i) = $"{mods}{p.Name} As {FormatTypeName(pt)}"
        Next
        Return String.Join(", ", parts)
    End Function

    ''' <summary>生成 VSCode 风格的补全占位符 $1, $2 ...</summary>
    Private Function MakePlaceholders(n As Integer) As String
        If n <= 0 Then Return ""
        Dim parts(n - 1) As String
        For i = 1 To n
            parts(i - 1) = "$" & i
        Next
        Return String.Join(", ", parts)
    End Function

    ''' <summary>
    ''' 按 CLR 类型名解析 Type：优先用全名（含命名空间）直接解析，
    ''' 否则按候选命名空间尝试，最后扫描已加载程序集。返回 Nothing 表示无法解析。
    ''' </summary>
    Public Function ResolveType(name As String) As Type
        If String.IsNullOrWhiteSpace(name) Then Return Nothing

        Dim t = Type.GetType(name)
        If t IsNot Nothing Then Return t

        For Each ns In CandidateNamespaces
            Dim full = If(String.IsNullOrEmpty(ns), name, ns & "." & name)
            t = Type.GetType(full)
            If t IsNot Nothing Then Return t
        Next

        For Each asm In AppDomain.CurrentDomain.GetAssemblies()
            t = asm.GetType(name)
            If t IsNot Nothing Then Return t
            For Each ns In CandidateNamespaces
                If Not String.IsNullOrEmpty(ns) Then
                    t = asm.GetType(ns & "." & name)
                    If t IsNot Nothing Then Return t
                End If
            Next
        Next

        Return Nothing
    End Function

End Module
