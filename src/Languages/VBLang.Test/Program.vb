Imports System
Imports System.IO
Imports VBLang.Syntax

Module Program

    Sub Main()
        Try
            TestProject()
        Catch ex As Exception
            Console.WriteLine("[WARN] VBProject.Load test failed: " & ex.Message)
        End Try

        Dim src As String = "
Imports System

''' <summary>demo namespace</summary>
Namespace DemoApp

    <Serializable>
    Public Delegate Function Transformer(Of T)(input As T) As T

    ''' <summary>demo class</summary>
    <ExportAPI>
    Public Class DemoClass(Of T As Class)
        Inherits BaseClass
        Implements IDisposable, IComparable

        Public Property Name As String
        Private _value As Integer

        Public Sub New()
            _value = 0
        End Sub

        Public Function Compute(x As Integer, _
                                y As T) As Integer
            Dim a As Integer = x
            Dim b, c As Double
            Const max As Long = 100L
            Return a
        End Function

        Public Shared Operator +(left As DemoClass(Of T), right As DemoClass(Of T)) As DemoClass(Of T)
            Return left
        End Operator

        Private Enum InnerEnum As Byte
            First
            Second = 5
        End Enum
    End Class

End Namespace
"

        Dim root As ContainerType = VBParser.Parse(src)
        Dump(root, 0)

        Dim failures As New List(Of String)
        RunAsserts(root, failures)

        If failures.Count = 0 Then
            Console.WriteLine(vbCrLf & "ALL TESTS PASSED")
        Else
            Console.WriteLine(vbCrLf & "FAILURES:")
            For Each f In failures
                Console.WriteLine("  - " & f)
            Next
            Environment.Exit(1)
        End If
    End Sub

    Sub RunAsserts(root As ContainerType, failures As List(Of String))
        Dim ns = CType(root.InternalNested("DemoApp"), ContainerType)
        Assert(ns IsNot Nothing AndAlso ns.Type = SymbolType.[Namespace], "namespace DemoApp", failures)

        Dim del As LanguageSymbolType = Nothing
        If ns.Members.TryGetValue("Transformer", del) Then
            Dim delT = CType(del, DelegateType)
            Assert(delT.Parameters IsNot Nothing AndAlso delT.Parameters.ContainsKey("input"), "delegate parameter input", failures)
            Assert(delT.ValueType IsNot Nothing AndAlso delT.ValueType.fullName = "T", "delegate return T", failures)
        Else
            Assert(False, "delegate Transformer present", failures)
        End If

        Dim cls = CType(ns.InternalNested("DemoClass"), ContainerType)
        Assert(cls IsNot Nothing AndAlso cls.Type = SymbolType.[Class], "class DemoClass", failures)
        Assert(cls.GenericTypeArguments IsNot Nothing AndAlso cls.GenericTypeArguments.Length = 1, "class generic T", failures)
        Assert(cls.InheritsType IsNot Nothing AndAlso cls.InheritsType.fullName = "BaseClass", "Inherits BaseClass", failures)
        Assert(cls.ImplementsInterfaces IsNot Nothing AndAlso cls.ImplementsInterfaces.Length = 2, "Implements 2 interfaces", failures)
        Assert(cls.Attributes IsNot Nothing AndAlso cls.Attributes.Contains("ExportAPI"), "attribute ExportAPI", failures)

        Dim prop = cls.Members("Name")
        Assert(prop IsNot Nothing AndAlso TypeOf prop Is InvokeSymbolType, "property Name", failures)

        Dim ctor = cls.Members("New")
        Assert(ctor IsNot Nothing AndAlso CType(ctor, InvokeSymbolType).Type = SymbolType.[New], "Sub New", failures)

        Dim fn = CType(cls.Members("Compute"), InvokeSymbolType)
        Assert(fn IsNot Nothing, "function Compute", failures)
        Assert(fn.Parameters.ContainsKey("x") AndAlso fn.Parameters("x").fullName = "Integer", "param x As Integer", failures)
        Assert(fn.Parameters.ContainsKey("y") AndAlso fn.Parameters("y").fullName = "T", "param y As T", failures)
        Assert(fn.ReturnType IsNot Nothing AndAlso fn.ReturnType.fullName = "Integer", "return Integer", failures)
        Assert(fn.Members.ContainsKey("a") AndAlso CType(fn.Members("a"), VariableSymbolType).ValueType.fullName = "Integer", "local a", failures)
        Assert(fn.Members.ContainsKey("b") AndAlso CType(fn.Members("b"), VariableSymbolType).ValueType.fullName = "Double", "local b", failures)
        Assert(fn.Members.ContainsKey("c") AndAlso CType(fn.Members("c"), VariableSymbolType).ValueType.fullName = "Double", "local c (shared type)", failures)
        Assert(fn.Members.ContainsKey("max") AndAlso CType(fn.Members("max"), VariableSymbolType).ValueType.fullName = "Long", "local max", failures)

        Dim op = cls.Members("+")
        Assert(op IsNot Nothing AndAlso CType(op, InvokeSymbolType).Type = SymbolType.[Operator], "operator +", failures)
        Assert(CType(op, InvokeSymbolType).ReturnType.fullName = "DemoClass(Of T)", "operator return type", failures)

        Assert(cls.Members.ContainsKey("_value"), "field _value", failures)

        Dim en = CType(cls.InternalNested("InnerEnum"), ContainerType)
        Assert(en IsNot Nothing AndAlso en.Type = SymbolType.[Enum], "nested enum", failures)
        Assert(en.EnumBaseType IsNot Nothing AndAlso en.EnumBaseType.fullName = "Byte", "enum base Byte", failures)
    End Sub

    Sub TestProject()
        Dim baseDir = AppContext.BaseDirectory
        Dim dir = baseDir
        Dim vbprojPath As String = Nothing
        While dir IsNot Nothing
            Dim cand = Path.Combine(dir, "VBLang", "VBLang.vbproj")
            If File.Exists(cand) Then
                vbprojPath = cand
                Exit While
            End If
            dir = Path.GetDirectoryName(dir)
        End While

        If vbprojPath Is Nothing Then
            Console.WriteLine("[SKIP] VBProject.Load test: VBLang.vbproj not found")
            Return
        End If

        Console.WriteLine(vbCrLf & "--- VBProject.Load ---")
        Console.WriteLine("vbproj: " & vbprojPath)

        Dim proj As VBProject = VBProject.Load(vbprojPath)
        Console.WriteLine("RootNamespace : " & proj.RootNamespace)
        Console.WriteLine("AssemblyName  : " & proj.AssemblyName)
        Console.WriteLine("OutputType    : " & proj.OutputType)
        Console.WriteLine("Compile files : " & If(proj.CompileFiles, New VBDocument() {}).Length)

        For Each doc In proj.CompileFiles
            Console.WriteLine("  " & doc.FileName & "  ->  " & doc.Types.Count & " top-level types, " & doc.Imports.Length & " imports")
        Next

        Console.WriteLine(vbCrLf & "--- VBProject.GetType ---")
        Dim probes As String() = {
            "VBLang.VBDocument",
            "VBLang.VBProject",
            "VBLang.Syntax.VBParser",
            "VBLang.LanguageSymbolType",
            "VBLang.Syntax.TokenKind",
            "VBLang.Syntax.VBScanner",
            "VBLang.VBDocument(Of T)",
            "VBParser",
            "NotExist.Type"
        }
        For Each p In probes
            Dim sym = proj.GetType(p)
            If sym Is Nothing Then
                Console.WriteLine("  " & p & " -> NOT FOUND")
            Else
                Console.WriteLine("  " & p & " -> " & sym.Type.ToString() & " " & sym.Name)
            End If
        Next
    End Sub

    Sub Assert(cond As Boolean, label As String, failures As List(Of String))
        If Not cond Then
            failures.Add(label)
        End If
        Console.WriteLine((If(cond, "[OK]   ", "[FAIL] ")) & label)
    End Sub

    Sub Dump(c As ContainerType, indent As Integer)
        Dim pad As String = New String(" "c, indent * 2)
        Console.WriteLine($"{pad}{c.Type} {c.Name} (generic={If(c.GenericTypeArguments Is Nothing, 0, c.GenericTypeArguments.Length)})")
        If c.InheritsType IsNot Nothing Then Console.WriteLine($"{pad}  Inherits {c.InheritsType.fullName}")
        If c.ImplementsInterfaces IsNot Nothing Then
            For Each i In c.ImplementsInterfaces
                Console.WriteLine($"{pad}  Implements {i.fullName}")
            Next
        End If
        If c.InternalNested IsNot Nothing Then
            For Each kv In c.InternalNested
                If TypeOf kv.Value Is ContainerType Then Dump(CType(kv.Value, ContainerType), indent + 1)
            Next
        End If
        If c.Members IsNot Nothing Then
            For Each kv In c.Members
                Dim m = kv.Value
                If TypeOf m Is ContainerType Then
                    Dump(CType(m, ContainerType), indent + 1)
                ElseIf TypeOf m Is InvokeSymbolType Then
                    Dim inv = CType(m, InvokeSymbolType)
                    Console.WriteLine($"{pad}  {inv.Type} {inv.Name} As {If(inv.ReturnType Is Nothing, "-", inv.ReturnType.fullName)}")
                ElseIf TypeOf m Is DelegateType Then
                    Dim d = CType(m, DelegateType)
                    Console.WriteLine($"{pad}  Delegate {d.Name} As {If(d.ValueType Is Nothing, "-", d.ValueType.fullName)}")
                ElseIf TypeOf m Is VariableSymbolType Then
                    Dim v = CType(m, VariableSymbolType)
                    Console.WriteLine($"{pad}  var {v.Name} As {If(v.ValueType Is Nothing, "-", v.ValueType.fullName)}")
                Else
                    Console.WriteLine($"{pad}  {m.Type} {m.Name}")
                End If
            Next
        End If
    End Sub

End Module
