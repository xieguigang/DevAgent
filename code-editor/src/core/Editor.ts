namespace CodeEditor.Core {
    import TokenType = Utils.TokenType;
    import Token = Utils.Token;
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import FoldRange = Features.FoldRange;
    import CodeFolder = Features.CodeFolder;
    import Symbol = Features.Symbol;
    import SymbolNavigator = Features.SymbolNavigator;
    import SymbolKind = Features.SymbolKind;
    import DiffViewer = Features.DiffViewer;
    import CompletionProvider = Features.CompletionProvider;
    import CompletionItem = Features.CompletionItem;
    import GoToLine = Features.GoToLine;

    export interface EditorOptions {
        tabSize?: number;
        useSpaces?: boolean;
        fontSize?: number;
        fontFamily?: string;
        lineNumbers?: boolean;
        wordWrap?: boolean;
    }

    /**
     * The main editor controller. Owns the text buffer, cursor, highlighter,
     * and renders into a set of layered DOM elements:
     *   - gutter (line numbers + fold markers)
     *   - code view (highlighted lines)
     *   - caret overlay
     *   - selection overlay
     *   - completion popup
     */
    export class Editor {
        private container: HTMLElement;
        private gutter!: HTMLElement;
        private codeView!: HTMLElement;
        private textarea!: HTMLTextAreaElement;
        private scrollContainer!: HTMLElement;
        private completionPopup!: HTMLElement;

        private buffer: TextBuffer = new TextBuffer();
        private cursor: Cursor = new Cursor();
        private highlighter: Highlighter = new Highlighter();
        private folder: CodeFolder = new CodeFolder();
        private symbolNav: SymbolNavigator = new SymbolNavigator();
        private diffViewer: DiffViewer = new DiffViewer();
        private completionProvider: CompletionProvider = new CompletionProvider();
        private goToLine: GoToLine = new GoToLine();

        private options: Required<EditorOptions> = {
            tabSize: 4,
            useSpaces: true,
            fontSize: 14,
            fontFamily: "'Cascadia Code', 'Consolas', 'Courier New', monospace",
            lineNumbers: true,
            wordWrap: false
        };

        private language: string = "plain";
        private currentHighlighter: ILanguageHighlighter | null = null;
        private foldRanges: FoldRange[] = [];
        private collapsedLines: Set<number> = new Set();
        private symbols: Symbol[] = [];
        private filename: string = "untitled";

        private charWidth: number = 8;
        private lineHeight: number = 20;
        private firstVisibleLine: number = 0;
        private visibleLineCount: number = 40;

        private completionItems: CompletionItem[] = [];
        private completionActive: boolean = false;
        private completionIndex: number = 0;
        private completionAnchor: { line: number; column: number } = { line: 0, column: 0 };

        private onChangeCallbacks: Array<() => void> = [];
        private onCursorChangeCallbacks: Array<() => void> = [];

        constructor(container: HTMLElement, options?: EditorOptions) {
            this.container = container;
            if (options) {
                this.options = { ...this.options, ...options };
            }
            this.buildDom();
            this.attachEvents();
            this.highlighter.setBuffer(this.buffer);
            this.buffer.onChange.on(() => {
                this.highlighter.invalidate(0);
                this.recomputeFolds();
                this.recomputeSymbols();
                this.diffViewer.setCurrent(this.buffer.getText());
                this.render();
                this.fireChange();
            });
            this.recomputeFolds();
            this.recomputeSymbols();
            this.render();
        }

        private buildDom(): void {
            this.container.classList.add("editor-root");
            this.container.innerHTML = "";

            this.scrollContainer = document.createElement("div");
            this.scrollContainer.className = "editor-scroll";
            this.scrollContainer.tabIndex = 0;

            this.gutter = document.createElement("div");
            this.gutter.className = "editor-gutter";

            this.codeView = document.createElement("div");
            this.codeView.className = "editor-codeview";

            this.textarea = document.createElement("textarea");
            this.textarea.className = "editor-input";
            this.textarea.spellcheck = false;
            this.textarea.setAttribute("autocapitalize", "off");
            this.textarea.setAttribute("autocorrect", "off");

            this.completionPopup = document.createElement("div");
            this.completionPopup.className = "completion-popup";
            this.completionPopup.style.display = "none";

            this.scrollContainer.appendChild(this.gutter);
            this.scrollContainer.appendChild(this.codeView);
            this.scrollContainer.appendChild(this.textarea);
            this.scrollContainer.appendChild(this.completionPopup);
            this.container.appendChild(this.scrollContainer);

            // Measure char width.
            this.measureCharWidth();
        }

        private measureCharWidth(): void {
            const measure = document.createElement("span");
            measure.className = "editor-measure";
            measure.textContent = "M".repeat(100);
            this.codeView.appendChild(measure);
            const rect = measure.getBoundingClientRect();
            this.charWidth = rect.width / 100;
            this.lineHeight = rect.height || 20;
            this.codeView.removeChild(measure);
        }

        private attachEvents(): void {
            this.scrollContainer.addEventListener("scroll", () => {
                this.firstVisibleLine = Math.floor(this.scrollContainer.scrollTop / this.lineHeight);
                this.render();
            });

            this.textarea.addEventListener("input", (e) => {
                this.handleInput();
            });

            this.textarea.addEventListener("keydown", (e) => {
                this.handleKeyDown(e);
            });

            this.textarea.addEventListener("click", () => {
                this.updateCaretFromTextarea();
            });

            this.textarea.addEventListener("keyup", () => {
                this.updateCaretFromTextarea();
            });

            this.textarea.addEventListener("blur", () => {
                this.hideCompletion();
            });

            this.codeView.addEventListener("click", (e) => {
                this.handleCodeViewClick(e);
            });

            this.gutter.addEventListener("click", (e) => {
                this.handleGutterClick(e);
            });

            window.addEventListener("resize", () => {
                this.render();
            });
        }

        private handleInput(): void {
            const value = this.textarea.value;
            const pos = this.textarea.selectionStart;
            const before = value.substring(0, pos);
            const after = value.substring(pos);

            // Convert textarea flat text to buffer operations.
            // Strategy: replace entire buffer with textarea content, preserving caret.
            const oldCaret = this.textareaToBufferPos(pos);

            // Actually, simpler: just set buffer text and update cursor.
            this.buffer.setText(value);
            this.cursor.setPosition(this.textareaToBufferPos(pos));
            this.render();
            this.fireCursorChange();

            // Trigger completion if applicable.
            this.maybeTriggerCompletion();
        }

        private textareaToBufferPos(pos: number): { line: number; column: number } {
            const text = this.textarea.value;
            let line = 0;
            let col = 0;
            for (let i = 0; i < pos && i < text.length; i++) {
                if (text[i] === "\n") {
                    line++;
                    col = 0;
                } else {
                    col++;
                }
            }
            return { line, column: col };
        }

        private bufferToTextareaPos(line: number, column: number): number {
            const text = this.textarea.value;
            let l = 0;
            let pos = 0;
            while (l < line && pos < text.length) {
                if (text[pos] === "\n") l++;
                pos++;
            }
            return pos + column;
        }

        private updateCaretFromTextarea(): void {
            const pos = this.textarea.selectionStart;
            this.cursor.setPosition(this.textareaToBufferPos(pos));
            this.render();
            this.fireCursorChange();
        }

        private handleKeyDown(e: KeyboardEvent): void {
            // Handle completion navigation first.
            if (this.completionActive) {
                if (e.key === "ArrowDown") {
                    e.preventDefault();
                    this.completionIndex = (this.completionIndex + 1) % this.completionItems.length;
                    this.renderCompletion();
                    return;
                }
                if (e.key === "ArrowUp") {
                    e.preventDefault();
                    this.completionIndex = (this.completionIndex - 1 + this.completionItems.length) % this.completionItems.length;
                    this.renderCompletion();
                    return;
                }
                if (e.key === "Enter" || e.key === "Tab") {
                    e.preventDefault();
                    this.acceptCompletion();
                    return;
                }
                if (e.key === "Escape") {
                    e.preventDefault();
                    this.hideCompletion();
                    return;
                }
            }

            if (e.key === "Tab") {
                e.preventDefault();
                const insertStr = this.options.useSpaces
                    ? " ".repeat(this.options.tabSize)
                    : "\t";
                const start = this.textarea.selectionStart;
                const end = this.textarea.selectionEnd;
                this.textarea.value = this.textarea.value.substring(0, start) + insertStr + this.textarea.value.substring(end);
                this.textarea.selectionStart = this.textarea.selectionEnd = start + insertStr.length;
                this.handleInput();
                return;
            }

            // Ctrl+Space: trigger completion manually.
            if (e.ctrlKey && e.key === " ") {
                e.preventDefault();
                this.triggerCompletion();
                return;
            }

            // Ctrl+G: go to line.
            if (e.ctrlKey && e.key === "g") {
                e.preventDefault();
                this.openGoToLineDialog();
                return;
            }

            // Ctrl+S: export (prevent browser save).
            if (e.ctrlKey && e.key === "s") {
                e.preventDefault();
                this.exportFile();
                return;
            }

            // Ctrl+Shift+D: toggle diff view.
            if (e.ctrlKey && e.shiftKey && (e.key === "D" || e.key === "d")) {
                e.preventDefault();
                this.toggleDiffView();
                return;
            }
        }

        private handleCodeViewClick(e: MouseEvent): void {
            const rect = this.codeView.getBoundingClientRect();
            const x = e.clientX - rect.left + this.codeView.scrollLeft;
            const y = e.clientY - rect.top + this.codeView.scrollTop;
            const line = Math.floor(y / this.lineHeight);
            const column = Math.floor(x / this.charWidth);
            if (line >= 0 && line < this.buffer.lineCount) {
                const lineText = this.buffer.getLine(line);
                const clampedCol = Math.min(column, lineText.length);
                const pos = this.bufferToTextareaPos(line, clampedCol);
                this.textarea.focus();
                this.textarea.selectionStart = this.textarea.selectionEnd = pos;
                this.cursor.setPosition({ line, column: clampedCol });
                this.render();
                this.fireCursorChange();
            }
        }

        private handleGutterClick(e: MouseEvent): void {
            const target = e.target as HTMLElement;
            if (target.classList.contains("fold-marker")) {
                const line = parseInt(target.getAttribute("data-line") || "0", 10);
                this.toggleFold(line);
                return;
            }
            // Click on line number selects whole line.
            if (target.classList.contains("line-number")) {
                const line = parseInt(target.getAttribute("data-line") || "0", 10);
                const lineLen = this.buffer.getLine(line).length;
                this.cursor.setSelection({ line, column: 0 }, { line, column: lineLen });
                const startPos = this.bufferToTextareaPos(line, 0);
                const endPos = this.bufferToTextareaPos(line, lineLen);
                this.textarea.focus();
                this.textarea.selectionStart = startPos;
                this.textarea.selectionEnd = endPos;
                this.render();
                this.fireCursorChange();
            }
        }

        // ---- Public API ----

        getText(): string {
            return this.buffer.getText();
        }

        setText(text: string, filename?: string): void {
            this.buffer.setText(text);
            if (filename) {
                this.setFilename(filename);
            }
            this.diffViewer.setOriginal(text);
            this.diffViewer.setCurrent(text);
            this.collapsedLines.clear();
            this.cursor.setPosition({ line: 0, column: 0 });
            this.textarea.value = text;
            this.textarea.selectionStart = this.textarea.selectionEnd = 0;
            this.render();
            this.fireCursorChange();
        }

        setFilename(filename: string): void {
            this.filename = filename;
            const h = Highlighters.HighlighterRegistry.detectFromFilename(filename);
            if (h) {
                this.setLanguage(h.language);
            }
        }

        getFilename(): string {
            return this.filename;
        }

        setLanguage(language: string): void {
            this.language = language;
            this.currentHighlighter = Highlighters.HighlighterRegistry.get(language);
            this.highlighter.setHighlighter(this.currentHighlighter);
            this.highlighter.invalidateAll();
            this.recomputeFolds();
            this.recomputeSymbols();
            this.render();
        }

        getLanguage(): string {
            return this.language;
        }

        setTheme(theme: "light" | "dark"): void {
            document.body.setAttribute("data-theme", theme);
        }

        getTheme(): string | null {
            return document.body.getAttribute("data-theme");
        }

        onChange(cb: () => void): void {
            this.onChangeCallbacks.push(cb);
        }

        onCursorChange(cb: () => void): void {
            this.onCursorChangeCallbacks.push(cb);
        }

        private fireChange(): void {
            for (const cb of this.onChangeCallbacks) cb();
        }

        private fireCursorChange(): void {
            for (const cb of this.onCursorChangeCallbacks) cb();
        }

        // ---- Folding ----

        private recomputeFolds(): void {
            this.foldRanges = this.folder.computeFoldRanges(this.buffer.getLines(), this.language);
        }

        toggleFold(line: number): void {
            if (this.collapsedLines.has(line)) {
                this.collapsedLines.delete(line);
            } else {
                this.collapsedLines.add(line);
            }
            this.render();
        }

        isLineCollapsed(line: number): boolean {
            return this.collapsedLines.has(line);
        }

        private isLineHiddenByFold(line: number): boolean {
            for (const startLine of this.collapsedLines) {
                const range = this.foldRanges.find(r => r.startLine === startLine);
                if (range && line > range.startLine && line <= range.endLine) {
                    return true;
                }
            }
            return false;
        }

        // ---- Symbols ----

        private recomputeSymbols(): void {
            this.symbols = this.symbolNav.extractSymbols(this.buffer.getLines(), this.language);
        }

        getSymbols(): Symbol[] {
            return this.symbols;
        }

        goToSymbol(symbol: Symbol): void {
            this.cursor.setPosition({ line: symbol.line, column: symbol.column });
            const pos = this.bufferToTextareaPos(symbol.line, symbol.column);
            this.textarea.focus();
            this.textarea.selectionStart = this.textarea.selectionEnd = pos;
            this.scrollToLine(symbol.line);
            this.render();
            this.fireCursorChange();
        }

        // ---- Diff ----

        getDiffViewer(): DiffViewer {
            return this.diffViewer;
        }

        toggleDiffView(): void {
            const event = new CustomEvent("editor:toggleDiff");
            this.container.dispatchEvent(event);
        }

        // ---- Completion ----

        getCompletionProvider(): CompletionProvider {
            return this.completionProvider;
        }

        private maybeTriggerCompletion(): void {
            const pos = this.cursor.position;
            const lineText = this.buffer.getLine(pos.line);
            const before = lineText.substring(0, pos.column);
            // Trigger when user types a letter or dot after an identifier.
            const triggerMatch = /[A-Za-z_][A-Za-z0-9_]*\.?$/;
            if (triggerMatch.test(before) && before.length >= 2) {
                this.triggerCompletion();
            } else {
                this.hideCompletion();
            }
        }

        private async triggerCompletion(): Promise<void> {
            const pos = this.cursor.position;
            this.completionAnchor = { line: pos.line, column: pos.column };
            const lineText = this.buffer.getLine(pos.line);
            const before = lineText.substring(0, pos.column);
            // Find word boundary.
            const wordMatch = /[A-Za-z_][A-Za-z0-9_]*$/.exec(before);
            const wordStart = wordMatch ? pos.column - wordMatch[0].length : pos.column;

            this.completionActive = true;
            this.completionItems = await this.completionProvider.requestCompletions({
                language: this.language,
                text: this.buffer.getText(),
                line: pos.line,
                column: pos.column
            });

            // Filter by current word.
            const currentWord = wordMatch ? wordMatch[0] : "";
            if (currentWord) {
                this.completionItems = this.completionItems.filter(item =>
                    item.label.toLowerCase().startsWith(currentWord.toLowerCase())
                );
            }

            this.completionIndex = 0;
            if (this.completionItems.length > 0) {
                this.renderCompletion();
            } else {
                this.hideCompletion();
            }
        }

        private renderCompletion(): void {
            if (!this.completionActive || this.completionItems.length === 0) {
                this.hideCompletion();
                return;
            }
            const pos = this.cursor.position;
            // The popup is a child of the scrolling container, so it scrolls
            // with the content. Use absolute line/column coordinates (not
            // viewport-relative) so it stays aligned after the user scrolls.
            const top = pos.line * this.lineHeight;
            const left = pos.column * this.charWidth;
            this.completionPopup.style.display = "block";
            this.completionPopup.style.top = top + "px";
            this.completionPopup.style.left = left + "px";

            const items = this.completionItems.slice(0, 12);
            const html = items.map((item, idx) => {
                const cls = idx === this.completionIndex ? "completion-item selected" : "completion-item";
                const kindCls = "completion-kind-" + (item.kind || "text");
                return `<div class="${cls}" data-idx="${idx}">` +
                    `<span class="completion-kind ${kindCls}">${this.kindIcon(item.kind)}</span>` +
                    `<span class="completion-label">${Utils.escapeHtml(item.label)}</span>` +
                    (item.detail ? `<span class="completion-detail">${Utils.escapeHtml(item.detail)}</span>` : "") +
                    `</div>`;
            }).join("");
            this.completionPopup.innerHTML = html;

            // Attach click handlers.
            const items2 = this.completionPopup.querySelectorAll(".completion-item");
            items2.forEach((el, idx) => {
                el.addEventListener("mousedown", (e) => {
                    e.preventDefault();
                    this.completionIndex = idx;
                    this.acceptCompletion();
                });
            });
        }

        private kindIcon(kind?: string): string {
            switch (kind) {
                case "function": return "f";
                case "variable": return "v";
                case "constant": return "c";
                case "keyword": return "k";
                case "snippet": return "s";
                case "class": return "C";
                case "module": return "M";
                case "property": return "p";
                default: return "·";
            }
        }

        private hideCompletion(): void {
            this.completionActive = false;
            this.completionPopup.style.display = "none";
        }

        private acceptCompletion(): void {
            if (!this.completionActive || this.completionItems.length === 0) return;
            const item = this.completionItems[this.completionIndex];
            const pos = this.cursor.position;
            const lineText = this.buffer.getLine(pos.line);
            const before = lineText.substring(0, pos.column);
            const wordMatch = /[A-Za-z_][A-Za-z0-9_]*$/.exec(before);
            const wordStart = wordMatch ? pos.column - wordMatch[0].length : pos.column;
            const insertText = item.insertText || item.label;
            // Replace word with insertText.
            const newLine = lineText.substring(0, wordStart) + insertText + lineText.substring(pos.column);
            const lines = this.buffer.getLines();
            lines[pos.line] = newLine;
            this.buffer.setText(lines.join("\n"));
            const newCol = wordStart + insertText.length;
            this.cursor.setPosition({ line: pos.line, column: newCol });
            const taPos = this.bufferToTextareaPos(pos.line, newCol);
            this.textarea.value = this.buffer.getText();
            this.textarea.selectionStart = this.textarea.selectionEnd = taPos;
            this.hideCompletion();
            this.render();
            this.fireCursorChange();
        }

        // ---- Go to line ----

        getGoToLine(): GoToLine {
            return this.goToLine;
        }

        openGoToLineDialog(): void {
            const event = new CustomEvent("editor:gotoLine");
            this.container.dispatchEvent(event);
        }

        goToLineNumber(line: number): void {
            const zeroBased = Math.max(0, Math.min(line - 1, this.buffer.lineCount - 1));
            this.cursor.setPosition({ line: zeroBased, column: 0 });
            const pos = this.bufferToTextareaPos(zeroBased, 0);
            this.textarea.focus();
            this.textarea.selectionStart = this.textarea.selectionEnd = pos;
            this.scrollToLine(zeroBased);
            this.render();
            this.fireCursorChange();
        }

        private scrollToLine(line: number): void {
            const targetTop = line * this.lineHeight;
            const viewTop = this.scrollContainer.scrollTop;
            const viewHeight = this.scrollContainer.clientHeight;
            if (targetTop < viewTop) {
                this.scrollContainer.scrollTop = targetTop;
            } else if (targetTop > viewTop + viewHeight - this.lineHeight * 2) {
                this.scrollContainer.scrollTop = targetTop - viewHeight + this.lineHeight * 2;
            }
        }

        // ---- Export ----

        exportFile(): void {
            const event = new CustomEvent("editor:export");
            this.container.dispatchEvent(event);
        }

        // ---- Rendering ----

        render(): void {
            this.renderGutter();
            this.renderCodeView();
            this.renderCaret();
        }

        private renderGutter(): void {
            const lineCount = this.buffer.lineCount;
            const parts: string[] = [];
            const maxNumWidth = String(lineCount).length;

            for (let i = 0; i < lineCount; i++) {
                if (this.isLineHiddenByFold(i)) continue;
                const num = i + 1;
                const foldRange = this.foldRanges.find(r => r.startLine === i);
                const isCollapsed = this.collapsedLines.has(i);
                let foldMarker = "";
                if (foldRange) {
                    foldMarker = `<span class="fold-marker ${isCollapsed ? "collapsed" : "expanded"}" data-line="${i}">${isCollapsed ? "+" : "−"}</span>`;
                } else {
                    foldMarker = `<span class="fold-spacer"></span>`;
                }
                const numStr = String(num).padStart(maxNumWidth, " ");
                parts.push(
                    `<div class="gutter-line">` +
                    `<span class="line-number" data-line="${i}">${numStr}</span>` +
                    foldMarker +
                    `</div>`
                );
            }
            this.gutter.innerHTML = parts.join("");
        }

        private renderCodeView(): void {
            const lineCount = this.buffer.lineCount;
            const parts: string[] = [];
            const cursorLine = this.cursor.position.line;

            for (let i = 0; i < lineCount; i++) {
                if (this.isLineHiddenByFold(i)) continue;
                const lineText = this.buffer.getLine(i);
                const isCursorLine = i === cursorLine;
                const lineHtml = this.renderLine(i, lineText);
                parts.push(
                    `<div class="code-line${isCursorLine ? " cursor-line" : ""}" data-line="${i}">${lineHtml}</div>`
                );
            }
            this.codeView.innerHTML = parts.join("");
        }

        private renderLine(line: number, text: string): string {
            const tokens = this.highlighter.getTokens(line, text);
            const parts: string[] = [];
            for (const t of tokens) {
                const cls = this.tokenClass(t.type);
                const escaped = Utils.escapeHtml(t.value);
                if (cls) {
                    parts.push(`<span class="${cls}">${escaped}</span>`);
                } else {
                    parts.push(escaped);
                }
            }
            // Ensure line has some height even when empty.
            if (parts.length === 0) {
                parts.push("&nbsp;");
            }
            return parts.join("");
        }

        private tokenClass(type: TokenType): string {
            switch (type) {
                case TokenType.Keyword: return "tok-keyword";
                case TokenType.ControlKeyword: return "tok-control";
                case TokenType.Identifier: return "tok-identifier";
                case TokenType.Type: return "tok-type";
                case TokenType.String: return "tok-string";
                case TokenType.Number: return "tok-number";
                case TokenType.Comment: return "tok-comment";
                case TokenType.DocComment: return "tok-doccomment";
                case TokenType.Operator: return "tok-operator";
                case TokenType.Punctuation: return "tok-punctuation";
                case TokenType.Preprocessor: return "tok-preprocessor";
                case TokenType.Attribute: return "tok-attribute";
                case TokenType.Tag: return "tok-tag";
                case TokenType.AttrName: return "tok-attrname";
                case TokenType.AttrValue: return "tok-attrvalue";
                case TokenType.XmlDelimiter: return "tok-xmldelimiter";
                case TokenType.XmlText: return "tok-xmltext";
                case TokenType.Heading: return "tok-heading";
                case TokenType.Bold: return "tok-bold";
                case TokenType.Italic: return "tok-italic";
                case TokenType.Code: return "tok-code";
                case TokenType.Link: return "tok-link";
                case TokenType.ListMarker: return "tok-listmarker";
                case TokenType.Quote: return "tok-quote";
                case TokenType.Property: return "tok-property";
                case TokenType.Function: return "tok-function";
                case TokenType.Constant: return "tok-constant";
                case TokenType.Annotation: return "tok-annotation";
                case TokenType.Error: return "tok-error";
                default: return "";
            }
        }

        private renderCaret(): void {
            // The caret is rendered by the textarea itself (we keep it focused).
            // Position the textarea so its caret aligns with the code view.
            const pos = this.cursor.position;
            const top = pos.line * this.lineHeight;
            const left = pos.column * this.charWidth;
            this.textarea.style.transform = `translate(${left}px, ${top}px)`;
        }

        // ---- Focus ----

        focus(): void {
            this.textarea.focus();
        }

        getCursor(): Cursor {
            return this.cursor;
        }

        getBuffer(): TextBuffer {
            return this.buffer;
        }
    }
}
