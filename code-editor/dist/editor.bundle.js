"use strict";
(() => {
  // src/main.ts
  var CodeEditor;
  ((CodeEditor2) => {
    const Editor = Core.Editor;
    const HighlighterRegistry = Highlighters.HighlighterRegistry;
    const SymbolKind = Features.SymbolKind;
    CodeEditor2.sample_vb = `
' VB.NET sample code
Imports System
Imports System.Collections.Generic

Namespace SampleApp
    Public Class Program

        Private Shared ReadOnly Version As String = "1.0.0"

        Public Shared Function Main(args As String()) As Integer
            Dim numbers As New List(Of Integer)() From {1, 2, 3, 4, 5}
            Dim total As Integer = 0

            For Each n As Integer In numbers
                total += n
            Next

            Console.WriteLine($"Total: {total}")
            Return 0
        End Function

        Public Property Name As String
    End Class
End Namespace
`;
    class App {
      constructor() {
        this.diffVisible = false;
        HighlighterRegistry.registerDefaults();
        const editorContainer = document.getElementById("editor-container");
        this.editor = new Editor(editorContainer, {
          tabSize: 4,
          useSpaces: true,
          fontSize: 14
        });
        this.fileInput = document.getElementById("file-input");
        this.languageSelect = document.getElementById("language-select");
        this.themeSelect = document.getElementById("theme-select");
        this.symbolList = document.getElementById("symbol-list");
        this.diffPanel = document.getElementById("diff-panel");
        this.diffContent = document.getElementById("diff-content");
        this.goToLineDialog = document.getElementById("goto-line-dialog");
        this.goToLineInput = document.getElementById("goto-line-input");
        this.statusLine = document.getElementById("status-line");
        this.statusCol = document.getElementById("status-col");
        this.statusLang = document.getElementById("status-lang");
        this.statusFile = document.getElementById("status-file");
        this.completionEndpointInput = document.getElementById("completion-endpoint");
        this.completionStatus = document.getElementById("completion-status");
        this.populateLanguages();
        this.attachEvents();
        this.loadSampleContent();
        this.updateStatus();
        this.refreshSymbols();
      }
      populateLanguages() {
        const languages = HighlighterRegistry.listLanguages();
        this.languageSelect.innerHTML = "";
        for (const lang of languages) {
          const opt = document.createElement("option");
          opt.value = lang;
          opt.textContent = this.languageDisplayName(lang);
          this.languageSelect.appendChild(opt);
        }
        const plain = document.createElement("option");
        plain.value = "plain";
        plain.textContent = "Plain Text";
        this.languageSelect.appendChild(plain);
      }
      languageDisplayName(lang) {
        switch (lang) {
          case "vbnet":
            return "VisualBasic.NET";
          case "r":
            return "R";
          case "json":
            return "JSON";
          case "xml":
            return "XML";
          case "markdown":
            return "Markdown";
          case "yaml":
            return "YAML";
          default:
            return lang;
        }
      }
      setTheme(theme) {
        this.editor.setTheme(theme);
      }
      setApiEndpoint(url) {
        this.editor.getCompletionProvider().setEndpoint(url);
      }
      toggleTheme() {
        if (this.editor.getTheme() == "light") {
          this.setTheme("dark");
        } else {
          this.setTheme("light");
        }
      }
      toggleMinimap() {
        this.editor.toggleMinimap();
      }
      attachEvents() {
        document.getElementById("btn-open").addEventListener("click", () => {
          this.fileInput.click();
        });
        this.fileInput.addEventListener("change", (e) => {
          const file = e.target.files?.[0];
          if (file) this.loadFile(file);
        });
        document.getElementById("btn-save").addEventListener("click", () => {
          this.exportFile();
        });
        this.languageSelect.addEventListener("change", () => {
          this.editor.setLanguage(this.languageSelect.value);
          this.refreshSymbols();
          this.updateStatus();
        });
        this.themeSelect.addEventListener("change", () => {
          this.editor.setTheme(this.themeSelect.value);
        });
        this.editor.setTheme("light");
        document.getElementById("btn-toggle-symbols").addEventListener("click", () => {
          const panel = document.getElementById("symbol-sidebar");
          panel.classList.toggle("hidden");
          if (!panel.classList.contains("hidden")) {
            this.refreshSymbols();
          }
        });
        const minimapBtn = document.getElementById("btn-toggle-minimap");
        minimapBtn.addEventListener("click", () => {
          this.editor.toggleMinimap();
          minimapBtn.classList.toggle("active", this.editor.isMinimapVisible());
        });
        this.symbolList.addEventListener("click", (e) => {
          const target = e.target;
          const toggle = target.closest(".symbol-toggle-expandable");
          if (toggle) {
            const node = toggle.closest(".symbol-node");
            if (node) {
              node.classList.toggle("collapsed");
              e.stopPropagation();
            }
            return;
          }
          const item = target.closest(".symbol-item");
          if (item) {
            const line = parseInt(item.getAttribute("data-line") || "0", 10);
            const col = parseInt(item.getAttribute("data-col") || "0", 10);
            const kind = item.getAttribute("data-kind") || SymbolKind.Function;
            this.editor.goToSymbol({ name: "", kind, line, column: col });
          }
        });
        document.getElementById("btn-toggle-diff").addEventListener("click", () => {
          this.toggleDiffView();
        });
        document.getElementById("btn-goto-line").addEventListener("click", () => {
          this.openGoToLineDialog();
        });
        this.editor["container"].addEventListener("editor:gotoLine", () => {
          this.openGoToLineDialog();
        });
        this.editor["container"].addEventListener("editor:toggleDiff", () => {
          this.toggleDiffView();
        });
        this.editor["container"].addEventListener("editor:export", () => {
          this.exportFile();
        });
        document.getElementById("goto-line-ok").addEventListener("click", () => {
          this.executeGoToLine();
        });
        document.getElementById("goto-line-cancel").addEventListener("click", () => {
          this.closeGoToLineDialog();
        });
        this.goToLineInput.addEventListener("keydown", (e) => {
          if (e.key === "Enter") {
            e.preventDefault();
            this.executeGoToLine();
          } else if (e.key === "Escape") {
            e.preventDefault();
            this.closeGoToLineDialog();
          }
        });
        this.completionEndpointInput.addEventListener("change", () => {
          this.editor.getCompletionProvider().setEndpoint(this.completionEndpointInput.value);
          this.completionStatus.textContent = "Endpoint updated";
          setTimeout(() => {
            this.completionStatus.textContent = "";
          }, 2e3);
        });
        document.getElementById("btn-test-completion").addEventListener("click", async () => {
          await this.testCompletion();
        });
        this.editor.onChange(() => {
          this.updateStatus();
          this.refreshSymbols();
          if (this.diffVisible) {
            this.renderDiff();
          }
        });
        this.editor.onCursorChange(() => {
          this.updateStatus();
        });
        this.editor.getDiffViewer().setOriginal(this.editor.getText());
      }
      loadSampleContent() {
        this.editor.setText(CodeEditor2.sample_vb, "sample.vb");
        this.languageSelect.value = "vbnet";
        this.editor.setLanguage("vbnet");
        this.refreshSymbols();
        this.updateStatus();
      }
      loadFile(file) {
        const reader = new FileReader();
        reader.onload = () => this.loadFileText(reader.result, file.name);
        reader.readAsText(file);
      }
      loadFileText(text, filename) {
        this.editor.setText(text, filename);
        const lang = this.editor.getLanguage();
        for (let i = 0; i < this.languageSelect.options.length; i++) {
          if (this.languageSelect.options[i].value === lang) {
            this.languageSelect.selectedIndex = i;
            break;
          }
        }
        this.refreshSymbols();
        this.updateStatus();
      }
      exportFile() {
        const text = this.getCodeText();
        const filename = this.editor.getFilename() || "untitled.txt";
        const blob = new Blob([text], { type: "text/plain;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }
      /**
       * export code text to webview2 host
      */
      getCodeText() {
        return this.editor.getText();
      }
      getCodeLanguage() {
        return this.editor.getLanguage();
      }
      refreshSymbols() {
        const tree = this.editor.getSymbolTree();
        if (tree.length === 0) {
          this.symbolList.innerHTML = '<div class="symbol-empty">No symbols found</div>';
          return;
        }
        this.symbolList.innerHTML = this.renderSymbolNodes(tree, 0);
      }
      /**
       * Recursively renders the symbol tree as nested, collapsible rows.
       * `depth` is the visual nesting level (number of ancestors) used for
       * indentation, independent of the structural level used for building.
       */
      renderSymbolNodes(nodes, depth) {
        const parts = [];
        for (const node of nodes) {
          parts.push(this.renderSymbolNode(node, depth));
        }
        return parts.join("");
      }
      renderSymbolNode(node, depth) {
        const sym = node.symbol;
        const icon = this.symbolIcon(sym.kind);
        const hasChildren = node.children.length > 0;
        const toggle = hasChildren ? `<span class="symbol-toggle symbol-toggle-expandable" title="Collapse / expand">&#9662;</span>` : `<span class="symbol-toggle symbol-toggle-leaf"></span>`;
        const childrenHtml = hasChildren ? `<div class="symbol-children">` + this.renderSymbolNodes(node.children, depth + 1) + `</div>` : "";
        const indent = depth * 16 + 10;
        return `<div class="symbol-node"><div class="symbol-item" data-line="${sym.line}" data-col="${sym.column}" data-kind="${sym.kind}" style="padding-left:${indent}px">` + toggle + `<span class="symbol-icon symbol-${sym.kind.toLowerCase()}">${icon}</span><span class="symbol-name">${Utils.escapeHtml(sym.name)}</span><span class="symbol-kind">${sym.kind}</span><span class="symbol-line">:${sym.line + 1}</span></div>` + childrenHtml + `</div>`;
      }
      symbolIcon(kind) {
        switch (kind) {
          case SymbolKind.Function:
            return "\u0192";
          case SymbolKind.Sub:
            return "s";
          case SymbolKind.Property:
            return "p";
          case SymbolKind.Class:
            return "C";
          case SymbolKind.Module:
            return "M";
          case SymbolKind.Structure:
            return "S";
          case SymbolKind.Interface:
            return "I";
          case SymbolKind.Enum:
            return "E";
          case SymbolKind.Namespace:
            return "N";
          case SymbolKind.Variable:
            return "v";
          case SymbolKind.Heading:
            return "H";
          case SymbolKind.Tag:
            return "T";
          case SymbolKind.Key:
            return "K";
          case SymbolKind.Field:
            return "F";
          default:
            return "\xB7";
        }
      }
      toggleDiffView() {
        this.diffVisible = !this.diffVisible;
        if (this.diffVisible) {
          this.diffPanel.classList.remove("hidden");
          this.renderDiff();
        } else {
          this.diffPanel.classList.add("hidden");
        }
      }
      renderDiff() {
        this.editor.getDiffViewer().setCurrent(this.editor.getText());
        const summary = this.editor.getDiffViewer().getSummary();
        const header = `<div class="diff-header">Changes: <span class="diff-added-count">+${summary.added}</span> <span class="diff-removed-count">-${summary.removed}</span></div>`;
        const body = this.editor.getDiffViewer().renderDiffHtml();
        this.diffContent.innerHTML = header + body;
      }
      openGoToLineDialog() {
        this.editor.getGoToLine().setMaxLine(this.editor.getBuffer().lineCount);
        this.goToLineDialog.classList.remove("hidden");
        this.goToLineInput.value = "";
        this.goToLineInput.focus();
      }
      closeGoToLineDialog() {
        this.goToLineDialog.classList.add("hidden");
        this.editor.focus();
      }
      executeGoToLine() {
        const input = this.goToLineInput.value.trim();
        const line = this.editor.getGoToLine().validate(input);
        if (line < 0) {
          this.goToLineInput.classList.add("error");
          setTimeout(() => this.goToLineInput.classList.remove("error"), 500);
          return;
        }
        this.editor.goToLineNumber(line);
        this.closeGoToLineDialog();
      }
      async testCompletion() {
        this.completionStatus.textContent = "Testing endpoint...";
        try {
          const provider = this.editor.getCompletionProvider();
          const items = await provider.requestCompletions({
            language: this.editor.getLanguage(),
            text: this.editor.getText(),
            line: 0,
            column: 0
          });
          this.completionStatus.textContent = `OK: ${items.length} items returned`;
        } catch (e) {
          this.completionStatus.textContent = `Error: ${e.message}`;
        }
        setTimeout(() => {
          this.completionStatus.textContent = "";
        }, 4e3);
      }
      updateStatus() {
        const cursor = this.editor.getCursor().position;
        this.statusLine.textContent = String(cursor.line + 1);
        this.statusCol.textContent = String(cursor.column + 1);
        this.statusLang.textContent = this.editor.getLanguage();
        this.statusFile.textContent = this.editor.getFilename();
        if (devkit) {
          devkit.updateStatus(
            this.statusLine.textContent,
            this.statusCol.textContent,
            this.statusLang.textContent,
            this.statusFile.textContent
          );
        }
      }
    }
    CodeEditor2.App = App;
    function bootstrap() {
      return new App();
    }
    CodeEditor2.bootstrap = bootstrap;
  })(CodeEditor || (CodeEditor = {}));
  window.addEventListener("DOMContentLoaded", () => {
    const codeEditor = CodeEditor.bootstrap();
    window.codeEditor = codeEditor;
    window.chrome.webview.addEventListener("message", function(event) {
      const message = event.data;
      if (message.type === "loadFile") {
        codeEditor.loadFileText(JSON.parse(message.text), message.filename);
      }
    });
  });
})();
