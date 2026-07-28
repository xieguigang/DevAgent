Imports System.IO
Imports RLang
Imports RLang.Syntax
Imports VBLang

Module Program

    Sub Main()
        TestParseDocument()
        TestParseRproj()
        Console.WriteLine()
        Console.WriteLine("All RLang parser checks completed.")
    End Sub

    Private Sub TestParseDocument()
        Console.WriteLine("=== ParseDocument ===")

        Dim script = "
# a top level comment
require(ggplot2)
library(dplyr)

x <- 10
config <- list(a = 1, b = 2)

add <- function(a, b = 1) {
  total <- a + b
  helper <- function() {
    return(total)
  }
  helper()
}

multiply <- function(x, y) {
  x * y
}
"

        Dim doc = Parser.ParseDocument(script)

        Console.WriteLine("Imports: " & String.Join(", ", doc.Imports))
        AssertEquals(2, doc.Imports.Length, "imports count")

        Dim fns = Parser.GetFunctions(doc)
        Dim vars = Parser.GetVariables(doc)

        Console.WriteLine("Functions: " & String.Join(", ", fns.ConvertAll(Function(f) f.Name)))
        Console.WriteLine("Variables: " & String.Join(", ", vars.ConvertAll(Function(v) v.Name)))
        AssertEquals(2, fns.Count, "top-level function count")
        AssertEquals(2, vars.Count, "top-level variable count")
        Assert(vars.Exists(Function(v) v.Name = "x"), "variable 'x'")
        Assert(vars.Exists(Function(v) v.Name = "config"), "variable 'config'")

        Dim add = fns.Find(Function(f) f.Name = "add")
        Assert(add IsNot Nothing, "function 'add' found")
        AssertEquals(2, add.Parameters.Count, "add parameter count")
        Assert(add.Parameters.ContainsKey("a"), "add has param a")
        Assert(add.Parameters.ContainsKey("b"), "add has param b")

        Assert(add.NestedFunctions IsNot Nothing AndAlso add.NestedFunctions.Count = 1, "add has 1 nested function")
        If add.NestedFunctions IsNot Nothing Then
            Console.WriteLine("  nested in add: " & add.NestedFunctions(0).Name)
            Assert(add.NestedFunctions(0).Name = "helper", "nested function name is 'helper'")
        End If

        Assert(add.Locals IsNot Nothing AndAlso add.Locals.ContainsKey("total"), "add has local 'total'")
    End Sub

    Private Sub TestParseRproj()
        Console.WriteLine()
        Console.WriteLine("=== ParseRproj (virtual VBProject) ===")

        Dim pkgDir = Path.Combine(Path.GetTempPath(), "rlang_parser_sample_" & Guid.NewGuid().ToString("N"))
        Dim rDir = Path.Combine(pkgDir, "R")
        Directory.CreateDirectory(rDir)

        File.WriteAllText(Path.Combine(pkgDir, "DESCRIPTION"), "
Package: mypkg
Type: Package
Title: My Example Package
Version: 0.1.0
Author: Jane Doe <jane@example.com>
Maintainer: Jane Doe <jane@example.com>
Description: An example R package used for parser testing.
License: GPL-3
Encoding: UTF-8
RoxygenNote: 7.2.3
URL: https://example.com/mypkg
BugReports: https://example.com/mypkg/issues
Depends: R (>= 3.5.0)
Imports:
    ggplot2,
    dplyr
Suggests: testthat
")

        File.WriteAllText(Path.Combine(rDir, "math.R"), "
library(stats)

square <- function(x) {
  x * x
}
")

        File.WriteAllText(Path.Combine(rDir, "io.R"), "
require(readr)

load_data <- function(path) {
  read_csv(path)
}
")

        Try
            Dim proj = Parser.ParseRproj(rDir)

            Console.WriteLine("RootNamespace: " & proj.RootNamespace)
            Console.WriteLine("AssemblyName: " & proj.AssemblyName)
            Console.WriteLine("Sdk: " & proj.Sdk)
            Console.WriteLine("OutputType: " & proj.OutputType)
            Console.WriteLine("NuGet.PackageId: " & proj.NuGet.PackageId)
            Console.WriteLine("NuGet.Version: " & proj.NuGet.Version)
            Console.WriteLine("NuGet.Authors: " & proj.NuGet.Authors)
            Console.WriteLine("Metadata.Title: " & proj.Metadata.Other("Title"))
            Console.WriteLine("NuGet.License: " & proj.NuGet.Other("License"))

            AssertEquals("mypkg", proj.RootNamespace, "RootNamespace")
            AssertEquals("mypkg", proj.AssemblyName, "AssemblyName")
            AssertEquals("mypkg", proj.NuGet.PackageId, "NuGet.PackageId")
            AssertEquals("0.1.0", proj.NuGet.Version, "NuGet.Version")
            AssertEquals("R.Package", proj.Sdk, "Sdk")

            Assert(proj.CompileFiles IsNot Nothing AndAlso proj.CompileFiles.Length = 2, "CompileFiles count = 2")
            If proj.CompileFiles IsNot Nothing Then
                For Each d In proj.CompileFiles
                    Console.WriteLine("  compile: " & d.FileName & " (imports: " & String.Join(",", d.Imports) & ")")
                Next
            End If

            Assert(proj.PackageReferences IsNot Nothing, "PackageReferences not nothing")
            If proj.PackageReferences IsNot Nothing Then
                Dim ids = Array.ConvertAll(proj.PackageReferences, Function(p) p.Id)
                Console.WriteLine("PackageReferences: " & String.Join(", ", ids))
                ' R dependency must be dropped; ggplot2, dplyr, testthat kept
                Assert(Not ids.Contains("R"), "R dependency dropped from PackageReferences")
                Assert(ids.Contains("ggplot2"), "ggplot2 in PackageReferences")
                Assert(ids.Contains("dplyr"), "dplyr in PackageReferences")
                Assert(ids.Contains("testthat"), "testthat in PackageReferences")
                AssertEquals(3, proj.PackageReferences.Length, "PackageReferences count = 3")
            End If
        Finally
            Try
                Directory.Delete(pkgDir, True)
            Catch
            End Try
        End Try
    End Sub

    ' ----------------------------------------------------------------

    Private failures As Integer = 0

    Private Sub Assert(condition As Boolean, message As String)
        If Not condition Then
            failures += 1
            Console.WriteLine("  [FAIL] " & message)
        Else
            Console.WriteLine("  [ok]   " & message)
        End If
    End Sub

    Private Sub AssertEquals(expected As Object, actual As Object, message As String)
        If Not Object.Equals(expected, actual) Then
            failures += 1
            Console.WriteLine("  [FAIL] " & message & " (expected: " & expected & ", actual: " & actual & ")")
        Else
            Console.WriteLine("  [ok]   " & message)
        End If
    End Sub

End Module
