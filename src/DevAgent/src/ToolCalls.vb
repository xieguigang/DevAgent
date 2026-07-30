Imports System.Runtime.CompilerServices
Imports Ollama

Public Module ToolCalls

    <Extension>
    Public Sub HookReadOnlyFileSystem(llm As LLMClient, fs As AgentTools)
        llm.AddFunction(fs, "read_file")
        llm.AddFunction(fs, "list_files")
        llm.AddFunction(fs, "file_exists")
        llm.AddFunction(fs, "search_files")
        llm.AddFunction(fs, "get_project_tree")
    End Sub
End Module
