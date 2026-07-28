Imports System.Text
Imports System.Text.RegularExpressions
Imports Flute.Http.Core.Message

''' <summary>
''' VB.NET 语言服务：提供基于文档上下文的智能提示和代码补全。
''' 通过分析 VB.NET 源代码文本，提取已声明的符号（类、模块、函数、属性、变量），
''' 结合内置关键词、类型、代码片段，生成补全候选项。
''' </summary>
Public Class VisualBasicLanguageService

#Region "静态数据"

    ' VB.NET 声明/修饰符关键词
    Private Shared ReadOnly Keywords As String() = {
        "Public", "Private", "Protected", "Friend", "Shared", "Static", "ReadOnly",
        "WriteOnly", "Dim", "Const", "Class", "Module", "Structure", "Interface",
        "Enum", "Namespace", "Sub", "Function", "Property", "Operator", "Event",
        "Delegate", "Handles", "Implements", "Inherits", "Of", "As", "New", "Me",
        "MyBase", "MyClass", "Nothing", "True", "False", "Null", "And", "Or", "Not",
        "Xor", "AndAlso", "OrElse", "Is", "IsNot", "Like", "Mod", "TypeOf", "GetType",
        "AddressOf", "Await", "Async", "Iterator", "Partial", "Overridable",
        "Overloads", "Overrides", "MustInherit", "MustOverride", "NotOverridable",
        "Shadows", "Widening", "Narrowing", "ByVal", "ByRef", "Optional", "ParamArray",
        "Declare", "Lib", "Alias", "Option", "Explicit", "Strict", "Compare", "Text",
        "Binary", "Off", "On", "Infer", "Custom", "AddHandler", "RemoveHandler",
        "RaiseEvent", "DirectCast", "TryCast", "CType", "CInt", "CStr", "CBool",
        "CDbl", "CDec", "CLng", "CShort", "CSng", "CByte", "CChar", "CDate",
        "CUInt", "CULng", "CUShort", "CSByte", "Let", "Set", "Get", "Wend", "Call",
        "ReDim", "Preserve", "Erase", "Error", "Resume", "Print", "Input", "Line",
        "Width", "Open", "Close", "Put", "Imports", "Region", "End", "ExternalSource",
        "ExternalChecksum", "EndIf"
    }

    ' VB.NET 控制流关键词
    Private Shared ReadOnly ControlKeywords As String() = {
        "If", "Then", "Else", "ElseIf", "End", "Select", "Case", "For", "Each",
        "In", "While", "Until", "Loop", "Do", "Next", "Exit", "Continue", "Return",
        "Yield", "Try", "Catch", "Finally", "Throw", "When", "Using", "SyncLock",
        "With", "Step", "To", "GoTo", "Stop"
    }

    ' VB.NET 内置值类型和框架常用类型
    Private Shared ReadOnly BuiltinTypes As String() = {
        "Boolean", "Byte", "SByte", "Char", "Date", "Decimal", "Double", "Single",
        "Integer", "UInteger", "Long", "ULong", "Short", "UShort", "String", "Object",
        "Void", "IntPtr", "UIntPtr"
    }

    ' 常用 .NET 框架类型
    Private Shared ReadOnly FrameworkTypes As String() = {
        "List", "Dictionary", "HashSet", "Queue", "Stack", "LinkedList",
        "SortedSet", "SortedDictionary", "IEnumerable", "IEnumerator",
        "ICollection", "IList", "IDictionary", "Array", "Math",
        "Console", "Convert", "Tuple", "KeyValuePair", "Action", "Func",
        "StringBuilder", "Regex", "DateTime", "TimeSpan"
    }

    ' 代码片段模板：label -> (insertText, detail)
    Private Shared ReadOnly Snippets As (label As String, insertText As String, detail As String)() = {
        ("If", "If $1 Then" & vbCrLf & "    $2" & vbCrLf & "End If", "If 语句"),
        ("ElseIf", "ElseIf $1 Then" & vbCrLf & "    $2", "ElseIf 语句"),
        ("For", "For i As Integer = $1 To $2" & vbCrLf & "    $3" & vbCrLf & "Next", "For 循环"),
        ("For Each", "For Each item In $1" & vbCrLf & "    $2" & vbCrLf & "Next", "For Each 循环"),
        ("While", "While $1" & vbCrLf & "    $2" & vbCrLf & "End While", "While 循环"),
        ("Do While", "Do While $1" & vbCrLf & "    $2" & vbCrLf & "Loop", "Do While 循环"),
        ("Do Until", "Do Until $1" & vbCrLf & "    $2" & vbCrLf & "Loop", "Do Until 循环"),
        ("Try", "Try" & vbCrLf & "    $1" & vbCrLf & "Catch ex As Exception" & vbCrLf & "    $2" & vbCrLf & "Finally" & vbCrLf & "    $3" & vbCrLf & "End Try", "Try/Catch 块"),
        ("Select", "Select Case $1" & vbCrLf & "    Case $2" & vbCrLf & "        $3" & vbCrLf & "    Case Else" & vbCrLf & "        $4" & vbCrLf & "End Select", "Select Case 语句"),
        ("Class", "Public Class $1" & vbCrLf & "    $2" & vbCrLf & "End Class", "类声明"),
        ("Module", "Module $1" & vbCrLf & "    $2" & vbCrLf & "End Module", "模块声明"),
        ("Structure", "Structure $1" & vbCrLf & "    $2" & vbCrLf & "End Structure", "结构声明"),
        ("Interface", "Interface $1" & vbCrLf & "    $2" & vbCrLf & "End Interface", "接口声明"),
        ("Enum", "Enum $1" & vbCrLf & "    $2" & vbCrLf & "End Enum", "枚举声明"),
        ("Function", "Public Function $1($2) As $3" & vbCrLf & "    $4" & vbCrLf & "End Function", "函数声明"),
        ("Sub", "Public Sub $1($2)" & vbCrLf & "    $3" & vbCrLf & "End Sub", "子过程声明"),
        ("Property", "Public Property $1 As $2", "属性声明"),
        ("Namespace", "Namespace $1" & vbCrLf & "    $2" & vbCrLf & "End Namespace", "命名空间声明"),
        ("Using", "Using $1" & vbCrLf & "    $2" & vbCrLf & "End Using", "Using 块"),
        ("SyncLock", "SyncLock $1" & vbCrLf & "    $2" & vbCrLf & "End SyncLock", "SyncLock 块"),
        ("With", "With $1" & vbCrLf & "    $2" & vbCrLf & "End With", "With 块"),
        ("Get Set", "Public Property $1 As $2" & vbCrLf & "    Get" & vbCrLf & "        Return $3" & vbCrLf & "    End Get" & vbCrLf & "    Set(value As $2)" & vbCrLf & "        $4 = value" & vbCrLf & "    End Set" & vbCrLf & "End Property", "带 Get/Set 的属性")
    }

    ' 内置类型的常用成员
    Private Shared Function BuildBuiltinTypeMembers() As Dictionary(Of String, LanguageMemberInfo())
        Dim dict As New Dictionary(Of String, LanguageMemberInfo())(StringComparer.OrdinalIgnoreCase)

        dict("String") = {
            New LanguageMemberInfo("Length", "property", "ReadOnly Property Length As Integer", "Length"),
            New LanguageMemberInfo("Substring", "function", "Function Substring(startIndex As Integer) As String", "Substring($1)"),
            New LanguageMemberInfo("IndexOf", "function", "Function IndexOf(value As String) As Integer", "IndexOf($1)"),
            New LanguageMemberInfo("LastIndexOf", "function", "Function LastIndexOf(value As String) As Integer", "LastIndexOf($1)"),
            New LanguageMemberInfo("Contains", "function", "Function Contains(value As String) As Boolean", "Contains($1)"),
            New LanguageMemberInfo("StartsWith", "function", "Function StartsWith(value As String) As Boolean", "StartsWith($1)"),
            New LanguageMemberInfo("EndsWith", "function", "Function EndsWith(value As String) As Boolean", "EndsWith($1)"),
            New LanguageMemberInfo("Replace", "function", "Function Replace(oldValue As String, newValue As String) As String", "Replace($1, $2)"),
            New LanguageMemberInfo("Split", "function", "Function Split(separator As Char()) As String()", "Split($1)"),
            New LanguageMemberInfo("Trim", "function", "Function Trim() As String", "Trim()"),
            New LanguageMemberInfo("TrimStart", "function", "Function TrimStart() As String", "TrimStart()"),
            New LanguageMemberInfo("TrimEnd", "function", "Function TrimEnd() As String", "TrimEnd()"),
            New LanguageMemberInfo("ToUpper", "function", "Function ToUpper() As String", "ToUpper()"),
            New LanguageMemberInfo("ToLower", "function", "Function ToLower() As String", "ToLower()"),
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("Equals", "function", "Function Equals(value As String) As Boolean", "Equals($1)"),
            New LanguageMemberInfo("CompareTo", "function", "Function CompareTo(strB As String) As Integer", "CompareTo($1)"),
            New LanguageMemberInfo("Concat", "function", "Shared Function Concat(a As String, b As String) As String", "Concat($1, $2)"),
            New LanguageMemberInfo("Join", "function", "Shared Function Join(separator As String, value As String()) As String", "Join($1, $2)"),
            New LanguageMemberInfo("IsNullOrEmpty", "function", "Shared Function IsNullOrEmpty(value As String) As Boolean", "IsNullOrEmpty($1)"),
            New LanguageMemberInfo("IsNullOrWhiteSpace", "function", "Shared Function IsNullOrWhiteSpace(value As String) As Boolean", "IsNullOrWhiteSpace($1)"),
            New LanguageMemberInfo("PadLeft", "function", "Function PadLeft(totalWidth As Integer) As String", "PadLeft($1)"),
            New LanguageMemberInfo("PadRight", "function", "Function PadRight(totalWidth As Integer) As String", "PadRight($1)"),
            New LanguageMemberInfo("Remove", "function", "Function Remove(startIndex As Integer) As String", "Remove($1)"),
            New LanguageMemberInfo("Insert", "function", "Function Insert(startIndex As Integer, value As String) As String", "Insert($1, $2)"),
            New LanguageMemberInfo("Chars", "property", "ReadOnly Property Chars(index As Integer) As Char", "Chars($1)")
        }

        dict("Integer") = {
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("Equals", "function", "Function Equals(obj As Object) As Boolean", "Equals($1)"),
            New LanguageMemberInfo("CompareTo", "function", "Function CompareTo(value As Integer) As Integer", "CompareTo($1)"),
            New LanguageMemberInfo("Parse", "function", "Shared Function Parse(s As String) As Integer", "Parse($1)"),
            New LanguageMemberInfo("TryParse", "function", "Shared Function TryParse(s As String, ByRef result As Integer) As Boolean", "TryParse($1, $2)")
        }

        dict("Double") = {
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("Equals", "function", "Function Equals(obj As Object) As Boolean", "Equals($1)"),
            New LanguageMemberInfo("Parse", "function", "Shared Function Parse(s As String) As Double", "Parse($1)"),
            New LanguageMemberInfo("TryParse", "function", "Shared Function TryParse(s As String, ByRef result As Double) As Boolean", "TryParse($1, $2)"),
            New LanguageMemberInfo("IsNaN", "function", "Shared Function IsNaN(d As Double) As Boolean", "IsNaN($1)"),
            New LanguageMemberInfo("IsInfinity", "function", "Shared Function IsInfinity(d As Double) As Boolean", "IsInfinity($1)")
        }

        dict("Decimal") = {
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("Parse", "function", "Shared Function Parse(s As String) As Decimal", "Parse($1)"),
            New LanguageMemberInfo("TryParse", "function", "Shared Function TryParse(s As String, ByRef result As Decimal) As Boolean", "TryParse($1, $2)")
        }

        dict("Boolean") = {
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("Equals", "function", "Function Equals(obj As Object) As Boolean", "Equals($1)"),
            New LanguageMemberInfo("Parse", "function", "Shared Function Parse(value As String) As Boolean", "Parse($1)"),
            New LanguageMemberInfo("TrueString", "constant", "ReadOnly TrueString As String = ""True""", "TrueString"),
            New LanguageMemberInfo("FalseString", "constant", "ReadOnly FalseString As String = ""False""", "FalseString")
        }

        dict("Date") = {
            New LanguageMemberInfo("Now", "property", "Shared ReadOnly Property Now As Date", "Now"),
            New LanguageMemberInfo("Today", "property", "Shared ReadOnly Property Today As Date", "Today"),
            New LanguageMemberInfo("Year", "property", "ReadOnly Property Year As Integer", "Year"),
            New LanguageMemberInfo("Month", "property", "ReadOnly Property Month As Integer", "Month"),
            New LanguageMemberInfo("Day", "property", "ReadOnly Property Day As Integer", "Day"),
            New LanguageMemberInfo("Hour", "property", "ReadOnly Property Hour As Integer", "Hour"),
            New LanguageMemberInfo("Minute", "property", "ReadOnly Property Minute As Integer", "Minute"),
            New LanguageMemberInfo("Second", "property", "ReadOnly Property Second As Integer", "Second"),
            New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
            New LanguageMemberInfo("AddDays", "function", "Function AddDays(value As Double) As Date", "AddDays($1)"),
            New LanguageMemberInfo("AddMonths", "function", "Function AddMonths(months As Integer) As Date", "AddMonths($1)"),
            New LanguageMemberInfo("AddYears", "function", "Function AddYears(value As Integer) As Date", "AddYears($1)"),
            New LanguageMemberInfo("AddHours", "function", "Function AddHours(value As Double) As Date", "AddHours($1)"),
            New LanguageMemberInfo("AddMinutes", "function", "Function AddMinutes(value As Double) As Date", "AddMinutes($1)"),
            New LanguageMemberInfo("Parse", "function", "Shared Function Parse(s As String) As Date", "Parse($1)"),
            New LanguageMemberInfo("TryParse", "function", "Shared Function TryParse(s As String, ByRef result As Date) As Boolean", "TryParse($1, $2)")
        }

        dict("Array") = {
            New LanguageMemberInfo("Length", "property", "ReadOnly Property Length As Integer", "Length"),
            New LanguageMemberInfo("Rank", "property", "ReadOnly Property Rank As Integer", "Rank"),
            New LanguageMemberInfo("GetLength", "function", "Function GetLength(dimension As Integer) As Integer", "GetLength($1)"),
            New LanguageMemberInfo("GetValue", "function", "Function GetValue(index As Integer) As Object", "GetValue($1)"),
            New LanguageMemberInfo("SetValue", "function", "Function SetValue(value As Object, index As Integer)", "SetValue($1, $2)"),
            New LanguageMemberInfo("Sort", "function", "Shared Sub Sort(array As Array)", "Sort($1)"),
            New LanguageMemberInfo("Reverse", "function", "Shared Sub Reverse(array As Array)", "Reverse($1)"),
            New LanguageMemberInfo("IndexOf", "function", "Shared Function IndexOf(array As Array, value As Object) As Integer", "IndexOf($1, $2)"),
            New LanguageMemberInfo("Copy", "function", "Shared Sub Copy(sourceArray As Array, destArray As Array, length As Integer)", "Copy($1, $2, $3)"),
            New LanguageMemberInfo("Clear", "function", "Shared Sub Clear(array As Array, index As Integer, length As Integer)", "Clear($1, $2, $3)")
        }

        dict("List") = {
            New LanguageMemberInfo("Count", "property", "ReadOnly Property Count As Integer", "Count"),
            New LanguageMemberInfo("Capacity", "property", "Property Capacity As Integer", "Capacity"),
            New LanguageMemberInfo("Item", "property", "Property Item(index As Integer) As T", "Item($1)"),
            New LanguageMemberInfo("Add", "function", "Sub Add(item As T)", "Add($1)"),
            New LanguageMemberInfo("AddRange", "function", "Sub AddRange(collection As IEnumerable(Of T))", "AddRange($1)"),
            New LanguageMemberInfo("Remove", "function", "Function Remove(item As T) As Boolean", "Remove($1)"),
            New LanguageMemberInfo("RemoveAt", "function", "Sub RemoveAt(index As Integer)", "RemoveAt($1)"),
            New LanguageMemberInfo("RemoveAll", "function", "Function RemoveAll(match As Predicate(Of T)) As Integer", "RemoveAll($1)"),
            New LanguageMemberInfo("Clear", "function", "Sub Clear()", "Clear()"),
            New LanguageMemberInfo("Contains", "function", "Function Contains(item As T) As Boolean", "Contains($1)"),
            New LanguageMemberInfo("IndexOf", "function", "Function IndexOf(item As T) As Integer", "IndexOf($1)"),
            New LanguageMemberInfo("Insert", "function", "Sub Insert(index As Integer, item As T)", "Insert($1, $2)"),
            New LanguageMemberInfo("Sort", "function", "Sub Sort()", "Sort()"),
            New LanguageMemberInfo("Reverse", "function", "Sub Reverse()", "Reverse()"),
            New LanguageMemberInfo("ToArray", "function", "Function ToArray() As T()", "ToArray()"),
            New LanguageMemberInfo("Find", "function", "Function Find(match As Predicate(Of T)) As T", "Find($1)"),
            New LanguageMemberInfo("FindAll", "function", "Function FindAll(match As Predicate(Of T)) As List(Of T)", "FindAll($1)"),
            New LanguageMemberInfo("ForEach", "function", "Sub ForEach(action As Action(Of T))", "ForEach($1)"),
            New LanguageMemberInfo("GetRange", "function", "Function GetRange(index As Integer, count As Integer) As List(Of T)", "GetRange($1, $2)")
        }

        dict("Dictionary") = {
            New LanguageMemberInfo("Count", "property", "ReadOnly Property Count As Integer", "Count"),
            New LanguageMemberInfo("Keys", "property", "ReadOnly Property Keys As KeyCollection", "Keys"),
            New LanguageMemberInfo("Values", "property", "ReadOnly Property Values As ValueCollection", "Values"),
            New LanguageMemberInfo("Item", "property", "Property Item(key As TKey) As TValue", "Item($1)"),
            New LanguageMemberInfo("Add", "function", "Sub Add(key As TKey, value As TValue)", "Add($1, $2)"),
            New LanguageMemberInfo("Remove", "function", "Function Remove(key As TKey) As Boolean", "Remove($1)"),
            New LanguageMemberInfo("Clear", "function", "Sub Clear()", "Clear()"),
            New LanguageMemberInfo("ContainsKey", "function", "Function ContainsKey(key As TKey) As Boolean", "ContainsKey($1)"),
            New LanguageMemberInfo("ContainsValue", "function", "Function ContainsValue(value As TValue) As Boolean", "ContainsValue($1)"),
            New LanguageMemberInfo("TryGetValue", "function", "Function TryGetValue(key As TKey, ByRef value As TValue) As Boolean", "TryGetValue($1, $2)")
        }

        dict("Math") = {
            New LanguageMemberInfo("Abs", "function", "Shared Function Abs(value As Double) As Double", "Abs($1)"),
            New LanguageMemberInfo("Max", "function", "Shared Function Max(val1 As Double, val2 As Double) As Double", "Max($1, $2)"),
            New LanguageMemberInfo("Min", "function", "Shared Function Min(val1 As Double, val2 As Double) As Double", "Min($1, $2)"),
            New LanguageMemberInfo("Sqrt", "function", "Shared Function Sqrt(d As Double) As Double", "Sqrt($1)"),
            New LanguageMemberInfo("Pow", "function", "Shared Function Pow(x As Double, y As Double) As Double", "Pow($1, $2)"),
            New LanguageMemberInfo("Round", "function", "Shared Function Round(d As Double) As Double", "Round($1)"),
            New LanguageMemberInfo("Floor", "function", "Shared Function Floor(d As Double) As Double", "Floor($1)"),
            New LanguageMemberInfo("Ceiling", "function", "Shared Function Ceiling(d As Double) As Double", "Ceiling($1)"),
            New LanguageMemberInfo("Log", "function", "Shared Function Log(d As Double) As Double", "Log($1)"),
            New LanguageMemberInfo("Log10", "function", "Shared Function Log10(d As Double) As Double", "Log10($1)"),
            New LanguageMemberInfo("Exp", "function", "Shared Function Exp(d As Double) As Double", "Exp($1)"),
            New LanguageMemberInfo("Sin", "function", "Shared Function Sin(d As Double) As Double", "Sin($1)"),
            New LanguageMemberInfo("Cos", "function", "Shared Function Cos(d As Double) As Double", "Cos($1)"),
            New LanguageMemberInfo("Tan", "function", "Shared Function Tan(d As Double) As Double", "Tan($1)"),
            New LanguageMemberInfo("Sign", "function", "Shared Function Sign(d As Double) As Integer", "Sign($1)"),
            New LanguageMemberInfo("PI", "constant", "Const PI As Double = 3.14159265358979", "PI"),
            New LanguageMemberInfo("E", "constant", "Const E As Double = 2.71828182845905", "E")
        }

        dict("Console") = {
            New LanguageMemberInfo("WriteLine", "function", "Shared Sub WriteLine(value As String)", "WriteLine($1)"),
            New LanguageMemberInfo("Write", "function", "Shared Sub Write(value As String)", "Write($1)"),
            New LanguageMemberInfo("ReadLine", "function", "Shared Function ReadLine() As String", "ReadLine()"),
            New LanguageMemberInfo("Read", "function", "Shared Function Read() As Integer", "Read()"),
            New LanguageMemberInfo("ReadKey", "function", "Shared Function ReadKey() As ConsoleKeyInfo", "ReadKey()"),
            New LanguageMemberInfo("Clear", "function", "Shared Sub Clear()", "Clear()")
        }

        dict("Convert") = {
            New LanguageMemberInfo("ToString", "function", "Shared Function ToString(value As Integer) As String", "ToString($1)"),
            New LanguageMemberInfo("ToInt32", "function", "Shared Function ToInt32(value As String) As Integer", "ToInt32($1)"),
            New LanguageMemberInfo("ToDouble", "function", "Shared Function ToDouble(value As String) As Double", "ToDouble($1)"),
            New LanguageMemberInfo("ToBoolean", "function", "Shared Function ToBoolean(value As String) As Boolean", "ToBoolean($1)"),
            New LanguageMemberInfo("ToDecimal", "function", "Shared Function ToDecimal(value As String) As Decimal", "ToDecimal($1)"),
            New LanguageMemberInfo("ToChar", "function", "Shared Function ToChar(value As String) As Char", "ToChar($1)")
        }

        Return dict
    End Function

    ' 静态共享实例（初始化一次后复用）
    Private Shared ReadOnly BuiltinTypeMembers As Dictionary(Of String, LanguageMemberInfo()) = BuildBuiltinTypeMembers()

    ' Object 类型的通用成员（所有类型的基类）
    Private Shared ReadOnly ObjectMembers As LanguageMemberInfo() = {
        New LanguageMemberInfo("ToString", "function", "Function ToString() As String", "ToString()"),
        New LanguageMemberInfo("Equals", "function", "Function Equals(obj As Object) As Boolean", "Equals($1)"),
        New LanguageMemberInfo("GetHashCode", "function", "Function GetHashCode() As Integer", "GetHashCode()"),
        New LanguageMemberInfo("GetType", "function", "Function GetType() As Type", "GetType()")
    }

#End Region

#Region "符号表"

    ''' <summary>表示一个已声明的符号</summary>
    Public Class Symbol
        Public Property Name As String
        Public Property Kind As String          ' class, module, function, sub, property, variable, field, enum, interface, structure
        Public Property TypeSignature As String ' 声明类型或返回类型
        Public Property AccessModifier As String = ""

        Public Overrides Function ToString() As String
            Return $"{Name} ({Kind})"
        End Function
    End Class

    ''' <summary>文档符号表</summary>
    Public Class SymbolTable
        Public ReadOnly Types As New List(Of Symbol)
        Public ReadOnly Methods As New List(Of Symbol)
        Public ReadOnly Properties As New List(Of Symbol)
        Public ReadOnly Variables As New List(Of Symbol)

        ''' <summary>根据名称查找变量或字段的声明类型</summary>
        Public Function FindVariableType(name As String) As String
            For Each v In Variables
                If String.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return v.TypeSignature
                End If
            Next
            For Each p In Properties
                If String.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return p.TypeSignature
                End If
            Next
            Return ""
        End Function

        ''' <summary>根据类型名称查找类型符号</summary>
        Public Function FindType(name As String) As Symbol
            For Each t In Types
                If String.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return t
                End If
            Next
            Return Nothing
        End Function
    End Class

#End Region

#Region "符号提取"

    ' 类型声明正则：Public Class Foo / Private Module Bar / Structure S / Interface I / Enum E
    Private Shared ReadOnly TypePattern As New Regex(
        "^\s*(?<access>(Public|Private|Protected|Friend|Shared|Partial|\s)*)\s*(?<kind>(Class|Module|Structure|Interface|Enum))\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    ' 方法声明正则：Public Function Foo(...) As Integer / Private Sub Bar(...)
    Private Shared ReadOnly MethodPattern As New Regex(
        "^\s*(?<access>(Public|Private|Protected|Friend|Shared|Overridable|Overrides|Overloads|MustOverride|NotOverridable|Shadows|Async|Iterator|\s)*)\s*(?<kind>(Sub|Function))\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<params>[^)]*)\)(\s+As\s+(?<retType>[A-Za-z_][A-Za-z0-9_\.\(\),\s]*))?",
        RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    ' 属性声明正则：Public Property Foo As Integer
    Private Shared ReadOnly PropertyPattern As New Regex(
        "^\s*(?<access>(Public|Private|Protected|Friend|Shared|ReadOnly|WriteOnly|Overridable|Overrides|Overloads|MustOverride|NotOverridable|Shadows|\s)*)\s*Property\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(\(\s*\))?\s*(As\s+(?<type>[A-Za-z_][A-Za-z0-9_\.\(\),\s]*))?",
        RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    ' 变量声明正则：Dim x As Integer / Const y As String = "hello" / Private field As Double
    Private Shared ReadOnly VariablePattern As New Regex(
        "^\s*(?<mod>(Dim|Const|Static|Private|Public|Protected|Friend|Shared|\s)*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(\((?<size>[^)]*)\))?\s*(As\s+(?<type>[A-Za-z_][A-Za-z0-9_\.\(\),\s]*?))?\s*(=|$)",
        RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    ''' <summary>从 VB.NET 源代码文本中提取符号表</summary>
    Public Shared Function ExtractSymbols(text As String) As SymbolTable
        Dim table As New SymbolTable
        If String.IsNullOrEmpty(text) Then Return table

        Dim lines() As String = text.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.None)

        For Each line In lines
            If String.IsNullOrWhiteSpace(line) Then Continue For
            Dim trimmed As String = line.Trim()

            ' 跳过注释行
            If trimmed.StartsWith("'") OrElse trimmed.StartsWith("REM", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            ' 检查类型声明
            Dim typeMatch = TypePattern.Match(line)
            If typeMatch.Success Then
                Dim sym As New Symbol With {
                    .Name = typeMatch.Groups("name").Value,
                    .Kind = typeMatch.Groups("kind").Value.ToLowerInvariant(),
                    .TypeSignature = typeMatch.Groups("kind").Value,
                    .AccessModifier = typeMatch.Groups("access").Value.Trim()
                }
                Select Case sym.Kind
                    Case "class", "module", "structure", "interface", "enum"
                        table.Types.Add(sym)
                End Select
                Continue For
            End If

            ' 检查方法声明
            Dim methodMatch = MethodPattern.Match(line)
            If methodMatch.Success Then
                Dim kindStr = methodMatch.Groups("kind").Value.ToLowerInvariant()
                Dim sym As New Symbol With {
                    .Name = methodMatch.Groups("name").Value,
                    .Kind = If(kindStr = "sub", "sub", "function"),
                    .TypeSignature = methodMatch.Groups("retType").Value.Trim(),
                    .AccessModifier = methodMatch.Groups("access").Value.Trim()
                }
                table.Methods.Add(sym)
                Continue For
            End If

            ' 检查属性声明
            Dim propMatch = PropertyPattern.Match(line)
            If propMatch.Success Then
                Dim sym As New Symbol With {
                    .Name = propMatch.Groups("name").Value,
                    .Kind = "property",
                    .TypeSignature = propMatch.Groups("type").Value.Trim(),
                    .AccessModifier = propMatch.Groups("access").Value.Trim()
                }
                table.Properties.Add(sym)
                Continue For
            End If

            ' 检查变量声明（Dim/Const/Static/Private field）
            Dim varMatch = VariablePattern.Match(line)
            If varMatch.Success Then
                Dim modStr = varMatch.Groups("mod").Value.Trim()
                ' 排除关键词组合中的非变量声明
                If modStr.Contains("Sub") OrElse modStr.Contains("Function") OrElse
                   modStr.Contains("Property") OrElse modStr.Contains("Class") OrElse
                   modStr.Contains("Module") Then
                    Continue For
                End If
                Dim sym As New Symbol With {
                    .Name = varMatch.Groups("name").Value,
                    .Kind = If(modStr.Contains("Const"), "constant", If(modStr.Contains("Private") OrElse modStr.Contains("Public"), "field", "variable")),
                    .TypeSignature = varMatch.Groups("type").Value.Trim(),
                    .AccessModifier = modStr
                }
                table.Variables.Add(sym)
            End If
        Next

        Return table
    End Function

#End Region

#Region "补全逻辑"

    ''' <summary>
    ''' 处理补全请求并写入 JSON 响应。
    ''' 请求体字段：language, text, line, column, trigger?
    ''' 响应体字段：{ items: [{ label, kind, detail?, documentation?, insertText? }] }
    ''' </summary>
    Public Shared Sub GetCompletions(post As HttpPOSTRequest, response As HttpResponse)
        ' 确保 CORS 头设置在写响应之前
        response.AccessControlAllowOrigin = "*"

        Dim items As New List(Of Dictionary(Of String, Object))

        Try
            ' 从 POST body 中提取请求字段
            Dim body As Dictionary(Of String, Object) = post.POSTData.Objects

            If body Is Nothing OrElse body.Count = 0 Then
                WriteJsonResponse(response, items)
                Return
            End If

            Dim language As String = GetStringField(body, "language")
            Dim text As String = GetStringField(body, "text")
            Dim lineNum As Integer = GetIntField(body, "line")
            Dim colNum As Integer = GetIntField(body, "column")
            Dim trigger As String = GetStringField(body, "trigger")

            ' 只处理 vbnet 语言；其他语言返回空列表
            If Not String.Equals(language, "vbnet", StringComparison.OrdinalIgnoreCase) Then
                WriteJsonResponse(response, items)
                Return
            End If

            ' 提取文档符号表
            Dim symbols As SymbolTable = ExtractSymbols(text)

            ' 获取当前行文本
            Dim lines() As String = If(String.IsNullOrEmpty(text), {}, text.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.None))
            Dim currentLine As String = If(lineNum >= 0 AndAlso lineNum < lines.Length, lines(lineNum), "")

            ' 根据触发方式生成补全项
            If trigger = "." Then
                ' 点号触发：成员补全
                items.AddRange(GetMemberCompletions(currentLine, colNum, symbols))
            Else
                ' 无触发或 Ctrl+Space：全局补全
                Dim prefix As String = ExtractPrefix(currentLine, colNum)
                items.AddRange(GetGlobalCompletions(prefix, symbols))
            End If

        Catch ex As Exception
            ' 出错时返回空列表，让前端优雅降级
            Try
                Console.Error.WriteLine(ex.ToString())
            Catch
            End Try
        End Try

        WriteJsonResponse(response, items)
    End Sub

    ''' <summary>全局补全：关键词 + 类型 + 片段 + 文档符号，按前缀过滤</summary>
    Private Shared Function GetGlobalCompletions(prefix As String, symbols As SymbolTable) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        ' 1. 关键词
        For Each kw In Keywords
            If ShouldInclude(kw, prefix) AndAlso Not seen.Contains(kw) Then
                result.Add(MakeItem(kw, "keyword", insertText:=kw))
                seen.Add(kw)
            End If
        Next
        For Each kw In ControlKeywords
            If ShouldInclude(kw, prefix) AndAlso Not seen.Contains(kw) Then
                result.Add(MakeItem(kw, "keyword", insertText:=kw))
                seen.Add(kw)
            End If
        Next

        ' 2. 内置类型
        For Each t In BuiltinTypes
            If ShouldInclude(t, prefix) AndAlso Not seen.Contains(t) Then
                result.Add(MakeItem(t, "type", detail:="内置类型", insertText:=t))
                seen.Add(t)
            End If
        Next

        ' 3. 框架类型
        For Each t In FrameworkTypes
            If ShouldInclude(t, prefix) AndAlso Not seen.Contains(t) Then
                result.Add(MakeItem(t, "class", detail:="System 命名空间类型", insertText:=t))
                seen.Add(t)
            End If
        Next

        ' 4. 代码片段
        For Each snip In Snippets
            If ShouldInclude(snip.label, prefix) AndAlso Not seen.Contains(snip.label) Then
                result.Add(MakeItem(snip.label, "snippet", snip.detail, snip.insertText))
                seen.Add(snip.label)
            End If
        Next

        ' 5. 文档中的类型符号
        For Each sym In symbols.Types
            If ShouldInclude(sym.Name, prefix) AndAlso Not seen.Contains(sym.Name) Then
                result.Add(MakeItem(sym.Name, sym.Kind, detail:=$"文档中声明的 {sym.Kind}", insertText:=sym.Name))
                seen.Add(sym.Name)
            End If
        Next

        ' 6. 文档中的方法
        For Each sym In symbols.Methods
            If ShouldInclude(sym.Name, prefix) AndAlso Not seen.Contains(sym.Name) Then
                Dim detail As String = If(String.IsNullOrEmpty(sym.TypeSignature),
                    sym.Kind,
                    $"{sym.Kind} As {sym.TypeSignature}")
                result.Add(MakeItem(sym.Name, sym.Kind, detail, insertText:=$"{sym.Name}()"))
                seen.Add(sym.Name)
            End If
        Next

        ' 7. 文档中的属性
        For Each sym In symbols.Properties
            If ShouldInclude(sym.Name, prefix) AndAlso Not seen.Contains(sym.Name) Then
                Dim detail As String = If(String.IsNullOrEmpty(sym.TypeSignature),
                    "property",
                    $"property As {sym.TypeSignature}")
                result.Add(MakeItem(sym.Name, "property", detail, insertText:=sym.Name))
                seen.Add(sym.Name)
            End If
        Next

        ' 8. 文档中的变量/字段/常量
        For Each sym In symbols.Variables
            If ShouldInclude(sym.Name, prefix) AndAlso Not seen.Contains(sym.Name) Then
                Dim detail As String = If(String.IsNullOrEmpty(sym.TypeSignature),
                    sym.Kind,
                    $"{sym.Kind} As {sym.TypeSignature}")
                result.Add(MakeItem(sym.Name, sym.Kind, detail, insertText:=sym.Name))
                seen.Add(sym.Name)
            End If
        Next

        Return result
    End Function

    ''' <summary>点号触发补全：提取 . 前的标识符，返回其类型的成员</summary>
    Private Shared Function GetMemberCompletions(currentLine As String, column As Integer, symbols As SymbolTable) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))

        ' 提取点号前的标识符
        Dim identifier As String = ExtractIdentifierBeforeDot(currentLine, column)
        If String.IsNullOrEmpty(identifier) Then
            Return result
        End If

        ' 特殊关键字
        If String.Equals(identifier, "Me", StringComparison.OrdinalIgnoreCase) Then
            ' Me 关键字：返回当前类型的所有成员（文档符号）
            AddMembers(result, GetDocumentMembers(symbols))
            Return result
        End If

        If String.Equals(identifier, "MyBase", StringComparison.OrdinalIgnoreCase) OrElse
           String.Equals(identifier, "MyClass", StringComparison.OrdinalIgnoreCase) Then
            ' MyBase/MyClass：返回 Object 成员 + 文档成员
            AddMembers(result, GetBuiltinTypeMembersList("Object"))
            AddMembers(result, GetDocumentMembers(symbols))
            Return result
        End If

        ' 在文档符号表中查找该标识符的类型
        Dim typeName As String = symbols.FindVariableType(identifier)

        If Not String.IsNullOrEmpty(typeName) Then
            ' 清理类型名（去掉数组括号、泛型参数等）
            typeName = CleanTypeName(typeName)

            ' 先查内置类型成员
            AddMembers(result, GetBuiltinTypeMembersList(typeName))

            ' 再查文档中是否有同名类型，返回其成员
            Dim typeSym = symbols.FindType(typeName)
            If typeSym IsNot Nothing Then
                AddMembers(result, GetDocumentMembersForType(symbols, typeName))
            End If

            If result.Count > 0 Then
                Return result
            End If
        End If

        ' 查找文档中是否有同名类型
        Dim docType = symbols.FindType(identifier)
        If docType IsNot Nothing Then
            AddMembers(result, GetDocumentMembersForType(symbols, identifier))
            Return result
        End If

        ' 如果是已知框架类型
        Dim builtinMembers = GetBuiltinTypeMembersList(identifier)
        If builtinMembers.Count > 0 Then
            AddMembers(result, builtinMembers)
            Return result
        End If

        ' 无法解析类型时返回通用 Object 成员
        AddMembers(result, GetBuiltinTypeMembersList("Object"))

        Return result
    End Function

    ''' <summary>获取内置类型的成员列表</summary>
    Private Shared Function GetBuiltinTypeMembersList(typeName As String) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))

        If String.Equals(typeName, "Object", StringComparison.OrdinalIgnoreCase) Then
            For Each m In ObjectMembers
                result.Add(MakeItem(m.Label, m.Kind, m.Detail, m.InsertText))
            Next
            Return result
        End If

        Dim members As LanguageMemberInfo() = Nothing
        If BuiltinTypeMembers.TryGetValue(typeName, members) Then
            For Each m In members
                result.Add(MakeItem(m.Label, m.Kind, m.Detail, m.InsertText))
            Next
        End If

        Return result
    End Function

    ''' <summary>获取文档中所有方法/属性/变量作为成员</summary>
    Private Shared Function GetDocumentMembers(symbols As SymbolTable) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))

        For Each m In symbols.Methods
            Dim detail As String = If(String.IsNullOrEmpty(m.TypeSignature), m.Kind, $"{m.Kind} As {m.TypeSignature}")
            result.Add(MakeItem(m.Name, m.Kind, detail, insertText:=$"{m.Name}()"))
        Next
        For Each p In symbols.Properties
            Dim detail As String = If(String.IsNullOrEmpty(p.TypeSignature), "property", $"property As {p.TypeSignature}")
            result.Add(MakeItem(p.Name, "property", detail, insertText:=p.Name))
        Next
        For Each v In symbols.Variables
            Dim detail As String = If(String.IsNullOrEmpty(v.TypeSignature), v.Kind, $"{v.Kind} As {v.TypeSignature}")
            result.Add(MakeItem(v.Name, v.Kind, detail, insertText:=v.Name))
        Next

        Return result
    End Function

    ''' <summary>获取文档中指定类型的成员</summary>
    Private Shared Function GetDocumentMembersForType(symbols As SymbolTable, typeName As String) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))

        ' 简化实现：返回所有方法和属性作为该类型的成员
        For Each m In symbols.Methods
            Dim detail As String = If(String.IsNullOrEmpty(m.TypeSignature), m.Kind, $"{m.Kind} As {m.TypeSignature}")
            result.Add(MakeItem(m.Name, m.Kind, detail, insertText:=$"{m.Name}()"))
        Next
        For Each p In symbols.Properties
            Dim detail As String = If(String.IsNullOrEmpty(p.TypeSignature), "property", $"property As {p.TypeSignature}")
            result.Add(MakeItem(p.Name, "property", detail, insertText:=p.Name))
        Next

        Return result
    End Function

    ''' <summary>将一个成员列表添加到结果中</summary>
    Private Shared Sub AddMembers(target As List(Of Dictionary(Of String, Object)), source As List(Of Dictionary(Of String, Object)))
        If source IsNot Nothing Then
            target.AddRange(source)
        End If
    End Sub

#End Region

#Region "辅助方法"

    ''' <summary>创建一个补全项字典（确保 JSON 字段名为 camelCase）</summary>
    Private Shared Function MakeItem(label As String, kind As String, Optional detail As String = "", Optional insertText As String = "") As Dictionary(Of String, Object)
        Dim item As New Dictionary(Of String, Object) From {
            {"label", label},
            {"kind", kind}
        }
        If Not String.IsNullOrEmpty(detail) Then
            item("detail") = detail
        End If
        If Not String.IsNullOrEmpty(insertText) Then
            item("insertText") = insertText
        End If
        Return item
    End Function

    ''' <summary>
    ''' 手动构建 JSON 字符串并写入响应。
    ''' 绕过 DataContractJsonSerializer 对 Dictionary(Of String, Object) 的已知类型限制。
    ''' </summary>
    Private Shared Sub WriteJsonResponse(response As HttpResponse, items As List(Of Dictionary(Of String, Object)))
        ' 手动构建 JSON 字符串
        Dim sb As New StringBuilder(1024)
        sb.Append("{""items"":[")
        For i As Integer = 0 To items.Count - 1
            If i > 0 Then sb.Append(",")
            sb.Append("{")
            Dim item = items(i)
            Dim first As Boolean = True
            For Each kvp As KeyValuePair(Of String, Object) In item
                If Not first Then sb.Append(",")
                sb.Append("""").Append(EscapeJsonString(kvp.Key)).Append(""":")
                sb.Append("""").Append(EscapeJsonString(CStr(kvp.Value))).Append("""")
                first = False
            Next
            sb.Append("}")
        Next
        sb.Append("]}")

        ' 写入 HTTP 响应
        Dim jsonBytes As Byte() = Encoding.UTF8.GetBytes(sb.ToString())
        Call response.WriteHttp(New Content With {.length = jsonBytes.Length, .type = "application/json"})
        Call response.Write(jsonBytes)
    End Sub

    ''' <summary>转义 JSON 字符串中的特殊字符</summary>
    Private Shared Function EscapeJsonString(s As String) As String
        If s Is Nothing Then Return ""
        Dim sb As New StringBuilder(s.Length)
        For Each ch As Char In s
            Select Case ch
                Case "\"c
                    sb.Append("\\")
                Case """"c
                    sb.Append("\""")
                Case ControlChars.Cr
                    sb.Append("\r")
                Case ControlChars.Lf
                    sb.Append("\n")
                Case ControlChars.Tab
                    sb.Append("\t")
                Case ControlChars.Back
                    sb.Append("\b")
                Case ControlChars.FormFeed
                    sb.Append("\f")
                Case Else
                    If AscW(ch) < 32 Then
                        sb.Append("\u" & AscW(ch).ToString("x4"))
                    Else
                        sb.Append(ch)
                    End If
            End Select
        Next
        Return sb.ToString()
    End Function

    ''' <summary>判断补全项是否应该包含在结果中（按前缀过滤）</summary>
    Private Shared Function ShouldInclude(candidate As String, prefix As String) As Boolean
        If String.IsNullOrEmpty(prefix) Then Return True
        Return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>从当前行中提取光标位置前的标识符前缀</summary>
    Private Shared Function ExtractPrefix(line As String, column As Integer) As String
        If String.IsNullOrEmpty(line) Then Return ""
        Dim col = Math.Min(column, line.Length)
        Dim sb As New Text.StringBuilder()
        ' 从光标位置往前扫描，收集标识符字符
        For i As Integer = col - 1 To 0 Step -1
            Dim ch = line(i)
            If Char.IsLetterOrDigit(ch) OrElse ch = "_"c Then
                sb.Insert(0, ch)
            Else
                Exit For
            End If
        Next
        Return sb.ToString()
    End Function

    ''' <summary>从当前行中提取点号前的标识符</summary>
    Private Shared Function ExtractIdentifierBeforeDot(line As String, column As Integer) As String
        If String.IsNullOrEmpty(line) Then Return ""
        Dim col = Math.Min(column, line.Length)
        ' 先跳过点号和空白
        Dim i As Integer = col - 1
        ' 跳过点号本身
        While i >= 0 AndAlso (line(i) = "."c OrElse Char.IsWhiteSpace(line(i)))
            i -= 1
        End While
        ' 收集标识符字符
        Dim sb As New Text.StringBuilder()
        While i >= 0
            Dim ch = line(i)
            If Char.IsLetterOrDigit(ch) OrElse ch = "_"c Then
                sb.Insert(0, ch)
                i -= 1
            Else
                Exit While
            End If
        End While
        Return sb.ToString()
    End Function

    ''' <summary>清理类型名：去掉数组括号、泛型参数等</summary>
    Private Shared Function CleanTypeName(typeName As String) As String
        If String.IsNullOrEmpty(typeName) Then Return ""
        Dim t = typeName.Trim()
        ' 去掉数组括号 () 或 (,)
        Dim parenIdx = t.IndexOf("("c)
        If parenIdx >= 0 Then
            t = t.Substring(0, parenIdx).Trim()
        End If
        ' 去掉泛型参数 (Of ...)
        Dim ofIdx = t.IndexOf("(Of", StringComparison.OrdinalIgnoreCase)
        If ofIdx >= 0 Then
            t = t.Substring(0, ofIdx).Trim()
        End If
        ' 去掉命名空间前缀，只保留类型名
        Dim dotIdx = t.LastIndexOf("."c)
        If dotIdx >= 0 Then
            t = t.Substring(dotIdx + 1).Trim()
        End If
        Return t
    End Function

    ''' <summary>从 JSON 字典中安全获取字符串字段</summary>
    Private Shared Function GetStringField(body As Dictionary(Of String, Object), key As String) As String
        If body.ContainsKey(key) AndAlso body(key) IsNot Nothing Then
            Return CStr(body(key))
        End If
        Return ""
    End Function

    ''' <summary>从 JSON 字典中安全获取整数字段（JSON 数字可能被解析为 Double）</summary>
    Private Shared Function GetIntField(body As Dictionary(Of String, Object), key As String) As Integer
        If body.ContainsKey(key) AndAlso body(key) IsNot Nothing Then
            Dim val = body(key)
            If TypeOf val Is Double Then
                Return CInt(CDbl(val))
            ElseIf TypeOf val Is Integer Then
                Return CInt(val)
            Else
                Return CInt(val(CStr(val)))
            End If
        End If
        Return 0
    End Function

#End Region

End Class
