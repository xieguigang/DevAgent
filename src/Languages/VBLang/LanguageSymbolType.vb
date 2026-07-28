Imports System.IO
Imports Microsoft.VisualBasic.Scripting.MetaData

Public MustInherit Class LanguageSymbolType

    Public MustOverride ReadOnly Property Type As SymbolType
    ''' <summary>
    ''' the symbol name
    ''' </summary>
    ''' <returns></returns>
    Public Property Name As String
    Public Property Parent As ContainerType
    ''' <summary>
    ''' generic type argument for XXX(Of T)
    ''' </summary>
    ''' <returns></returns>
    Public Property GenericTypeArguments As TypeInfo()

    ''' <summary>
    ''' access and custom modifiers, e.g. "Public Shared Overloads"
    ''' </summary>
    Public Property Modifiers As String
    ''' <summary>
    ''' attribute declaration blocks applied on this symbol, e.g. &lt;ExportAPI()&gt;
    ''' </summary>
    Public Property Attributes As List(Of String)
    ''' <summary>
    ''' the xml documentation comment lines (''') that precedes this symbol
    ''' </summary>
    Public Property XmlDoc As String

End Class

''' <summary>
''' Dim XXX As XXX variable symbol inside a function/sub/operator/sub new
''' </summary>
Public Class VariableSymbolType : Inherits LanguageSymbolType

    Public Property ValueType As TypeInfo

    Public Overrides ReadOnly Property Type As SymbolType
        Get
            Return SymbolType.Variable
        End Get
    End Property

End Class

Public Class DelegateType : Inherits LanguageSymbolType

    Public Overrides ReadOnly Property Type As SymbolType
        Get
            Return SymbolType.Delegate
        End Get
    End Property

    Public Property Parameters As Dictionary(Of String, TypeInfo)
    Public Property ValueType As TypeInfo

End Class

''' <summary>
''' function/sub/operator/property
''' </summary>
Public Class InvokeSymbolType : Inherits ContainerType

    Public Property Parameters As Dictionary(Of String, TypeInfo)
    Public Property ReturnType As TypeInfo

    Public Sub New(type As SymbolType)
        MyBase.New(type)
    End Sub
End Class

Public Class ContainerType : Inherits LanguageSymbolType

    Public Overrides ReadOnly Property Type As SymbolType

    Public Property InternalNested As Dictionary(Of String, LanguageSymbolType)

    ''' <summary>
    ''' member of class/structure/interface/module/namespace container type
    ''' variable inside a function/sub/operator/property
    ''' </summary>
    ''' <returns></returns>
    Public Property Members As Dictionary(Of String, LanguageSymbolType)

    ''' <summary>
    ''' the base type from the Inherits clause
    ''' </summary>
    Public Property InheritsType As TypeInfo
    ''' <summary>
    ''' the implemented interfaces from the Implements clause
    ''' </summary>
    Public Property ImplementsInterfaces As TypeInfo()
    ''' <summary>
    ''' the underlying base type of an enum, e.g. Enum X As Long
    ''' </summary>
    Public Property EnumBaseType As TypeInfo

    Sub New(type As SymbolType)
        Select Case type
            Case SymbolType.Delegate : Throw New InvalidDataException("delegate type is not a symbol container type!")
            Case SymbolType.Variable : Throw New InvalidDataException("variable symbol is not a symbol container type!")
            Case SymbolType.Event : Throw New InvalidDataException("event symbol could not be used as a symbol container type!")
            Case Else
                _Type = type
        End Select
    End Sub
End Class

Public Enum SymbolType
    [Class]     ' - Class XXX
    [Module]    ' - Module XXX
    [Structure] ' - Structure XXX
    [Enum]      ' - Enum XXX
    [Interface] ' - Interface XXX
    [Namespace] ' - Namespace XXX
    [Operator]  ' - Operator +(x As X, y As Y) As XX
    [Function]  ' - Function AAA(x As XX) As XXX
    [Sub]       ' - Sub AAA(x As XX)
    [Property]  ' - Property X As XX
    [Field]     ' - Public/Private/Dim X As XX (A type member)
    [Event]     ' - Event AAA(x As XX)
    [New]       ' - Sub New()
    [Delegate]  ' - Public Delegate Function AAA(x As XX) As XXX
    Variable    ' - Dim X As XX (variable symbol inside a method/function/operator/sub new)
End Enum