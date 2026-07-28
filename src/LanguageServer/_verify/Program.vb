Imports System.Reflection

Module Program
    Sub Main()
        Dim cases As New System.Collections.Generic.Dictionary(Of String, Type) From {
            {"String", GetType(String)},
            {"Integer", GetType(Integer)},
            {"List", GetType(Global.System.Collections.Generic.List(Of))},
            {"Dictionary", GetType(Global.System.Collections.Generic.Dictionary(Of,))},
            {"Math", GetType(Math)},
            {"DateTime", GetType(Date)},
            {"Regex", Nothing}
        }

        For Each kv In cases
            Dim t As Type = kv.Value
            If t Is Nothing Then t = TypeReflection.ResolveType(kv.Key)
            If t Is Nothing Then
                Console.WriteLine($"=== {kv.Key}: UNRESOLVED ===")
                Continue For
            End If
            Console.WriteLine($"=== {kv.Key} ({t.FullName}) : {TypeReflection.ParseTypeMembers(t).Count()} members ===")
            For Each m In TypeReflection.ParseTypeMembers(t)
                Console.WriteLine($"  [{m.Kind}] {m.Detail}  ->  {m.InsertText}")
            Next
            Console.WriteLine()
        Next
    End Sub
End Module
