namespace CodeEditor {

    import Editor = Core.Editor;
    import HighlighterRegistry = Highlighters.HighlighterRegistry;
    import SymbolKind = Features.SymbolKind;

    export const sample_vb = `
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

    /**
     * Application entry point. Wires up the editor, toolbar, file load/export,
     * symbol navigator panel, diff view panel, go-to-line dialog, and theme
     * switching.
     */
    export class App {

        private editor: Editor;
        private fileInput: HTMLInputElement;
        private languageSelect: HTMLSelectElement;
        private themeSelect: HTMLSelectElement;
        private symbolList: HTMLElement;
        private diffPanel: HTMLElement;
        private diffContent: HTMLElement;
        private goToLineDialog: HTMLElement;
        private goToLineInput: HTMLInputElement;
        private statusLine: HTMLElement;
        private statusCol: HTMLElement;
        private statusLang: HTMLElement;
        private statusFile: HTMLElement;
        private completionEndpointInput: HTMLInputElement;
        private completionStatus: HTMLElement;
        private diffVisible: boolean = false;

        constructor() {
            HighlighterRegistry.registerDefaults();

            const editorContainer = document.getElementById("editor-container")!;
            this.editor = new Editor(editorContainer, {
                tabSize: 4,
                useSpaces: true,
                fontSize: 14
            });

            this.fileInput = document.getElementById("file-input") as HTMLInputElement;
            this.languageSelect = document.getElementById("language-select") as HTMLSelectElement;
            this.themeSelect = document.getElementById("theme-select") as HTMLSelectElement;
            this.symbolList = document.getElementById("symbol-list")!;
            this.diffPanel = document.getElementById("diff-panel")!;
            this.diffContent = document.getElementById("diff-content")!;
            this.goToLineDialog = document.getElementById("goto-line-dialog")!;
            this.goToLineInput = document.getElementById("goto-line-input") as HTMLInputElement;
            this.statusLine = document.getElementById("status-line")!;
            this.statusCol = document.getElementById("status-col")!;
            this.statusLang = document.getElementById("status-lang")!;
            this.statusFile = document.getElementById("status-file")!;
            this.completionEndpointInput = document.getElementById("completion-endpoint") as HTMLInputElement;
            this.completionStatus = document.getElementById("completion-status")!;

            this.populateLanguages();
            this.attachEvents();
            this.loadSampleContent();
            this.updateStatus();
            this.refreshSymbols();
        }

        private populateLanguages(): void {
            const languages = HighlighterRegistry.listLanguages();
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

        private languageDisplayName(lang: string): string {
            switch (lang) {
                case "vbnet": return "VisualBasic.NET";
                case "r": return "R";
                case "json": return "JSON";
                case "xml": return "XML";
                case "markdown": return "Markdown";
                case "yaml": return "YAML";
                default: return lang;
            }
        }

        private attachEvents(): void {
            // File load.
            document.getElementById("btn-open")!.addEventListener("click", () => {
                this.fileInput.click();
            });
            this.fileInput.addEventListener("change", (e) => {
                const file = (e.target as HTMLInputElement).files?.[0];
                if (file) this.loadFile(file);
            });

            // Export (save button).
            document.getElementById("btn-save")!.addEventListener("click", () => {
                this.exportFile();
            });

            // Language select.
            this.languageSelect.addEventListener("change", () => {
                this.editor.setLanguage(this.languageSelect.value);
                this.refreshSymbols();
                this.updateStatus();
            });

            // Theme select.
            this.themeSelect.addEventListener("change", () => {
                this.editor.setTheme(this.themeSelect.value as "light" | "dark");
            });
            this.editor.setTheme("light");

            // Toggle symbols panel.
            document.getElementById("btn-toggle-symbols")!.addEventListener("click", () => {
                const panel = document.getElementById("symbol-sidebar")!;
                panel.classList.toggle("hidden");
                if (!panel.classList.contains("hidden")) {
                    this.refreshSymbols();
                }
            });

            // Toggle diff view.
            document.getElementById("btn-toggle-diff")!.addEventListener("click", () => {
                this.toggleDiffView();
            });

            // Go to line.
            document.getElementById("btn-goto-line")!.addEventListener("click", () => {
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

            // Go to line dialog buttons.
            document.getElementById("goto-line-ok")!.addEventListener("click", () => {
                this.executeGoToLine();
            });
            document.getElementById("goto-line-cancel")!.addEventListener("click", () => {
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

            // Completion endpoint.
            this.completionEndpointInput.addEventListener("change", () => {
                this.editor.getCompletionProvider().setEndpoint(this.completionEndpointInput.value);
                this.completionStatus.textContent = "Endpoint updated";
                setTimeout(() => { this.completionStatus.textContent = ""; }, 2000);
            });

            // Test completion button.
            document.getElementById("btn-test-completion")!.addEventListener("click", async () => {
                await this.testCompletion();
            });

            // Editor events.
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

            // Set original text for diff.
            this.editor.getDiffViewer().setOriginal(this.editor.getText());
        }

        private loadSampleContent(): void {
            this.editor.setText(sample_vb, "sample.vb");
            this.languageSelect.value = "vbnet";
            this.editor.setLanguage("vbnet");
            this.refreshSymbols();
            this.updateStatus();
        }

        private loadFile(file: File): void {
            const reader = new FileReader();

            reader.onload = () => this.loadFileText(reader.result as string, file.name);
            reader.readAsText(file);
        }

        public loadFileText(text: string, filename: string) {
            this.editor.setText(text, filename);
            // Update language select to match.
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

        private exportFile(): void {
            const text = this.editor.getText();
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

        private refreshSymbols(): void {
            const symbols = this.editor.getSymbols();
            if (symbols.length === 0) {
                this.symbolList.innerHTML = '<div class="symbol-empty">No symbols found</div>';
                return;
            }
            const parts: string[] = [];
            for (const sym of symbols) {
                const icon = this.symbolIcon(sym.kind);
                parts.push(
                    `<div class="symbol-item" data-line="${sym.line}" data-col="${sym.column}">` +
                    `<span class="symbol-icon symbol-${sym.kind.toLowerCase()}">${icon}</span>` +
                    `<span class="symbol-name">${Utils.escapeHtml(sym.name)}</span>` +
                    `<span class="symbol-kind">${sym.kind}</span>` +
                    `<span class="symbol-line">:${sym.line + 1}</span>` +
                    `</div>`
                );
            }
            this.symbolList.innerHTML = parts.join("");

            // Attach click handlers.
            const items = this.symbolList.querySelectorAll(".symbol-item");
            items.forEach(el => {
                el.addEventListener("click", () => {
                    const line = parseInt(el.getAttribute("data-line") || "0", 10);
                    const col = parseInt(el.getAttribute("data-col") || "0", 10);
                    this.editor.goToSymbol({ name: "", kind: SymbolKind.Function, line, column: col });
                });
            });
        }

        private symbolIcon(kind: SymbolKind): string {
            switch (kind) {
                case SymbolKind.Function: return "ƒ";
                case SymbolKind.Sub: return "s";
                case SymbolKind.Property: return "p";
                case SymbolKind.Class: return "C";
                case SymbolKind.Module: return "M";
                case SymbolKind.Structure: return "S";
                case SymbolKind.Interface: return "I";
                case SymbolKind.Enum: return "E";
                case SymbolKind.Namespace: return "N";
                case SymbolKind.Variable: return "v";
                case SymbolKind.Heading: return "H";
                case SymbolKind.Tag: return "T";
                case SymbolKind.Key: return "K";
                case SymbolKind.Field: return "F";
                default: return "·";
            }
        }

        private toggleDiffView(): void {
            this.diffVisible = !this.diffVisible;
            if (this.diffVisible) {
                this.diffPanel.classList.remove("hidden");
                this.renderDiff();
            } else {
                this.diffPanel.classList.add("hidden");
            }
        }

        private renderDiff(): void {
            this.editor.getDiffViewer().setCurrent(this.editor.getText());
            const summary = this.editor.getDiffViewer().getSummary();
            const header = `<div class="diff-header">Changes: <span class="diff-added-count">+${summary.added}</span> <span class="diff-removed-count">-${summary.removed}</span></div>`;
            const body = this.editor.getDiffViewer().renderDiffHtml();
            this.diffContent.innerHTML = header + body;
        }

        private openGoToLineDialog(): void {
            this.editor.getGoToLine().setMaxLine(this.editor.getBuffer().lineCount);
            this.goToLineDialog.classList.remove("hidden");
            this.goToLineInput.value = "";
            this.goToLineInput.focus();
        }

        private closeGoToLineDialog(): void {
            this.goToLineDialog.classList.add("hidden");
            this.editor.focus();
        }

        private executeGoToLine(): void {
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

        private async testCompletion(): Promise<void> {
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
                this.completionStatus.textContent = `Error: ${(e as Error).message}`;
            }
            setTimeout(() => { this.completionStatus.textContent = ""; }, 4000);
        }

        private updateStatus(): void {
            const cursor = this.editor.getCursor().position;
            this.statusLine.textContent = String(cursor.line + 1);
            this.statusCol.textContent = String(cursor.column + 1);
            this.statusLang.textContent = this.editor.getLanguage();
            this.statusFile.textContent = this.editor.getFilename();
        }
    }

    // Bootstrap.
    export function bootstrap(): void {
        new App();
    }
}

window.addEventListener("DOMContentLoaded", () => {
    CodeEditor.bootstrap();
});
