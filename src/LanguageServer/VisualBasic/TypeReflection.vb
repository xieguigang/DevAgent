Imports System.Reflection

Module TypeReflection

    Public Iterator Function ParseTypeMembers(type As Type) As IEnumerable(Of LanguageMemberInfo)
        For Each prop As PropertyInfo In type.GetProperties

        Next
        For Each func As MethodInfo In type.GetMethods

        Next
        For Each field As FieldInfo In type.GetFields

        Next
    End Function
End Module
