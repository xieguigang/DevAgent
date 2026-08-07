"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Common token types used across all highlighters.
         */
        let TokenType;
        (function (TokenType) {
            TokenType[TokenType["Plain"] = 0] = "Plain";
            TokenType[TokenType["Keyword"] = 1] = "Keyword";
            TokenType[TokenType["ControlKeyword"] = 2] = "ControlKeyword";
            TokenType[TokenType["Identifier"] = 3] = "Identifier";
            TokenType[TokenType["Type"] = 4] = "Type";
            TokenType[TokenType["String"] = 5] = "String";
            TokenType[TokenType["Number"] = 6] = "Number";
            TokenType[TokenType["Comment"] = 7] = "Comment";
            TokenType[TokenType["Operator"] = 8] = "Operator";
            TokenType[TokenType["Punctuation"] = 9] = "Punctuation";
            TokenType[TokenType["Preprocessor"] = 10] = "Preprocessor";
            TokenType[TokenType["Attribute"] = 11] = "Attribute";
            TokenType[TokenType["Tag"] = 12] = "Tag";
            TokenType[TokenType["AttrName"] = 13] = "AttrName";
            TokenType[TokenType["AttrValue"] = 14] = "AttrValue";
            TokenType[TokenType["XmlDelimiter"] = 15] = "XmlDelimiter";
            TokenType[TokenType["XmlText"] = 16] = "XmlText";
            TokenType[TokenType["Heading"] = 17] = "Heading";
            TokenType[TokenType["Bold"] = 18] = "Bold";
            TokenType[TokenType["Italic"] = 19] = "Italic";
            TokenType[TokenType["Code"] = 20] = "Code";
            TokenType[TokenType["Link"] = 21] = "Link";
            TokenType[TokenType["ListMarker"] = 22] = "ListMarker";
            TokenType[TokenType["Quote"] = 23] = "Quote";
            TokenType[TokenType["Property"] = 24] = "Property";
            TokenType[TokenType["Function"] = 25] = "Function";
            TokenType[TokenType["Constant"] = 26] = "Constant";
            TokenType[TokenType["Annotation"] = 27] = "Annotation";
            TokenType[TokenType["DocComment"] = 28] = "DocComment";
            TokenType[TokenType["Error"] = 29] = "Error";
            TokenType[TokenType["PrimitiveFunction"] = 30] = "PrimitiveFunction";
            TokenType[TokenType["StatementTerminator"] = 31] = "StatementTerminator";
            // --- Web language token types (appended; never reorder) ---
            TokenType[TokenType["Regex"] = 32] = "Regex";
            TokenType[TokenType["TemplateString"] = 33] = "TemplateString";
            TokenType[TokenType["TemplateDelimiter"] = 34] = "TemplateDelimiter";
            TokenType[TokenType["Decorator"] = 35] = "Decorator";
            TokenType[TokenType["Selector"] = 36] = "Selector";
            TokenType[TokenType["PseudoClass"] = 37] = "PseudoClass";
            TokenType[TokenType["Unit"] = 38] = "Unit";
            TokenType[TokenType["ColorValue"] = 39] = "ColorValue";
            TokenType[TokenType["AtRule"] = 40] = "AtRule";
            TokenType[TokenType["Variable"] = 41] = "Variable";
            TokenType[TokenType["Builtin"] = 42] = "Builtin";
            TokenType[TokenType["TypeParameter"] = 43] = "TypeParameter";
        })(TokenType = Utils.TokenType || (Utils.TokenType = {}));
        /**
         * Helper for building token lists without manually tracking offsets.
         */
        class TokenBuilder {
            constructor() {
                this.tokens = [];
                this.pos = 0;
            }
            push(type, value) {
                const start = this.pos;
                this.pos += value.length;
                this.tokens.push({ type, value, start, end: this.pos });
            }
            advance(n) {
                this.pos += n;
            }
            get position() {
                return this.pos;
            }
            set position(value) {
                this.pos = value;
            }
            get result() {
                return this.tokens;
            }
        }
        Utils.TokenBuilder = TokenBuilder;
        /**
         * Match a regex at the current position; returns the match or null.
         */
        function matchAt(regex, text, pos) {
            // Use sticky flag emulation by anchoring with substring.
            const slice = text.substr(pos);
            const m = regex.exec(slice);
            if (m && m.index === 0) {
                return m;
            }
            return null;
        }
        Utils.matchAt = matchAt;
        /**
         * Escape HTML special characters for safe insertion into innerHTML.
         */
        function escapeHtml(text) {
            return text
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;");
        }
        Utils.escapeHtml = escapeHtml;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Minimal event emitter used internally for editor events.
         */
        class EventEmitter {
            constructor() {
                this.listeners = [];
            }
            on(listener) {
                this.listeners.push(listener);
            }
            off(listener) {
                const idx = this.listeners.indexOf(listener);
                if (idx >= 0) {
                    this.listeners.splice(idx, 1);
                }
            }
            emit(data) {
                for (const l of this.listeners.slice()) {
                    l(data);
                }
            }
        }
        Utils.EventEmitter = EventEmitter;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Computes a line-level diff between two text documents using the
         * classic dynamic-programming Longest Common Subsequence algorithm.
         * The result is a list of DiffLine entries that can be rendered
         * directly by the DiffViewer.
         */
        function computeLineDiff(oldText, newText) {
            const oldLines = oldText.length === 0 ? [] : oldText.split(/\r\n|\r|\n/);
            const newLines = newText.length === 0 ? [] : newText.split(/\r\n|\r|\n/);
            const m = oldLines.length;
            const n = newLines.length;
            // dp[i][j] = length of LCS of oldLines[i..] and newLines[j..]
            const dp = Array.from({ length: m + 1 }, () => new Array(n + 1).fill(0));
            for (let i = m - 1; i >= 0; i--) {
                for (let j = n - 1; j >= 0; j--) {
                    if (oldLines[i] === newLines[j]) {
                        dp[i][j] = dp[i + 1][j + 1] + 1;
                    }
                    else {
                        dp[i][j] = Math.max(dp[i + 1][j], dp[i][j + 1]);
                    }
                }
            }
            // Backtrack to build the diff.
            const result = [];
            let i = 0;
            let j = 0;
            let oldNum = 1;
            let newNum = 1;
            while (i < m && j < n) {
                if (oldLines[i] === newLines[j]) {
                    result.push({ type: "equal", oldLineNumber: oldNum++, newLineNumber: newNum++, content: oldLines[i] });
                    i++;
                    j++;
                }
                else if (dp[i + 1][j] >= dp[i][j + 1]) {
                    result.push({ type: "removed", oldLineNumber: oldNum++, newLineNumber: 0, content: oldLines[i] });
                    i++;
                }
                else {
                    result.push({ type: "added", oldLineNumber: 0, newLineNumber: newNum++, content: newLines[j] });
                    j++;
                }
            }
            while (i < m) {
                result.push({ type: "removed", oldLineNumber: oldNum++, newLineNumber: 0, content: oldLines[i] });
                i++;
            }
            while (j < n) {
                result.push({ type: "added", oldLineNumber: 0, newLineNumber: newNum++, content: newLines[j] });
                j++;
            }
            return result;
        }
        Utils.computeLineDiff = computeLineDiff;
        /**
         * Summarize a diff: number of added / removed lines.
         */
        function summarizeDiff(diff) {
            let added = 0;
            let removed = 0;
            for (const d of diff) {
                if (d.type === "added")
                    added++;
                else if (d.type === "removed")
                    removed++;
            }
            return { added, removed };
        }
        Utils.summarizeDiff = summarizeDiff;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        var EventEmitter = CodeEditor.Utils.EventEmitter;
        /**
         * Holds the document text as an array of lines (no trailing newlines).
         * Provides efficient line-based editing operations and emits change events.
         */
        class TextBuffer {
            constructor() {
                this.lines = [""];
                this._changeEmitter = new EventEmitter();
            }
            get onChange() {
                return this._changeEmitter;
            }
            get lineCount() {
                return this.lines.length;
            }
            getText() {
                return this.lines.join("\n");
            }
            setText(text) {
                if (text.length === 0) {
                    this.lines = [""];
                }
                else {
                    this.lines = text.split(/\r\n|\r|\n/);
                }
                this._changeEmitter.emit({
                    startLine: 0,
                    startColumn: 0,
                    endLine: this.lines.length,
                    endColumn: 0,
                    insertedText: text
                });
            }
            getLine(index) {
                if (index < 0 || index >= this.lines.length) {
                    return "";
                }
                return this.lines[index];
            }
            getLines() {
                return this.lines.slice();
            }
            /**
             * Insert text at the given position. Position is (line, column) where
             * column is a UTF-16 code unit offset within the line.
             */
            insert(line, column, text) {
                if (line < 0 || line >= this.lines.length) {
                    return;
                }
                const before = this.lines[line].substring(0, column);
                const after = this.lines[line].substring(column);
                const inserted = text.split(/\r\n|\r|\n/);
                inserted[0] = before + inserted[0];
                inserted[inserted.length - 1] = inserted[inserted.length - 1] + after;
                const newLines = this.lines.slice(0, line).concat(inserted).concat(this.lines.slice(line + 1));
                this.lines = newLines;
                this._changeEmitter.emit({
                    startLine: line,
                    startColumn: column,
                    endLine: line + inserted.length - 1,
                    endColumn: inserted[inserted.length - 1].length - after.length,
                    insertedText: text
                });
            }
            /**
             * Delete text in the given inclusive range (startLine,startColumn) to
             * (endLine,endColumn).
             */
            deleteRange(startLine, startColumn, endLine, endColumn) {
                if (startLine < 0 || startLine >= this.lines.length) {
                    return "";
                }
                if (endLine < 0 || endLine >= this.lines.length) {
                    return "";
                }
                // Capture deleted text.
                let deleted;
                if (startLine === endLine) {
                    deleted = this.lines[startLine].substring(startColumn, endColumn);
                }
                else {
                    deleted = this.lines[startLine].substring(startColumn);
                    for (let i = startLine + 1; i < endLine; i++) {
                        deleted += "\n" + this.lines[i];
                    }
                    deleted += "\n" + this.lines[endLine].substring(0, endColumn);
                }
                // Rebuild.
                const merged = this.lines[startLine].substring(0, startColumn) + this.lines[endLine].substring(endColumn);
                const newLines = this.lines.slice(0, startLine);
                newLines.push(merged);
                for (let i = endLine + 1; i < this.lines.length; i++) {
                    newLines.push(this.lines[i]);
                }
                this.lines = newLines;
                this._changeEmitter.emit({
                    startLine,
                    startColumn,
                    endLine: startLine,
                    endColumn: startColumn,
                    insertedText: ""
                });
                return deleted;
            }
            /**
             * Replace the entire range with new text (combination of delete + insert).
             */
            replaceRange(startLine, startColumn, endLine, endColumn, text) {
                this.deleteRange(startLine, startColumn, endLine, endColumn);
                this.insert(startLine, startColumn, text);
            }
            /**
             * Return the column index after indenting the given line by one tab
             * (or by tabSize spaces, depending on editor settings).
             */
            getLineLength(line) {
                return this.getLine(line).length;
            }
        }
        Core.TextBuffer = TextBuffer;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        /**
         * Manages a single primary selection (caret + optional range).
         */
        class Cursor {
            constructor() {
                this._selection = {
                    anchor: { line: 0, column: 0 },
                    active: { line: 0, column: 0 }
                };
            }
            get selection() {
                return this._selection;
            }
            setSelection(anchor, active) {
                this._selection = { anchor, active };
            }
            setPosition(pos, keepAnchor = false) {
                if (keepAnchor) {
                    this._selection.active = pos;
                }
                else {
                    this._selection.anchor = pos;
                    this._selection.active = pos;
                }
            }
            get position() {
                return this._selection.active;
            }
            get hasSelection() {
                const a = this._selection.anchor;
                const b = this._selection.active;
                return a.line !== b.line || a.column !== b.column;
            }
            /**
             * Return the selection as an ordered (start <= end) range.
             */
            getOrderedRange() {
                const a = this._selection.anchor;
                const b = this._selection.active;
                if (a.line < b.line || (a.line === b.line && a.column <= b.column)) {
                    return { start: a, end: b };
                }
                return { start: b, end: a };
            }
        }
        Core.Cursor = Cursor;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        var TokenType = CodeEditor.Utils.TokenType;
        /**
         * Manages per-line tokenization cache for the current document.
         * Re-tokenizes lines when they change, carrying state across lines.
         */
        class Highlighter {
            constructor() {
                this.highlighter = null;
                this.cache = [];
                this.dirtyFromLine = 0;
                this.buffer = null;
            }
            setHighlighter(h) {
                this.highlighter = h;
                this.cache = [];
                this.dirtyFromLine = 0;
            }
            get language() {
                return this.highlighter ? this.highlighter.language : "plain";
            }
            invalidate(fromLine) {
                if (fromLine < this.dirtyFromLine) {
                    this.dirtyFromLine = fromLine;
                }
            }
            invalidateAll() {
                this.cache = [];
                this.dirtyFromLine = 0;
            }
            /**
             * Ensure cache is valid up to and including `line`. Returns the tokens
             * for that line.
             */
            getTokens(line, lineText) {
                if (!this.highlighter) {
                    return [{ type: TokenType.Plain, value: lineText, start: 0, end: lineText.length }];
                }
                // If cache is stale, re-tokenize from dirtyFromLine forward.
                if (this.dirtyFromLine <= line) {
                    this.retokenize(Math.max(0, this.dirtyFromLine));
                }
                if (line < this.cache.length && this.cache[line]) {
                    return this.cache[line].tokens;
                }
                return [];
            }
            retokenize(fromLine) {
                if (!this.highlighter)
                    return;
                // Determine starting state.
                let state;
                if (fromLine === 0 || this.cache.length === 0) {
                    state = this.highlighter.initialState();
                    fromLine = 0;
                    this.cache = [];
                }
                else {
                    state = this.cache[fromLine - 1] ? this.cache[fromLine - 1].state : this.highlighter.initialState();
                }
                // We need the full document text to retokenize. The caller (Editor)
                // will pass lines via getTokens, but for retokenization we need
                // access to all lines. We store a reference to the buffer.
                if (!this.buffer) {
                    return;
                }
                const lineCount = this.buffer.lineCount;
                for (let i = fromLine; i < lineCount; i++) {
                    const lineText = this.buffer.getLine(i);
                    const result = this.highlighter.tokenizeLine(lineText, state);
                    this.cache[i] = { tokens: result.tokens, state: result.state };
                    state = result.state;
                }
                // Truncate any stale entries beyond lineCount.
                this.cache.length = lineCount;
                this.dirtyFromLine = lineCount;
            }
            setBuffer(buffer) {
                this.buffer = buffer;
                this.invalidateAll();
            }
        }
        Core.Highlighter = Highlighter;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * VB.NET syntax highlighter.
         *
         * Handles:
         *   - Line comments (') and REM statements
         *   - XML doc comments ('')
         *   - String literals ("..." with "" escape) and interpolated strings ($"...{...}...")
         *   - Char literals ("a"c)
         *   - Numbers (decimal, hex &H..., binary &B..., octal &O...)
         *   - Preprocessor (#If, #End If, #Region, etc.)
         *   - Keywords (control flow, declarations, modifiers)
         *   - Type characters and identifiers
         *   - Multi-line XML literals (basic)
         */
        class VbNetHighlighter {
            constructor() {
                this.language = "vbnet";
            }
            initialState() {
                return { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0, stringChar: "", interp: false };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Continue a multi-line string opened on a previous line.
                if (state && state.inString) {
                    const interp = !!(state.interp);
                    const [j, closed] = VbNetHighlighter.scanStringBody(line, i, interp);
                    // Trailing '\r' at end-of-line inside a string body is tolerated.
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    if (closed) {
                        state = { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0, stringChar: "", interp: false };
                    }
                    else {
                        return { tokens: b.result, state };
                    }
                }
                while (i < n) {
                    const ch = line[i];
                    // Line comment.
                    if (ch === "'") {
                        // Check for XML doc comment (''')
                        if (line.substr(i, 3) === "'''") {
                            b.push(TokenType.DocComment, line.substr(i));
                            i = n;
                            break;
                        }
                        b.push(TokenType.Comment, line.substr(i));
                        i = n;
                        break;
                    }
                    // REM comment (only at start of token).
                    if (ch === "R" || ch === "r") {
                        const remMatch = /^REM\b/i.exec(line.substr(i));
                        if (remMatch && (i === 0 || /\s/.test(line[i - 1]))) {
                            b.push(TokenType.Comment, line.substr(i));
                            i = n;
                            break;
                        }
                    }
                    // Preprocessor directive.
                    if (ch === "#" && (i === 0 || /\s/.test(line[i - 1]))) {
                        const ppMatch = /^#\s*[A-Za-z]+/.exec(line.substr(i));
                        if (ppMatch) {
                            b.push(TokenType.Preprocessor, ppMatch[0]);
                            i += ppMatch[0].length;
                            continue;
                        }
                    }
                    // String literal (may span multiple lines). VB.NET escapes a quote by doubling it.
                    if (ch === '"') {
                        let j;
                        let closed;
                        // Char literal: a single character between quotes followed by 'c', e.g. "a"c.
                        // Must be closed on this line and the char type suffix 'c' present.
                        let isCharLiteral = false;
                        [j, closed] = VbNetHighlighter.scanStringBody(line, i + 1, false);
                        if (closed && j < n && line[j] === "c") {
                            const inner = line.substring(i + 1, j - 1);
                            // A char literal is a single code point (or escaped "") followed by c.
                            if (inner.length === 1 || (inner.length === 2 && inner[0] === '"' && inner[1] === '"')) {
                                isCharLiteral = true;
                            }
                        }
                        if (isCharLiteral) {
                            const str = line.substring(i, j + 1);
                            b.push(TokenType.String, str);
                            i = j + 1;
                            continue;
                        }
                        // Plain string: emit what we have; if unclosed carry state across lines.
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        if (!closed) {
                            return { tokens: b.result, state: { inBlockComment: false, inXmlLiteral: false, inString: true, stringDepth: 0, stringChar: '"', interp: false } };
                        }
                        continue;
                    }
                    // Interpolated string: $"..." or $@"..." (supports multiple lines).
                    if ((ch === "$" && line[i + 1] === '"') || (ch === "$" && line[i + 1] === "@" && line[i + 2] === '"')) {
                        const startChar = line[i + 1] === "@" ? 2 : 1;
                        const [j, closed] = VbNetHighlighter.scanStringBody(line, i + 1 + startChar, true);
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        if (!closed) {
                            return { tokens: b.result, state: { inBlockComment: false, inXmlLiteral: false, inString: true, stringDepth: 0, stringChar: '"', interp: true } };
                        }
                        continue;
                    }
                    // Number (including &H hex, &B binary, &O octal).
                    if (ch === "&" && (line[i + 1] === "H" || line[i + 1] === "h" || line[i + 1] === "B" || line[i + 1] === "b" || line[i + 1] === "O" || line[i + 1] === "o")) {
                        let j = i + 2;
                        while (j < n && /[0-9A-Fa-f]/.test(line[j]))
                            j++;
                        // Optional type suffix
                        if (j < n && /[A-Za-z]/.test(line[j])) {
                            const suffixMatch = /^[A-Za-z]+/.exec(line.substr(j));
                            if (suffixMatch && /^(S|I|L|US|UI|UL|D|F|R|C)$/i.test(suffixMatch[0])) {
                                j += suffixMatch[0].length;
                            }
                        }
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    if (/[0-9]/.test(ch) || (ch === "." && /[0-9]/.test(line[i + 1] || ""))) {
                        let j = i;
                        while (j < n && /[0-9.eE+\-]/.test(line[j])) {
                            // Stop on +/- that isn't part of exponent.
                            if ((line[j] === "+" || line[j] === "-") && j > i) {
                                const prev = line[j - 1];
                                if (prev !== "e" && prev !== "E")
                                    break;
                            }
                            j++;
                        }
                        // Type suffix.
                        if (j < n && /[A-Za-z%&#@!]/.test(line[j])) {
                            const sm = /^[A-Za-z%&#@!]+/.exec(line.substr(j));
                            if (sm)
                                j += sm[0].length;
                        }
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Identifier or keyword.
                    if (/[A-Za-z_]/.test(ch)) {
                        let j = i;
                        while (j < n && /[A-Za-z0-9_]/.test(line[j]))
                            j++;
                        // Type character suffix.
                        if (j < n && /[%&@!#$]/.test(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        const wordNoSuffix = word.replace(/[%&@!#$]+$/, "");
                        if (VbNetHighlighter.CONTROL_KEYWORDS.has(wordNoSuffix)) {
                            b.push(TokenType.ControlKeyword, word);
                        }
                        else if (VbNetHighlighter.KEYWORDS.has(wordNoSuffix)) {
                            b.push(TokenType.Keyword, word);
                        }
                        else if (VbNetHighlighter.TYPES.has(wordNoSuffix)) {
                            b.push(TokenType.Type, word);
                        }
                        else {
                            // Check if it's a function/sub call (followed by '(').
                            let k = j;
                            while (k < n && /\s/.test(line[k]))
                                k++;
                            if (line[k] === "(") {
                                b.push(TokenType.Function, word);
                            }
                            else {
                                b.push(TokenType.Identifier, word);
                            }
                        }
                        i = j;
                        continue;
                    }
                    // Operators and punctuation.
                    if (/[+\-*/\\^<>=&!]/.test(ch)) {
                        let j = i;
                        while (j < n && /[+\-*/\\^<>=&!]/.test(line[j]))
                            j++;
                        b.push(TokenType.Operator, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    if (/[(){}\[\],.;:]/.test(ch)) {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // Whitespace or anything else.
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                return { tokens: b.result, state };
            }
            // Scan the body of a string starting at `start`, returning the index just past
            // the closing quote (or n when unclosed) and whether it closed on this line.
            // For interpolated strings (interp=true) we track brace depth so that quotes
            // inside an {expression} do not close the string; literal braces are written {{ / }}.
            // VB.NET escapes a quote by doubling it (""), which is skipped in both modes.
            static scanStringBody(line, start, interp) {
                const n = line.length;
                let j = start;
                let depth = 0;
                while (j < n) {
                    const c = line[j];
                    if (interp) {
                        if (c === "{" && line[j + 1] === "{") {
                            j += 2;
                            continue;
                        } // literal {{
                        if (c === "}" && line[j + 1] === "}") {
                            j += 2;
                            continue;
                        } // literal }}
                        if (c === "{") {
                            depth++;
                            j++;
                            continue;
                        }
                        if (c === "}") {
                            if (depth > 0)
                                depth--;
                            j++;
                            continue;
                        }
                        if (c === '"') {
                            if (line[j + 1] === '"') {
                                j += 2;
                                continue;
                            } // escaped quote
                            if (depth === 0) {
                                j++;
                                return [j, true];
                            }
                            // quote inside an expression: not a string terminator
                            j++;
                            continue;
                        }
                    }
                    else {
                        if (c === '"') {
                            if (line[j + 1] === '"') {
                                j += 2;
                                continue;
                            } // escaped quote
                            j++;
                            return [j, true];
                        }
                    }
                    j++;
                }
                return [j, false];
            }
        }
        // Keywords that begin/control statements.
        VbNetHighlighter.CONTROL_KEYWORDS = new Set([
            "If", "Then", "Else", "ElseIf", "End", "Select", "Case", "For", "Each",
            "In", "While", "Until", "Loop", "Do", "Next", "Exit", "Continue", "Return",
            "Yield", "Try", "Catch", "Finally", "Throw", "When", "Using", "SyncLock",
            "With", "Step", "To", "GoTo", "Stop", "End"
        ]);
        // Declaration / modifier keywords.
        VbNetHighlighter.KEYWORDS = new Set([
            "Public", "Private", "Protected", "Friend", "Shared", "Static", "ReadOnly",
            "WriteOnly", "Dim", "Const", "Class", "Module", "Structure", "Interface",
            "Enum", "Namespace", "Sub", "Function", "Property", "Operator", "Event",
            "Delegate", "Handles", "Implements", "Inherits", "Of", "As", "New", "Me",
            "MyBase", "MyClass", "Nothing", "True", "False", "Null", "And", "Or", "Not",
            "Xor", "AndAlso", "OrElse", "Is", "IsNot", "Like", "Mod", "TypeOf", "GetType",
            "AddressOf", "Await", "Async", "Iterator", "Partial", "Overridable",
            "Overloads", "Overrides", "MustInherit", "MustOverride", "NotOverridable",
            "Shadows", "Widening", "Narrowing", "ByVal", "ByRef", "Optional", "ParamArray",
            "Declare", "Lib", "Alias", "Narrowing", "Widening", "Mid", "Option",
            "Explicit", "Strict", "Compare", "Text", "Binary", "Off", "On", "Infer",
            "Custom", "AddHandler", "RemoveHandler", "RaiseEvent", "DirectCast",
            "TryCast", "CType", "CInt", "CStr", "CBool", "CDbl", "CDec", "CLng",
            "CShort", "CSng", "CByte", "CChar", "CDate", "CUInt", "CULng", "CUShort",
            "CSByte", "Let", "Set", "Get", "Wend", "Call", "ReDim", "Preserve",
            "Erase", "Error", "Resume", "On", "Print", "Input", "Line", "Width",
            "Open", "Close", "Put", "Get", "Imports", "Option", "Region", "End",
            "ExternalSource", "ExternalChecksum", "If", "EndIf"
        ]);
        // Built-in value types and common framework types.
        VbNetHighlighter.TYPES = new Set([
            "Boolean", "Byte", "SByte", "Char", "Date", "Decimal", "Double", "Single",
            "Integer", "UInteger", "Long", "ULong", "Short", "UShort", "String", "Object",
            "Void", "IntPtr", "UIntPtr"
        ]);
        Highlighters.VbNetHighlighter = VbNetHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * R syntax highlighter.
         *
         * Handles:
         *   - Comments (# to end of line)
         *   - Roxygen comments (#' ...)
         *   - Strings: single, double, and backtick-quoted raw strings r"(...)"
         *   - Numbers (incl. scientific, hex 0x..., and 1L integer suffix)
         *   - Control-flow keywords (if, for, while, function, ...)
         *   - Built-in constants (TRUE, FALSE, NULL, NA, Inf, NaN, ...)
         *   - Function call detection (identifier followed by '(')
         *   - Infix operators (%>%, %in%, etc.)
         *   - Assignment operators (<-, ->, <<-, ->>, =)
         *   - Statement terminators (; highlighted distinctly in sky blue)
         */
        class RHighlighter {
            constructor() {
                this.language = "r";
            }
            initialState() {
                return { inString: false, stringChar: "", rawClose: "" };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Continue a multi-line string opened on a previous line.
                if (state && state.inString) {
                    const rawClose = state.rawClose || "";
                    const closeCh = state.stringChar || '"';
                    // For raw strings the terminator is `rawClose + '"'`; for plain
                    // strings the terminator is a single (non-escaped) quote.
                    let j = 0;
                    if (rawClose) {
                        while (j < n) {
                            if (line[j] === rawClose && line[j + 1] === '"') {
                                j += 2;
                                break;
                            }
                            j++;
                        }
                    }
                    else {
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === closeCh) {
                                j++;
                                break;
                            }
                            j++;
                        }
                    }
                    // Tolerate a trailing '\r' at end-of-line inside the string body.
                    let end = j;
                    while (end > i && line[end - 1] === "\r")
                        end--;
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    if (j < n) {
                        // String closed on this line; clear continuation state.
                        state = { inString: false, stringChar: "", rawClose: "" };
                    }
                    else {
                        // Still inside the string; carry state to next line.
                        return { tokens: b.result, state };
                    }
                }
                while (i < n) {
                    const ch = line[i];
                    // Roxygen comment.
                    if (ch === "#" && line[i + 1] === "'") {
                        b.push(TokenType.DocComment, line.substr(i));
                        i = n;
                        break;
                    }
                    // Regular comment.
                    if (ch === "#") {
                        b.push(TokenType.Comment, line.substr(i));
                        i = n;
                        break;
                    }
                    // Raw string: r"(...)", r"[...]", r"{...}". May span multiple lines.
                    if ((ch === "r" || ch === "R") && line[i + 1] === '"') {
                        const open = line[i + 2];
                        if (open === "(" || open === "[" || open === "{") {
                            const close = open === "(" ? ")" : open === "[" ? "]" : "}";
                            let j = i + 3;
                            while (j < n) {
                                if (line[j] === close && line[j + 1] === '"') {
                                    j += 2;
                                    break;
                                }
                                j++;
                            }
                            b.push(TokenType.String, line.substring(i, j));
                            i = j;
                            if (j >= n) {
                                // Unclosed raw string: carry state across lines.
                                return { tokens: b.result, state: { inString: true, stringChar: '"', rawClose: close } };
                            }
                            continue;
                        }
                    }
                    // Double-quoted string. May span multiple lines.
                    if (ch === '"') {
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === '"') {
                                j++;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        if (j >= n) {
                            // Unclosed at end of line: carry state across lines.
                            return { tokens: b.result, state: { inString: true, stringChar: '"', rawClose: "" } };
                        }
                        continue;
                    }
                    // Single-quoted string. May span multiple lines.
                    if (ch === "'") {
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === "'") {
                                j++;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        if (j >= n) {
                            // Unclosed at end of line: carry state across lines.
                            return { tokens: b.result, state: { inString: true, stringChar: "'", rawClose: "" } };
                        }
                        continue;
                    }
                    // Backtick identifier.
                    if (ch === "`") {
                        let j = i + 1;
                        while (j < n && line[j] !== "`")
                            j++;
                        if (j < n)
                            j++;
                        b.push(TokenType.Identifier, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Number (hex, decimal, scientific, integer suffix L, complex i).
                    if (/[0-9]/.test(ch) || (ch === "." && /[0-9]/.test(line[i + 1] || ""))) {
                        let j = i;
                        if (ch === "0" && (line[i + 1] === "x" || line[i + 1] === "X")) {
                            j = i + 2;
                            while (j < n && /[0-9A-Fa-f]/.test(line[j]))
                                j++;
                        }
                        else {
                            while (j < n && /[0-9.]/.test(line[j]))
                                j++;
                            if (j < n && (line[j] === "e" || line[j] === "E")) {
                                j++;
                                if (j < n && (line[j] === "+" || line[j] === "-"))
                                    j++;
                                while (j < n && /[0-9]/.test(line[j]))
                                    j++;
                            }
                        }
                        if (j < n && (line[j] === "L" || line[j] === "i"))
                            j++;
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Infix operator %...%.
                    if (ch === "%") {
                        let j = i + 1;
                        while (j < n && line[j] !== "%")
                            j++;
                        if (j < n)
                            j++;
                        const op = line.substring(i, j);
                        b.push(RHighlighter.PRIMITIVE_OPS.has(op) ? TokenType.PrimitiveFunction : TokenType.Operator, op);
                        i = j;
                        continue;
                    }
                    // Assignment and other operators.
                    if (ch === "<" && line[i + 1] === "-" || ch === "-" && line[i + 1] === ">" ||
                        ch === "<" && line[i + 1] === "<" && line[i + 2] === "-" ||
                        ch === "-" && line[i + 1] === "-" && line[i + 2] === ">") {
                        let j = i;
                        if (line[j] === "<" && line[j + 1] === "<" && line[j + 2] === "-")
                            j += 3;
                        else if (line[j] === "-" && line[j + 1] === "-" && line[j + 2] === ">")
                            j += 3;
                        else
                            j += 2;
                        const asg = line.substring(i, j);
                        b.push(RHighlighter.PRIMITIVE_OPS.has(asg) ? TokenType.PrimitiveFunction : TokenType.Operator, asg);
                        i = j;
                        continue;
                    }
                    // Bracket-index operators [[ ]] (R primitives). Single [ ] are
                    // handled as punctuation below.
                    if (ch === "[" || ch === "]") {
                        if (line[i + 1] === ch) {
                            b.push(TokenType.PrimitiveFunction, line.substring(i, i + 2));
                            i += 2;
                        }
                        else {
                            b.push(TokenType.Punctuation, ch);
                            i++;
                        }
                        continue;
                    }
                    if (/[+\-*/^<>=!&|~$@?:]/.test(ch)) {
                        let j = i;
                        while (j < n && /[+\-*/^<>=!&|~$@?:]/.test(line[j]))
                            j++;
                        const op = line.substring(i, j);
                        if (RHighlighter.PRIMITIVE_OPS.has(op)) {
                            b.push(TokenType.PrimitiveFunction, op);
                        }
                        else {
                            b.push(TokenType.Operator, op);
                        }
                        i = j;
                        continue;
                    }
                    // Identifier or keyword.
                    if (/[A-Za-z_.]/.test(ch)) {
                        let j = i;
                        while (j < n && /[A-Za-z0-9_.]/.test(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        if (RHighlighter.CONTROL_KEYWORDS.has(word)) {
                            b.push(TokenType.ControlKeyword, word);
                        }
                        else if (RHighlighter.KEYWORDS.has(word)) {
                            b.push(TokenType.Keyword, word);
                        }
                        else if (RHighlighter.CONSTANTS.has(word)) {
                            b.push(TokenType.Constant, word);
                        }
                        else if (RHighlighter.PRIMITIVES.has(word)) {
                            // Primitive function call detection.
                            let k = j;
                            while (k < n && /\s/.test(line[k]))
                                k++;
                            if (line[k] === "(") {
                                b.push(TokenType.PrimitiveFunction, word);
                            }
                            else {
                                b.push(TokenType.Identifier, word);
                            }
                        }
                        else {
                            // Function call detection.
                            let k = j;
                            while (k < n && /\s/.test(line[k]))
                                k++;
                            if (line[k] === "(" || line[k] === "<" && line[k + 1] === "-") {
                                if (line[k] === "(") {
                                    b.push(TokenType.Function, word);
                                }
                                else {
                                    b.push(TokenType.Identifier, word);
                                }
                            }
                            else {
                                b.push(TokenType.Identifier, word);
                            }
                        }
                        i = j;
                        continue;
                    }
                    // Statement terminator: semicolon (highlights statement ends).
                    if (ch === ";") {
                        b.push(TokenType.StatementTerminator, ch);
                        i++;
                        continue;
                    }
                    // Punctuation.
                    if (/[(){}\[\],]/.test(ch)) {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                return { tokens: b.result, state };
            }
        }
        RHighlighter.CONTROL_KEYWORDS = new Set([
            "if", "else", "for", "while", "repeat", "function", "return", "break",
            "next", "in", "switch"
        ]);
        RHighlighter.KEYWORDS = new Set([
            "local", "global", "library", "require", "source", "invisible",
            "on", "exit"
        ]);
        RHighlighter.CONSTANTS = new Set([
            "TRUE", "FALSE", "NULL", "NA", "NA_integer_", "NA_real_", "NA_complex_",
            "NA_character_", "Inf", "-Inf", "NaN", "T", "F", "pi", "LETTERS", "letters",
            "month.abb", "month.name"
        ]);
        /**
         * R's internal primitive functions (prefix form), sourced from
         * base:::primaries / get("__Primitives__", baseenv()). These are
         * highlighted distinctly from user-defined (third-party) functions.
         * Control-flow keywords and constants are intentionally excluded here
         * so they keep their existing keyword/constant styling.
         */
        RHighlighter.PRIMITIVES = new Set([
            ".subset", ".subset2", ".External", ".Call", ".Fortran", ".C",
            "c", "list", "vector", "numeric", "character", "logical", "integer",
            "double", "complex", "raw", "structure", "attributes", "attr",
            "class", "unclass", "names", "dim", "dimnames", "length", "levels",
            "typeof", "storage.mode", "mode", "oldClass", "comment",
            "as.character", "as.numeric", "as.integer", "as.logical",
            "as.complex", "as.double", "as.raw", "as.vector", "as.list",
            "is.null", "is.na", "is.nan", "is.finite", "is.infinite",
            "is.numeric", "is.character", "is.logical", "is.complex",
            "is.raw", "is.list", "is.function", "is.expression", "is.object",
            "is.single", "is.environment", "is.pairlist", "is.language",
            "is.symbol", "is.matrix", "is.array", "is.atomic", "is.recursive",
            "is.call", "is.expression", "is.primitive", "is.s4",
            "substitute", "quote", "enquote", "eval", "evalq", "call",
            "expression", "force", "on.exit", "environment", "globalenv",
            "baseenv", "emptyenv", "new.env", "parent.env", "parent.frame",
            "sys.frame", "sys.function", "sys.call", "sys.parent", "sys.nframe",
            "sys.calls", "sys.frames", "sys.parents", "sys.body", "sys.function",
            "missing", "nargs", "Recall", "UseMethod", "NextMethod",
            "standardGeneric", "body", "formals", "args", "invisible",
            "withVisible", "delayedAssign", "bindenv", "lockBinding",
            "unlockBinding", "lockEnvironment", "makelazy", "pos.to.env",
            "proc.time", "gc", "gc.time", "memory.profile", "tracemem",
            "retracemem", "untracemem", "gctorture", "gctorture2",
            "file", "textConnection", "gzfile", "bzfile", "xzfile", "unz",
            "pipe", "socketConnection", "url", "stdin", "stdout", "stderr",
            "readLines", "writeLines", "cat", "print", "format", "format.info",
            "paste", "paste0", "sprintf", "strsplit", "sub", "gsub", "match",
            "pmatch", "charmatch", "startsWith", "endsWith", "grep", "grepl",
            "regexpr", "gregexpr", "agrep", "tolower", "toupper", "chartr",
            "abbreviate", "nchar", "nzchar", "substr", "substring", "strtrim",
            "make.names", "make.unique", "all", "any", "sum", "prod", "min",
            "max", "range", "mean", "median", "var", "sd", "cov", "cor",
            "diff", "cumsum", "cumprod", "cummax", "cummin", "round", "signif",
            "trunc", "floor", "ceiling", "abs", "sign", "sqrt", "exp", "log",
            "expm1", "log1p", "cos", "sin", "tan", "acos", "asin", "atan",
            "cosh", "sinh", "tanh", "acos", "acosh", "asinh", "atanh",
            "gamma", "lgamma", "digamma", "trigamma", "choose", "factorial",
            "beta", "lbeta", "rowSums", "colSums", "rowMeans", "colMeans",
            "apply", "lapply", "sapply", "vapply", "tapply", "mapply", "Map",
            "Reduce", "Filter", "Find", "Position", "Negate", "eapply",
            "rapply", "outer", "kronecker", "sweep", "scale", "rowsum",
            "aggregate", "by", "split", "unsplit", "rbind", "cbind",
            "data.frame", "as.data.frame", "expand.grid", "order", "sort",
            "sort.list", "rank", "unique", "duplicated", "union", "intersect",
            "setdiff", "setequal", "is.element", "which", "which.min",
            "which.max", "array", "matrix", "diag", "upper.tri", "lower.tri",
            "t", "crossprod", "tcrossprod", "solve", "eigen", "svd", "qr",
            "det", "determinant", "fft", "nextn", "convolve", "filter", "poly",
            "sample", "rnorm", "runif", "rpois", "rexp", "rbinom", "rbeta",
            "rgamma", "rchisq", "rt", "rf", "rgeom", "rhyper", "rnbinom",
            "rweibull", "rwilcox", "rsignrank", "set.seed", "date", "Sys.time",
            "Sys.Date", "as.POSIXct", "as.Date", "difftime", "julian",
            "months", "quarters", "weekdays", "try", "tryCatch",
            "withCallingHandlers", "stop", "warning", "message", "gettext",
            "gettextf", "ngettext", "options", "getOption", "par", "dev.off",
            "plot", "hist", "boxplot", "points", "lines", "abline", "title",
            "axis", "legend", "text", "arrows", "segments", "polygon", "curve",
            "pairs", "coplot", "image", "contour", "persp", "barplot",
            "dotchart", "identify", "locator", "stem", "qqnorm", "qqline",
            "rep", "seq", "seq.int", "seq_len", "seq_along", "rev", "rep.int",
            "rep_len", "table", "xtabs", "prop.table", "margin.table",
            "ftable", "as.table", "aperm", "trace", "untrace", "browser",
            "recover", "capabilities", "machine", "commandArgs", "getwd",
            "setwd", "getenv", "setenv", "unsetenv", "Sys.getenv",
            "Sys.setenv", "Sys.unsetenv", "list.files", "dir", "file.path",
            "normalizePath", "basename", "dirname", "file.exists",
            "file.choose", "file.copy", "file.create", "file.remove",
            "file.rename", "file.append", "file.symlink", "dir.create",
            "tempfile", "tempdir", "R.home", "system", "system2", "shell",
            "shell.exec", "Sys.which", "Sys.info", "load", "save", "saveRDS",
            "readRDS", "serialize", "unserialize", "serializeToConn",
            "assign", "get", "exists", "remove", "rm", "ls", "objects",
            "laply", "mget", "eapply", "attach", "detach", "with", "within",
            "local", "do.call", "do.call", "browser", "interactive",
            "is.loaded", "dyn.load", "dyn.unload", "getLoadedDLLs",
            "noquote", "dQuote", "sQuote", "encodeString", "iconv",
            "iconvlist", "utf8ToInt", "intToUtf8", "charToRaw", "rawToChar",
            "rawShift", "intToBits", "rawConnection", "rawConnectionValue",
            "seek", "truncate", "flush", "close", "open", "isOpen",
            "readChar", "writeChar", "readBin", "writeBin", "readLines",
            "pushBack", "clearPushBack", "getConnection", "summary",
            "print", "cat", "format", "str", "ls", "dump", "dput", "dget",
            "withRestarts", "signalCondition", "simpleCondition",
            "errorCondition", "warningCondition", "restart", "invokeRestart",
            "computeRestarts", "findRestart", "conditionCall",
            "conditionMessage", "geterrmessage", "gregexpr", "sub", "gsub",
            "nrow", "ncol", "isTRUE", "isFALSE", "suppressPackageStartupMessages",
            "dir.exists"
        ]);
        /**
         * R's internal primitive operators (infix/symbol form). When a run of
         * operator characters or an identifier-style operator matches one of
         * these, it is highlighted as a primitive.
         */
        RHighlighter.PRIMITIVE_OPS = new Set([
            "+", "-", "*", "/", "^", "%%", "%/%", "%*%", "%o%", "%x%", "%in%",
            ":", ">", "<", ">=", "<=", "==", "!=", "!", "&", "&&", "|", "||",
            "~", "<-", "<<-", "->", "->>", "$", "@", "[[", "]]", "[", "]", "="
        ]);
        Highlighters.RHighlighter = RHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * JSON syntax highlighter. Supports per-line state for multi-line strings
         * (rare but valid in some streaming parsers) and tracks whether we are
         * inside an object key context vs. value context for nicer coloring.
         */
        class JsonHighlighter {
            constructor() {
                this.language = "json";
            }
            initialState() {
                return { inString: false, depth: 0, expectKey: false };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                let inString = state.inString === true;
                let expectKey = state.expectKey === true;
                let strStart = 0;
                while (i < n) {
                    const ch = line[i];
                    if (inString) {
                        // Continue string from previous line.
                        let j = i;
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === '"') {
                                j++;
                                break;
                            }
                            j++;
                        }
                        if (j >= n) {
                            // Still in string at end of line.
                            b.push(TokenType.String, line.substring(strStart));
                            state = { inString: true, depth: state.depth, expectKey };
                            return { tokens: b.result, state };
                        }
                        const str = line.substring(strStart, j);
                        if (expectKey) {
                            b.push(TokenType.Property, str);
                        }
                        else {
                            b.push(TokenType.String, str);
                        }
                        inString = false;
                        i = j;
                        continue;
                    }
                    // Whitespace.
                    if (/\s/.test(ch)) {
                        let j = i;
                        while (j < n && /\s/.test(line[j]))
                            j++;
                        b.push(TokenType.Plain, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // String.
                    if (ch === '"') {
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === '"') {
                                j++;
                                break;
                            }
                            j++;
                        }
                        if (j >= n && line[n - 1] !== '"') {
                            // Multi-line string (rare). Treat rest of line as string start.
                            strStart = i;
                            inString = true;
                            b.push(TokenType.String, line.substring(i));
                            i = n;
                            continue;
                        }
                        const str = line.substring(i, j);
                        // Peek next non-space char to decide if it's a key.
                        let k = j;
                        while (k < n && /\s/.test(line[k]))
                            k++;
                        if (line[k] === ":") {
                            b.push(TokenType.Property, str);
                        }
                        else {
                            b.push(TokenType.String, str);
                        }
                        i = j;
                        continue;
                    }
                    // Number.
                    if (/[0-9\-]/.test(ch) && (ch !== "-" || /[0-9]/.test(line[i + 1] || ""))) {
                        let j = i;
                        if (line[j] === "-")
                            j++;
                        while (j < n && /[0-9.eE+\-]/.test(line[j])) {
                            if ((line[j] === "+" || line[j] === "-") && j > i) {
                                const prev = line[j - 1];
                                if (prev !== "e" && prev !== "E")
                                    break;
                            }
                            j++;
                        }
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Constants.
                    if (/[a-z]/.test(ch)) {
                        let j = i;
                        while (j < n && /[a-z]/.test(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        if (word === "true" || word === "false" || word === "null") {
                            b.push(TokenType.Constant, word);
                        }
                        else {
                            b.push(TokenType.Plain, word);
                        }
                        i = j;
                        continue;
                    }
                    // Punctuation.
                    if (ch === "{" || ch === "[") {
                        b.push(TokenType.Punctuation, ch);
                        if (ch === "{")
                            expectKey = true;
                        i++;
                        continue;
                    }
                    if (ch === "}" || ch === "]") {
                        b.push(TokenType.Punctuation, ch);
                        expectKey = false;
                        i++;
                        continue;
                    }
                    if (ch === ":") {
                        b.push(TokenType.Operator, ch);
                        expectKey = false;
                        i++;
                        continue;
                    }
                    if (ch === ",") {
                        b.push(TokenType.Punctuation, ch);
                        // After comma in object, expect key; in array, expect value.
                        // We can't perfectly know without tracking, so use heuristic.
                        i++;
                        continue;
                    }
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                state = { inString, depth: state.depth, expectKey };
                return { tokens: b.result, state };
            }
        }
        Highlighters.JsonHighlighter = JsonHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * XML syntax highlighter with multi-line state tracking.
         * Handles tags, attributes, comments, CDATA, processing instructions.
         */
        class XmlHighlighter {
            constructor() {
                this.language = "xml";
            }
            initialState() {
                return { inComment: false, inCData: false, inTag: false, inString: false, stringChar: "" };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Continue multi-line comment.
                if (state.inComment) {
                    const endIdx = line.indexOf("-->");
                    if (endIdx < 0) {
                        b.push(TokenType.Comment, line);
                        return { tokens: b.result, state: { ...state, inComment: true } };
                    }
                    b.push(TokenType.Comment, line.substring(0, endIdx + 3));
                    i = endIdx + 3;
                    state = { ...state, inComment: false };
                }
                // Continue multi-line CDATA.
                if (state.inCData) {
                    const endIdx = line.indexOf("]]>");
                    if (endIdx < 0) {
                        b.push(TokenType.XmlText, line);
                        return { tokens: b.result, state: { ...state, inCData: true } };
                    }
                    b.push(TokenType.XmlText, line.substring(0, endIdx + 3));
                    i = endIdx + 3;
                    state = { ...state, inCData: false };
                }
                while (i < n) {
                    const ch = line[i];
                    // Comment start.
                    if (line.substr(i, 4) === "<!--") {
                        const endIdx = line.indexOf("-->", i + 4);
                        if (endIdx < 0) {
                            b.push(TokenType.Comment, line.substr(i));
                            return { tokens: b.result, state: { ...state, inComment: true } };
                        }
                        b.push(TokenType.Comment, line.substring(i, endIdx + 3));
                        i = endIdx + 3;
                        continue;
                    }
                    // CDATA.
                    if (line.substr(i, 9) === "<![CDATA[") {
                        const endIdx = line.indexOf("]]>", i + 9);
                        if (endIdx < 0) {
                            b.push(TokenType.XmlText, line.substr(i));
                            return { tokens: b.result, state: { ...state, inCData: true } };
                        }
                        b.push(TokenType.XmlText, line.substring(i, endIdx + 3));
                        i = endIdx + 3;
                        continue;
                    }
                    // Processing instruction <?xml ...?>
                    if (line.substr(i, 2) === "<?") {
                        const endIdx = line.indexOf("?>", i + 2);
                        if (endIdx < 0) {
                            b.push(TokenType.Preprocessor, line.substr(i));
                            i = n;
                            break;
                        }
                        b.push(TokenType.Preprocessor, line.substring(i, endIdx + 2));
                        i = endIdx + 2;
                        continue;
                    }
                    // Tag start.
                    if (ch === "<") {
                        let j = i + 1;
                        if (line[j] === "/")
                            j++;
                        while (j < n && /[A-Za-z0-9_:\-.]/.test(line[j]))
                            j++;
                        // Tag name.
                        const tagEnd = j;
                        // Emit '<' or '</' delimiter.
                        const delim = line.substring(i, line[i + 1] === "/" ? i + 2 : i + 1);
                        b.push(TokenType.XmlDelimiter, delim);
                        const tagName = line.substring(line[i + 1] === "/" ? i + 2 : i + 1, tagEnd);
                        if (tagName.length > 0) {
                            b.push(TokenType.Tag, tagName);
                        }
                        i = tagEnd;
                        // Now parse attributes until '>' or '/>'.
                        while (i < n && line[i] !== ">") {
                            // Skip whitespace.
                            if (/\s/.test(line[i])) {
                                let k = i;
                                while (k < n && /\s/.test(line[k]))
                                    k++;
                                b.push(TokenType.Plain, line.substring(i, k));
                                i = k;
                                continue;
                            }
                            // Self-close.
                            if (line[i] === "/" && line[i + 1] === ">") {
                                b.push(TokenType.XmlDelimiter, "/>");
                                i += 2;
                                break;
                            }
                            // Attribute name.
                            if (/[A-Za-z_:@]/.test(line[i])) {
                                let k = i;
                                while (k < n && /[A-Za-z0-9_:\-.]/.test(line[k]))
                                    k++;
                                b.push(TokenType.AttrName, line.substring(i, k));
                                i = k;
                                continue;
                            }
                            // '='
                            if (line[i] === "=") {
                                b.push(TokenType.Operator, "=");
                                i++;
                                continue;
                            }
                            // Attribute value (quoted string).
                            if (line[i] === '"' || line[i] === "'") {
                                const q = line[i];
                                let k = i + 1;
                                while (k < n && line[k] !== q)
                                    k++;
                                if (k < n)
                                    k++;
                                b.push(TokenType.AttrValue, line.substring(i, k));
                                i = k;
                                continue;
                            }
                            // Unknown char in tag.
                            b.push(TokenType.Plain, line[i]);
                            i++;
                        }
                        if (line[i] === ">") {
                            b.push(TokenType.XmlDelimiter, ">");
                            i++;
                        }
                        continue;
                    }
                    // Text content.
                    if (ch === "&") {
                        // Entity reference.
                        let j = i + 1;
                        while (j < n && line[j] !== ";" && /[A-Za-z0-9#]/.test(line[j]))
                            j++;
                        if (j < n && line[j] === ";")
                            j++;
                        b.push(TokenType.Constant, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Plain text content - read until next '<'.
                    let j = i;
                    while (j < n && line[j] !== "<")
                        j++;
                    b.push(TokenType.XmlText, line.substring(i, j));
                    i = j;
                }
                return { tokens: b.result, state };
            }
        }
        Highlighters.XmlHighlighter = XmlHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * Markdown syntax highlighter.
         * Handles headings, bold, italic, code, links, lists, blockquotes,
         * horizontal rules, and fenced code blocks (with state for multi-line).
         */
        class MarkdownHighlighter {
            constructor() {
                this.language = "markdown";
            }
            initialState() {
                return { inFence: false, fenceChar: "" };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Inside fenced code block.
                if (state.inFence) {
                    // Check for closing fence.
                    const fenceMatch = new RegExp("^\\s*" + state.fenceChar + "{3,}\\s*$").exec(line);
                    if (fenceMatch) {
                        b.push(TokenType.Code, line);
                        return { tokens: b.result, state: { inFence: false, fenceChar: "" } };
                    }
                    b.push(TokenType.Code, line);
                    return { tokens: b.result, state };
                }
                // Blank line.
                if (n === 0 || /^\s*$/.test(line)) {
                    b.push(TokenType.Plain, line);
                    return { tokens: b.result, state };
                }
                // Heading.
                const headingMatch = /^(#{1,6})\s+(.*)$/.exec(line);
                if (headingMatch) {
                    b.push(TokenType.Heading, headingMatch[1] + " ");
                    this.tokenizeInline(headingMatch[2], b);
                    return { tokens: b.result, state };
                }
                // Fenced code block start.
                const fenceStart = /^\s*(```|~~~)(.*)$/.exec(line);
                if (fenceStart) {
                    b.push(TokenType.Code, fenceStart[1]);
                    if (fenceStart[2]) {
                        b.push(TokenType.Property, fenceStart[2]);
                    }
                    return { tokens: b.result, state: { inFence: true, fenceChar: fenceStart[1][0] } };
                }
                // Horizontal rule.
                if (/^\s*([-*_])\1{2,}\s*$/.test(line)) {
                    b.push(TokenType.Operator, line);
                    return { tokens: b.result, state };
                }
                // Blockquote.
                const bqMatch = /^(\s*>)+\s?/.exec(line);
                if (bqMatch) {
                    b.push(TokenType.Quote, bqMatch[0]);
                    this.tokenizeInline(line.substring(bqMatch[0].length), b);
                    return { tokens: b.result, state };
                }
                // List item.
                const listMatch = /^(\s*)([-*+]|\d+\.)\s+/.exec(line);
                if (listMatch) {
                    b.push(TokenType.Plain, listMatch[1]);
                    b.push(TokenType.ListMarker, listMatch[2] + " ");
                    this.tokenizeInline(line.substring(listMatch[0].length), b);
                    return { tokens: b.result, state };
                }
                // Table row (contains |).
                if (line.indexOf("|") >= 0 && /\|/.test(line)) {
                    this.tokenizeInline(line, b);
                    return { tokens: b.result, state };
                }
                // Regular paragraph.
                this.tokenizeInline(line, b);
                return { tokens: b.result, state };
            }
            tokenizeInline(text, b) {
                let i = 0;
                const n = text.length;
                while (i < n) {
                    const ch = text[i];
                    // Inline code `code` or ``code``.
                    if (ch === "`") {
                        let ticks = 0;
                        while (text[i + ticks] === "`")
                            ticks++;
                        const fence = "`".repeat(ticks);
                        const endIdx = text.indexOf(fence, i + ticks);
                        if (endIdx >= 0) {
                            b.push(TokenType.Code, text.substring(i, endIdx + ticks));
                            i = endIdx + ticks;
                            continue;
                        }
                    }
                    // Bold **text** or __text__.
                    if ((text.substr(i, 2) === "**" || text.substr(i, 2) === "__")) {
                        const marker = text.substr(i, 2);
                        const endIdx = text.indexOf(marker, i + 2);
                        if (endIdx >= 0) {
                            b.push(TokenType.Bold, marker);
                            this.tokenizeInline(text.substring(i + 2, endIdx), b);
                            b.push(TokenType.Bold, marker);
                            i = endIdx + 2;
                            continue;
                        }
                    }
                    // Italic *text* or _text_.
                    if ((ch === "*" || ch === "_") && text[i + 1] !== ch) {
                        const endIdx = text.indexOf(ch, i + 1);
                        if (endIdx > i + 1) {
                            b.push(TokenType.Italic, ch);
                            this.tokenizeInline(text.substring(i + 1, endIdx), b);
                            b.push(TokenType.Italic, ch);
                            i = endIdx + 1;
                            continue;
                        }
                    }
                    // Link [text](url) or image ![alt](url).
                    if (ch === "[" || (ch === "!" && text[i + 1] === "[")) {
                        const imgPrefix = ch === "!" ? "!" : "";
                        const start = imgPrefix ? i : i;
                        const bracketEnd = text.indexOf("]", i + 1);
                        if (bracketEnd > 0 && text[bracketEnd + 1] === "(") {
                            const parenEnd = text.indexOf(")", bracketEnd + 2);
                            if (parenEnd > 0) {
                                if (imgPrefix) {
                                    b.push(TokenType.Operator, "!");
                                }
                                b.push(TokenType.Link, text.substring(start + (imgPrefix ? 1 : 0), bracketEnd + 1));
                                b.push(TokenType.Link, text.substring(bracketEnd + 1, parenEnd + 1));
                                i = parenEnd + 1;
                                continue;
                            }
                        }
                    }
                    // Reference link [text][ref].
                    if (ch === "[") {
                        const bracketEnd = text.indexOf("]", i + 1);
                        if (bracketEnd > 0 && text[bracketEnd + 1] === "[") {
                            const refEnd = text.indexOf("]", bracketEnd + 2);
                            if (refEnd > 0) {
                                b.push(TokenType.Link, text.substring(i, refEnd + 1));
                                i = refEnd + 1;
                                continue;
                            }
                        }
                    }
                    b.push(TokenType.Plain, ch);
                    i++;
                }
            }
        }
        Highlighters.MarkdownHighlighter = MarkdownHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * YAML syntax highlighter.
         * Handles keys, scalars (string, number, bool, null), comments,
         * block scalars (| and > with multi-line state), anchors and aliases,
         * flow sequences/mappings, and document markers (---, ...).
         */
        class YamlHighlighter {
            constructor() {
                this.language = "yaml";
            }
            initialState() {
                return { inBlockScalar: false, blockScalarIndent: -1, blockScalarChar: "" };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                const n = line.length;
                // Continue block scalar.
                if (state.inBlockScalar) {
                    // Check indentation: if line is blank or indented enough, it's part of scalar.
                    const indentMatch = /^(\s*)/.exec(line);
                    const indent = indentMatch ? indentMatch[1].length : 0;
                    if (n === 0 || indent >= state.blockScalarIndent) {
                        b.push(TokenType.String, line);
                        return { tokens: b.result, state };
                    }
                    // Otherwise, block scalar ended.
                    state = { inBlockScalar: false, blockScalarIndent: -1, blockScalarChar: "" };
                }
                // Document markers.
                if (/^(---|\.\.\.)\s*$/.test(line)) {
                    b.push(TokenType.Preprocessor, line);
                    return { tokens: b.result, state };
                }
                // Comment-only line.
                const commentOnlyMatch = /^(\s*)#(.*)$/.exec(line);
                if (commentOnlyMatch) {
                    b.push(TokenType.Plain, commentOnlyMatch[1]);
                    b.push(TokenType.Comment, "#" + commentOnlyMatch[2]);
                    return { tokens: b.result, state };
                }
                let i = 0;
                // Leading whitespace.
                const wsMatch = /^(\s*)/.exec(line);
                if (wsMatch && wsMatch[1].length > 0) {
                    b.push(TokenType.Plain, wsMatch[1]);
                    i = wsMatch[1].length;
                }
                // List item marker.
                const listMatch = /^(-\s+)/.exec(line.substr(i));
                if (listMatch) {
                    b.push(TokenType.Punctuation, listMatch[1]);
                    i += listMatch[1].length;
                }
                // Try to match key: value pattern.
                const rest = line.substr(i);
                const kvMatch = /^([A-Za-z_][A-Za-z0-9_\-\.]*|"[^"]*"|'[^']*')(\s*):(\s*)(.*)$/.exec(rest);
                if (kvMatch && !rest.startsWith("- ")) {
                    // Key.
                    if (kvMatch[1][0] === '"' || kvMatch[1][0] === "'") {
                        b.push(TokenType.Property, kvMatch[1]);
                    }
                    else {
                        b.push(TokenType.Property, kvMatch[1]);
                    }
                    b.push(TokenType.Plain, kvMatch[2]);
                    b.push(TokenType.Operator, ":");
                    b.push(TokenType.Plain, kvMatch[3]);
                    // Value.
                    this.tokenizeValue(kvMatch[4], b, state);
                    return { tokens: b.result, state };
                }
                // Just a value (e.g. list item value).
                this.tokenizeValue(rest, b, state);
                return { tokens: b.result, state };
            }
            tokenizeValue(text, b, state) {
                let i = 0;
                const n = text.length;
                // Trim trailing comment.
                let commentIdx = -1;
                let inStr = null;
                for (let k = 0; k < n; k++) {
                    const c = text[k];
                    if (inStr) {
                        if (c === inStr && text[k - 1] !== "\\")
                            inStr = null;
                        continue;
                    }
                    if (c === '"' || c === "'") {
                        inStr = c;
                        continue;
                    }
                    if (c === "#") {
                        // Comment must be preceded by whitespace or start of line.
                        if (k === 0 || /\s/.test(text[k - 1])) {
                            commentIdx = k;
                            break;
                        }
                    }
                }
                const valuePart = commentIdx >= 0 ? text.substring(0, commentIdx) : text;
                const commentPart = commentIdx >= 0 ? text.substring(commentIdx) : "";
                // Check for block scalar indicators.
                const blockScalarMatch = /^(\s*)([|>])([+\-]?)(\s*)$/.exec(valuePart);
                if (blockScalarMatch) {
                    b.push(TokenType.Plain, blockScalarMatch[1]);
                    b.push(TokenType.Operator, blockScalarMatch[2] + blockScalarMatch[3]);
                    b.push(TokenType.Plain, blockScalarMatch[4]);
                    // Determine indent for block scalar: use current line's leading indent + 1.
                    // We don't have access to it here cleanly, so use a heuristic: any non-empty line.
                    state.inBlockScalar = true;
                    state.blockScalarIndent = 0; // Will accept any indented line.
                    if (commentPart) {
                        b.push(TokenType.Comment, commentPart);
                    }
                    return;
                }
                this.tokenizeInlineValue(valuePart, b);
                if (commentPart) {
                    b.push(TokenType.Comment, commentPart);
                }
            }
            tokenizeInlineValue(text, b) {
                let i = 0;
                const n = text.length;
                while (i < n) {
                    const ch = text[i];
                    if (/\s/.test(ch)) {
                        let j = i;
                        while (j < n && /\s/.test(text[j]))
                            j++;
                        b.push(TokenType.Plain, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Double-quoted string.
                    if (ch === '"') {
                        let j = i + 1;
                        while (j < n) {
                            if (text[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (text[j] === '"') {
                                j++;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Single-quoted string.
                    if (ch === "'") {
                        let j = i + 1;
                        while (j < n) {
                            if (text[j] === "'" && text[j + 1] === "'") {
                                j += 2;
                                continue;
                            }
                            if (text[j] === "'") {
                                j++;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Anchor &name or Alias *name.
                    if (ch === "&" || ch === "*") {
                        let j = i + 1;
                        while (j < n && /[A-Za-z0-9_\-]/.test(text[j]))
                            j++;
                        b.push(TokenType.Annotation, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Tag !name.
                    if (ch === "!") {
                        let j = i + 1;
                        while (j < n && /[A-Za-z0-9_\-\/!]/.test(text[j]))
                            j++;
                        b.push(TokenType.Annotation, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Number.
                    if (/[0-9\-]/.test(ch) && (ch !== "-" || /[0-9.]/.test(text[i + 1] || ""))) {
                        let j = i;
                        if (text[j] === "-")
                            j++;
                        if (text[j] === "0" && (text[j + 1] === "x" || text[j + 1] === "X")) {
                            j += 2;
                            while (j < n && /[0-9A-Fa-f]/.test(text[j]))
                                j++;
                        }
                        else {
                            while (j < n && /[0-9.]/.test(text[j]))
                                j++;
                            if (j < n && (text[j] === "e" || text[j] === "E")) {
                                j++;
                                if (text[j] === "+" || text[j] === "-")
                                    j++;
                                while (j < n && /[0-9]/.test(text[j]))
                                    j++;
                            }
                        }
                        b.push(TokenType.Number, text.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Constants.
                    if (/[a-z]/.test(ch)) {
                        let j = i;
                        while (j < n && /[A-Za-z0-9_\-]/.test(text[j]))
                            j++;
                        const word = text.substring(i, j);
                        if (word === "true" || word === "false" || word === "null" ||
                            word === "yes" || word === "no" || word === "on" || word === "off" ||
                            word === "True" || word === "False" || word === "Null" ||
                            word === "Yes" || word === "No" || word === "On" || word === "Off" ||
                            word === "TRUE" || word === "FALSE" || word === "NULL" ||
                            word === "YES" || word === "NO" || word === "ON" || word === "OFF") {
                            b.push(TokenType.Constant, word);
                        }
                        else {
                            b.push(TokenType.Identifier, word);
                        }
                        i = j;
                        continue;
                    }
                    // Flow punctuation.
                    if (/[{}\[\],]/.test(ch)) {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    if (ch === ":") {
                        b.push(TokenType.Operator, ch);
                        i++;
                        continue;
                    }
                    b.push(TokenType.Plain, ch);
                    i++;
                }
            }
        }
        Highlighters.YamlHighlighter = YamlHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * JavaScript syntax highlighter with multi-line state tracking.
         *
         * Handles:
         *   - Block comments and JSDoc (multi-line)
         *   - Single-line comments
         *   - Single/double-quoted strings with escapes and \-line-continuation
         *   - Template literals with ${} interpolation (nested, multi-line)
         *   - Regex literals with division-ambiguity resolution
         *   - Numbers (decimal, hex, binary, octal, scientific, BigInt)
         *   - Keywords vs. control-flow keywords (separate colours)
         *   - Built-in global objects
         *   - Function call / definition name detection
         *   - Property access after a dot
         *   - Operators and punctuation
         *
         * The class is designed for inheritance: TypeScriptHighlighter extends it
         * and overrides the keyword/type sets and the language identifier.
         */
        class JavaScriptHighlighter {
            constructor() {
                this.language = "javascript";
            }
            initialState() {
                return {
                    inBlockComment: false,
                    inDocComment: false,
                    inString: false,
                    stringQuote: "",
                    inTemplate: false,
                    templateStack: [],
                    braceDepth: 0,
                    lastSignificant: "none"
                };
            }
            /** Sub-classes may override to extend the keyword set. */
            isControlKeyword(word) {
                return JavaScriptHighlighter.CONTROL_KEYWORDS.has(word);
            }
            isKeyword(word) {
                return JavaScriptHighlighter.KEYWORDS.has(word);
            }
            isValueKeyword(word) {
                return JavaScriptHighlighter.VALUE_KEYWORDS.has(word);
            }
            isBuiltin(word) {
                return JavaScriptHighlighter.BUILTINS.has(word);
            }
            /** Whether the word is a built-in type (overridden by TS). */
            isType(word) {
                return false;
            }
            /** Whether decorators (@name) should be parsed (enabled in TS). */
            parseDecorators() {
                return false;
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                let s = state ? state : this.initialState();
                // Always clone to avoid mutating the cached state object.
                s = { ...s, templateStack: s.templateStack.slice() };
                // ---- Continue multi-line block comment ----
                if (s.inBlockComment || s.inDocComment) {
                    const endIdx = line.indexOf("*/");
                    if (endIdx < 0) {
                        b.push(s.inDocComment ? TokenType.DocComment : TokenType.Comment, line);
                        return { tokens: b.result, state: s };
                    }
                    const close = endIdx + 2;
                    b.push(s.inDocComment ? TokenType.DocComment : TokenType.Comment, line.substring(0, close));
                    i = close;
                    s.inBlockComment = false;
                    s.inDocComment = false;
                }
                // ---- Continue multi-line string (\-continuation) ----
                if (s.inString) {
                    const q = s.stringQuote;
                    let j = 0;
                    while (j < n) {
                        if (line[j] === "\\" && j + 1 < n) {
                            j += 2;
                            continue;
                        }
                        if (line[j] === q) {
                            j++;
                            break;
                        }
                        j++;
                    }
                    b.push(TokenType.String, line.substring(0, j));
                    i = j;
                    if (j < n) {
                        s.inString = false;
                        s.stringQuote = "";
                        s.lastSignificant = "value";
                    }
                    else {
                        // Still inside string; carry state.
                        return { tokens: b.result, state: s };
                    }
                }
                // ---- Continue multi-line template literal text ----
                if (s.inTemplate) {
                    const res = this.scanTemplateText(line, i, n, s, b);
                    i = res.i;
                    s = res.state;
                    if (s.inTemplate) {
                        return { tokens: b.result, state: s };
                    }
                }
                // ---- Main scan loop ----
                while (i < n) {
                    const ch = line[i];
                    // --- Whitespace ---
                    if (ch === " " || ch === "\t" || ch === "\r") {
                        let j = i;
                        while (j < n && (line[j] === " " || line[j] === "\t" || line[j] === "\r"))
                            j++;
                        b.push(TokenType.Plain, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // --- Line comment ---
                    if (ch === "/" && line[i + 1] === "/") {
                        b.push(TokenType.Comment, line.substring(i));
                        i = n;
                        break;
                    }
                    // --- Block comment / JSDoc ---
                    if (ch === "/" && line[i + 1] === "*") {
                        const isDoc = line[i + 2] === "*" && line[i + 3] !== "/";
                        const endIdx = line.indexOf("*/", i + 2);
                        if (endIdx < 0) {
                            b.push(isDoc ? TokenType.DocComment : TokenType.Comment, line.substring(i));
                            s.inBlockComment = !isDoc;
                            s.inDocComment = isDoc;
                            return { tokens: b.result, state: s };
                        }
                        const close = endIdx + 2;
                        b.push(isDoc ? TokenType.DocComment : TokenType.Comment, line.substring(i, close));
                        i = close;
                        // Comments don't change lastSignificant.
                        continue;
                    }
                    // --- Decorator (@Component) — TS only ---
                    if (this.parseDecorators() && ch === "@") {
                        let j = i + 1;
                        while (j < n && /[A-Za-z0-9_]/.test(line[j]))
                            j++;
                        if (j > i + 1) {
                            b.push(TokenType.Decorator, line.substring(i, j));
                            i = j;
                            s.lastSignificant = "none";
                            continue;
                        }
                    }
                    // --- Regex literal (division-ambiguity resolution) ---
                    if (ch === "/" && s.lastSignificant !== "value") {
                        const regexEnd = this.scanRegex(line, i, n);
                        if (regexEnd > i) {
                            b.push(TokenType.Regex, line.substring(i, regexEnd));
                            i = regexEnd;
                            s.lastSignificant = "value";
                            continue;
                        }
                    }
                    // --- Single/double-quoted string ---
                    if (ch === "'" || ch === '"') {
                        const res = this.scanString(line, i, n, ch, b);
                        i = res.i;
                        if (res.continues) {
                            s.inString = true;
                            s.stringQuote = ch;
                            return { tokens: b.result, state: s };
                        }
                        s.lastSignificant = "value";
                        continue;
                    }
                    // --- Template literal ---
                    if (ch === "`") {
                        s.inTemplate = true;
                        i++; // consume opening backtick
                        b.push(TokenType.TemplateDelimiter, "`");
                        const res = this.scanTemplateText(line, i, n, s, b);
                        i = res.i;
                        s = res.state;
                        if (s.inTemplate) {
                            return { tokens: b.result, state: s };
                        }
                        continue;
                    }
                    // --- Number ---
                    if (this.isNumberStart(line, i, n)) {
                        const j = this.scanNumber(line, i, n);
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        s.lastSignificant = "value";
                        continue;
                    }
                    // --- Identifier / keyword ---
                    if (this.isIdentStart(ch)) {
                        let j = i;
                        while (j < n && this.isIdentPart(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        i = j;
                        if (this.isControlKeyword(word)) {
                            b.push(TokenType.ControlKeyword, word);
                            s.lastSignificant = "operator";
                        }
                        else if (this.isKeyword(word)) {
                            b.push(TokenType.Keyword, word);
                            s.lastSignificant = "operator";
                        }
                        else if (this.isValueKeyword(word)) {
                            b.push(TokenType.Constant, word);
                            s.lastSignificant = "value";
                        }
                        else if (this.isType(word)) {
                            b.push(TokenType.Type, word);
                            s.lastSignificant = "value";
                        }
                        else if (this.isBuiltin(word)) {
                            b.push(TokenType.Builtin, word);
                            s.lastSignificant = "value";
                        }
                        else {
                            // Check if this is a function call: identifier followed by (
                            let k = i;
                            while (k < n && (line[k] === " " || line[k] === "\t"))
                                k++;
                            if (line[k] === "(") {
                                b.push(TokenType.Function, word);
                            }
                            else if (k < n && line[k] === ".") {
                                // Property access — keep as identifier
                                b.push(TokenType.Identifier, word);
                            }
                            else {
                                // Capitalized identifier → heuristic type/class name
                                if (word.length > 0 && word[0] >= "A" && word[0] <= "Z") {
                                    b.push(TokenType.Type, word);
                                }
                                else {
                                    b.push(TokenType.Identifier, word);
                                }
                            }
                            s.lastSignificant = "value";
                        }
                        continue;
                    }
                    // --- Property access: .identifier ---
                    if (ch === ".") {
                        // Distinguish number-starting dot (handled above) from member access.
                        b.push(TokenType.Operator, ".");
                        i++;
                        s.lastSignificant = "operator";
                        continue;
                    }
                    // --- Brace tracking for template ${} closers ---
                    if (ch === "{") {
                        if (!s.inTemplate) {
                            s.braceDepth++;
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        s.lastSignificant = "operator";
                        continue;
                    }
                    if (ch === "}") {
                        if (s.templateStack.length > 0 && s.braceDepth === s.templateStack[s.templateStack.length - 1]) {
                            // Close a ${} interpolation → back to template text.
                            s.templateStack.pop();
                            b.push(TokenType.TemplateDelimiter, "}");
                            i++;
                            s.inTemplate = true;
                            s.lastSignificant = "none"; // template text, not a value
                            const res = this.scanTemplateText(line, i, n, s, b);
                            i = res.i;
                            s = res.state;
                            if (s.inTemplate) {
                                return { tokens: b.result, state: s };
                            }
                            continue;
                        }
                        if (!s.inTemplate) {
                            s.braceDepth--;
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        s.lastSignificant = "value"; // } can precede division: obj.x / 2
                        continue;
                    }
                    // --- Punctuation ---
                    if (ch === "(" || ch === ")" || ch === "[" || ch === "]" || ch === "," || ch === ";") {
                        if (ch === "(" || ch === "[") {
                            // These don't increment braceDepth (only {} does).
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        // ( and [ are operator-like (regex can follow).
                        // ) and ] are value-like (division can follow).
                        s.lastSignificant = (ch === ")" || ch === "]") ? "value" : "operator";
                        continue;
                    }
                    if (ch === ":") {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        s.lastSignificant = "operator";
                        continue;
                    }
                    // --- Operators ---
                    if (this.isOperatorChar(ch)) {
                        let j = i;
                        while (j < n && this.isOperatorChar(line[j]))
                            j++;
                        b.push(TokenType.Operator, line.substring(i, j));
                        i = j;
                        s.lastSignificant = "operator";
                        continue;
                    }
                    // --- Fallback: unknown character ---
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                return { tokens: b.result, state: s };
            }
            // ========== Helper methods ==========
            isIdentStart(ch) {
                return (ch >= "a" && ch <= "z") || (ch >= "A" && ch <= "Z") || ch === "_" || ch === "$";
            }
            isIdentPart(ch) {
                return this.isIdentStart(ch) || (ch >= "0" && ch <= "9");
            }
            isOperatorChar(ch) {
                return "+-*/%=<>!&|~^?".indexOf(ch) >= 0;
            }
            isNumberStart(line, i, n) {
                const ch = line[i];
                if (ch >= "0" && ch <= "9")
                    return true;
                if (ch === ".") {
                    // Only if followed by a digit (otherwise it's member access).
                    return i + 1 < n && line[i + 1] >= "0" && line[i + 1] <= "9";
                }
                return false;
            }
            scanNumber(line, i, n) {
                let j = i;
                // Hex / binary / octal prefix.
                if (line[i] === "0" && i + 1 < n) {
                    const p = line[i + 1];
                    if (p === "x" || p === "X") {
                        j = i + 2;
                        while (j < n && /[0-9A-Fa-f_]/.test(line[j]))
                            j++;
                        if (j < n && line[j] === "n")
                            j++; // BigInt
                        return j;
                    }
                    if (p === "b" || p === "B") {
                        j = i + 2;
                        while (j < n && /[01_]/.test(line[j]))
                            j++;
                        if (j < n && line[j] === "n")
                            j++;
                        return j;
                    }
                    if (p === "o" || p === "O") {
                        j = i + 2;
                        while (j < n && /[0-7_]/.test(line[j]))
                            j++;
                        if (j < n && line[j] === "n")
                            j++;
                        return j;
                    }
                }
                // Decimal / float.
                while (j < n && /[0-9_]/.test(line[j]))
                    j++;
                if (j < n && line[j] === ".") {
                    j++;
                    while (j < n && /[0-9_]/.test(line[j]))
                        j++;
                }
                // Exponent.
                if (j < n && (line[j] === "e" || line[j] === "E")) {
                    j++;
                    if (j < n && (line[j] === "+" || line[j] === "-"))
                        j++;
                    while (j < n && /[0-9_]/.test(line[j]))
                        j++;
                }
                // BigInt suffix.
                if (j < n && line[j] === "n")
                    j++;
                return j;
            }
            /**
             * Scan a regex literal starting at `i` (the opening `/`).
             * Returns the index past the closing `/` (and any flags), or `i`
             * if this doesn't look like a regex.
             */
            scanRegex(line, i, n) {
                let j = i + 1; // skip opening /
                let inClass = false;
                while (j < n) {
                    const c = line[j];
                    if (c === "\\" && j + 1 < n) {
                        j += 2;
                        continue;
                    }
                    if (c === "[") {
                        inClass = true;
                        j++;
                        continue;
                    }
                    if (c === "]" && inClass) {
                        inClass = false;
                        j++;
                        continue;
                    }
                    if (c === "/" && !inClass) {
                        j++;
                        // Consume flags.
                        while (j < n && /[gimsuyd]/.test(line[j]))
                            j++;
                        return j;
                    }
                    j++;
                }
                // Unterminated regex — consume to end of line.
                return n;
            }
            /**
             * Scan a single/double-quoted string starting at `i` (the quote).
             * Pushes the string token and returns the new index. If the string
             * is unterminated at end of line (with a trailing \), sets `continues`.
             */
            scanString(line, i, n, quote, b) {
                let j = i + 1;
                while (j < n) {
                    if (line[j] === "\\" && j + 1 < n) {
                        j += 2;
                        continue;
                    }
                    if (line[j] === quote) {
                        j++;
                        b.push(TokenType.String, line.substring(i, j));
                        return { i: j, continues: false };
                    }
                    j++;
                }
                // Reached end of line without closing quote.
                // Check for \-continuation (last char is \).
                b.push(TokenType.String, line.substring(i, j));
                return { i: j, continues: true };
            }
            /**
             * Scan template literal text (the portion between `${}` segments or
             * after the opening backtick). Exits when it encounters `${` (entering
             * an expression) or a closing backtick. Pushes template text as a
             * single TemplateString token and delimiters as TemplateDelimiter.
             *
             * Returns the updated index and state.
             */
            scanTemplateText(line, i, n, s, b) {
                let start = i;
                while (i < n) {
                    const ch = line[i];
                    // Escape sequence inside template.
                    if (ch === "\\" && i + 1 < n) {
                        i += 2;
                        continue;
                    }
                    // ${ → start interpolation expression.
                    if (ch === "$" && line[i + 1] === "{") {
                        // Flush accumulated template text.
                        if (i > start) {
                            b.push(TokenType.TemplateString, line.substring(start, i));
                        }
                        b.push(TokenType.TemplateDelimiter, "${");
                        i += 2;
                        // Enter expression mode: push current braceDepth, the }
                        // at this depth will close the interpolation.
                        s.templateStack.push(s.braceDepth);
                        s.inTemplate = false;
                        s.lastSignificant = "operator";
                        return { i, state: s };
                    }
                    // Closing backtick.
                    if (ch === "`") {
                        if (i > start) {
                            b.push(TokenType.TemplateString, line.substring(start, i));
                        }
                        b.push(TokenType.TemplateDelimiter, "`");
                        i++;
                        s.inTemplate = false;
                        s.lastSignificant = "value";
                        return { i, state: s };
                    }
                    i++;
                }
                // End of line while still in template text.
                if (i > start) {
                    b.push(TokenType.TemplateString, line.substring(start, i));
                }
                // s.inTemplate remains true → carry to next line.
                return { i, state: s };
            }
        }
        JavaScriptHighlighter.CONTROL_KEYWORDS = new Set([
            "if", "else", "for", "while", "do", "switch", "case", "break",
            "continue", "return", "throw", "try", "catch", "finally",
            "yield", "await", "default"
        ]);
        JavaScriptHighlighter.KEYWORDS = new Set([
            "var", "let", "const", "function", "class", "extends", "new",
            "delete", "typeof", "instanceof", "in", "of", "void", "import",
            "export", "from", "as", "static", "get", "set", "async", "with",
            "debugger"
        ]);
        /** Keywords that behave syntactically like values. */
        JavaScriptHighlighter.VALUE_KEYWORDS = new Set([
            "true", "false", "null", "undefined", "this", "super", "NaN",
            "Infinity"
        ]);
        JavaScriptHighlighter.BUILTINS = new Set([
            "console", "Math", "JSON", "Object", "Array", "String", "Number",
            "Boolean", "Symbol", "Date", "RegExp", "Error", "Promise", "Map",
            "Set", "WeakMap", "WeakSet", "Proxy", "Reflect", "window",
            "document", "globalThis", "parseInt", "parseFloat", "isNaN",
            "isFinite", "setTimeout", "setInterval", "clearTimeout",
            "clearInterval", "encodeURIComponent", "decodeURIComponent",
            "encodeURI", "decodeURI", "Buffer", "process", "module",
            "require", "exports", "undefined", "BigInt", "Symbol",
            "FinalizationRegistry", "WeakRef", "AggregateError",
            "Intl", "DataView", "Float32Array", "Float64Array", "Int8Array",
            "Int16Array", "Int32Array", "Uint8Array", "Uint8ClampedArray",
            "Uint16Array", "Uint32Array", "BigInt64Array", "BigUint64Array",
            "ArrayBuffer", "SharedArrayBuffer", "Atomics", "queueMicrotask",
            "structuredClone", "fetch", "Request", "Response", "Headers",
            "URL", "URLSearchParams", "TextEncoder", "TextDecoder",
            "crypto", "performance", "Event", "EventTarget", "CustomEvent",
            "Worker", "MessageChannel", "BroadcastChannel"
        ]);
        Highlighters.JavaScriptHighlighter = JavaScriptHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        /**
         * TypeScript syntax highlighter.
         *
         * Extends {@link JavaScriptHighlighter} by overriding the keyword and
         * type sets to inject TypeScript-specific vocabulary, and enabling
         * decorator parsing. No scan logic is duplicated.
         *
         * Additions over JavaScript:
         *   - Type-system keywords (interface, type, enum, implements, declare,
         *     namespace, readonly, abstract, public, private, protected,
         *     satisfies, asserts, keyof, infer, is, module, override, out)
         *   - Built-in primitive types (string, number, boolean, any, unknown,
         *     never, void, object, symbol, bigint)
         *   - Decorators (@Component, @Injectable, …)
         */
        class TypeScriptHighlighter extends Highlighters.JavaScriptHighlighter {
            constructor() {
                super(...arguments);
                this.language = "typescript";
            }
            isKeyword(word) {
                return super.isKeyword(word) || TypeScriptHighlighter.TS_EXTRA_KEYWORDS.has(word);
            }
            isType(word) {
                return TypeScriptHighlighter.TS_TYPES.has(word);
            }
            parseDecorators() {
                return true;
            }
        }
        TypeScriptHighlighter.TS_EXTRA_KEYWORDS = new Set([
            "interface", "type", "enum", "implements", "declare", "namespace",
            "readonly", "abstract", "public", "private", "protected",
            "satisfies", "asserts", "keyof", "infer", "is", "module",
            "override", "global", "unique"
        ]);
        TypeScriptHighlighter.TS_TYPES = new Set([
            "string", "number", "boolean", "any", "unknown", "never", "void",
            "object", "symbol", "bigint", "String", "Number", "Boolean",
            "Symbol", "BigInt", "Object", "Array", "ReadonlyArray", "Map",
            "Set", "ReadonlyMap", "ReadonlySet", "Promise", "Date", "RegExp",
            "Error", "Record", "Partial", "Required", "Readonly", "Pick",
            "Omit", "Exclude", "Extract", "NonNullable", "Parameters",
            "ConstructorParameters", "ReturnType", "InstanceType",
            "ThisType", "Awaited", "Lowercase", "Uppercase", "Capitalize",
            "Uncapitalize", "Function", "Error"
        ]);
        Highlighters.TypeScriptHighlighter = TypeScriptHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * CSS syntax highlighter with multi-line state tracking.
         *
         * Handles:
         *   - Block comments (multi-line)
         *   - At-rules (@media, @import, @keyframes, @supports, @font-face, etc.)
         *   - Selectors: element, .class, #id, :pseudo-class, ::pseudo-element,
         *     [attr], combinators (>, +, ~, space)
         *   - Property names and values (context-sensitive colouring)
         *   - Colour values (#hex, named colours)
         *   - Numbers with units (px, em, %, etc.)
         *   - Strings (single and double-quoted)
         *   - CSS custom properties (--var-name)
         *   - Functions: rgb(), var(), calc(), url(), etc.
         *   - !important
         */
        class CssHighlighter {
            constructor() {
                this.language = "css";
            }
            initialState() {
                return {
                    inBlockComment: false,
                    context: "selector",
                    depth: 0,
                    inAtRuleParens: false
                };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                let s = state ? state : this.initialState();
                s = { ...s };
                // ---- Continue multi-line block comment ----
                if (s.inBlockComment) {
                    const endIdx = line.indexOf("*/");
                    if (endIdx < 0) {
                        b.push(TokenType.Comment, line);
                        return { tokens: b.result, state: s };
                    }
                    const close = endIdx + 2;
                    b.push(TokenType.Comment, line.substring(0, close));
                    i = close;
                    s.inBlockComment = false;
                }
                while (i < n) {
                    const ch = line[i];
                    // --- Whitespace ---
                    if (ch === " " || ch === "\t" || ch === "\r") {
                        let j = i;
                        while (j < n && (line[j] === " " || line[j] === "\t" || line[j] === "\r"))
                            j++;
                        b.push(TokenType.Plain, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // --- Block comment ---
                    if (ch === "/" && line[i + 1] === "*") {
                        const endIdx = line.indexOf("*/", i + 2);
                        if (endIdx < 0) {
                            b.push(TokenType.Comment, line.substring(i));
                            s.inBlockComment = true;
                            return { tokens: b.result, state: s };
                        }
                        const close = endIdx + 2;
                        b.push(TokenType.Comment, line.substring(i, close));
                        i = close;
                        continue;
                    }
                    // --- !important ---
                    if (ch === "!" && line.substr(i, 10).toLowerCase() === "!important") {
                        b.push(TokenType.Keyword, line.substring(i, i + 10));
                        i += 10;
                        continue;
                    }
                    // --- { : enter declaration block ---
                    if (ch === "{") {
                        s.depth++;
                        s.context = "property";
                        s.inAtRuleParens = false;
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- } : leave declaration block ---
                    if (ch === "}") {
                        if (s.depth > 0)
                            s.depth--;
                        s.context = s.depth === 0 ? "selector" : "property";
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- ; : end declaration ---
                    if (ch === ";") {
                        if (!s.inAtRuleParens) {
                            s.context = s.depth === 0 ? "selector" : "property";
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- : : switch from property to value context ---
                    if (ch === ":") {
                        if (s.context === "property" && !s.inAtRuleParens) {
                            s.context = "value";
                            b.push(TokenType.Punctuation, ch);
                            i++;
                            continue;
                        }
                        // In selector context: pseudo-class/element (handled below via identifier scan).
                        // But we reach here only if the identifier scan didn't catch it.
                        // Fall through to pseudo handling.
                        let j = i + 1;
                        let isPseudoElement = false;
                        if (line[j] === ":") {
                            isPseudoElement = true;
                            j++;
                        }
                        while (j < n && /[A-Za-z0-9_-]/.test(line[j]))
                            j++;
                        if (j > i + 1) {
                            b.push(TokenType.PseudoClass, line.substring(i, j));
                            i = j;
                            continue;
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- ( ) ---
                    if (ch === "(" || ch === ")") {
                        if (ch === "(" && s.context === "selector") {
                            s.inAtRuleParens = true;
                        }
                        if (ch === ")" && s.inAtRuleParens) {
                            s.inAtRuleParens = false;
                        }
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- @ at-rule ---
                    if (ch === "@") {
                        let j = i + 1;
                        while (j < n && /[A-Za-z0-9_-]/.test(line[j]))
                            j++;
                        b.push(TokenType.AtRule, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // --- String ---
                    if (ch === "'" || ch === '"') {
                        const q = ch;
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === "\\" && j + 1 < n) {
                                j += 2;
                                continue;
                            }
                            if (line[j] === q) {
                                j++;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // --- # hex colour or ID selector ---
                    if (ch === "#") {
                        let j = i + 1;
                        // Try hex colour first: # followed by hex digits (3, 4, 6, or 8)
                        while (j < n && /[0-9A-Fa-f]/.test(line[j]))
                            j++;
                        if (j > i + 1 && (j - i - 1) <= 8) {
                            // Looks like a hex colour.
                            b.push(TokenType.ColorValue, line.substring(i, j));
                            i = j;
                            continue;
                        }
                        // ID selector: #idName
                        j = i + 1;
                        while (j < n && /[A-Za-z0-9_-]/.test(line[j]))
                            j++;
                        if (j > i + 1) {
                            b.push(TokenType.Selector, line.substring(i, j));
                            i = j;
                            continue;
                        }
                        b.push(TokenType.Operator, "#");
                        i++;
                        continue;
                    }
                    // --- Number + unit ---
                    if (this.isNumberStart(ch, line, i, n)) {
                        let j = this.scanNumber(line, i, n);
                        // Check for unit suffix.
                        let u = j;
                        while (u < n && /[A-Za-z%]/.test(line[u]))
                            u++;
                        const unit = line.substring(j, u);
                        if (unit.length > 0 && CssHighlighter.UNITS.has(unit.toLowerCase())) {
                            b.push(TokenType.Number, line.substring(i, j));
                            b.push(TokenType.Unit, unit);
                            i = u;
                        }
                        else if (unit.length > 0) {
                            b.push(TokenType.Number, line.substring(i, j));
                            b.push(TokenType.Identifier, unit);
                            i = u;
                        }
                        else {
                            b.push(TokenType.Number, line.substring(i, j));
                            i = j;
                        }
                        continue;
                    }
                    // --- Identifier / keyword / property / function ---
                    if (/[A-Za-z_\-]/.test(ch)) {
                        let j = i;
                        while (j < n && /[A-Za-z0-9_\-]/.test(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        const wordLower = word.toLowerCase();
                        i = j;
                        // Check if followed by '(' → function call.
                        let k = i;
                        while (k < n && (line[k] === " " || line[k] === "\t"))
                            k++;
                        const isFunction = line[k] === "(";
                        if (s.context === "selector" || s.inAtRuleParens) {
                            // Selector context: element type selector or at-rule keyword.
                            b.push(TokenType.Selector, word);
                        }
                        else if (s.context === "property") {
                            // Property name or CSS custom property.
                            if (word.startsWith("--")) {
                                b.push(TokenType.Variable, word);
                            }
                            else {
                                b.push(TokenType.Property, word);
                            }
                        }
                        else if (s.context === "value") {
                            // Property value context.
                            if (isFunction) {
                                b.push(TokenType.Function, word);
                            }
                            else if (CssHighlighter.NAMED_COLORS.has(wordLower)) {
                                b.push(TokenType.ColorValue, word);
                            }
                            else {
                                b.push(TokenType.Identifier, word);
                            }
                        }
                        else {
                            b.push(TokenType.Identifier, word);
                        }
                        continue;
                    }
                    // --- . class selector ---
                    if (ch === ".") {
                        let j = i + 1;
                        while (j < n && /[A-Za-z0-9_-]/.test(line[j]))
                            j++;
                        if (j > i + 1) {
                            b.push(TokenType.Selector, line.substring(i, j));
                            i = j;
                        }
                        else {
                            b.push(TokenType.Operator, ".");
                            i++;
                        }
                        continue;
                    }
                    // --- [ attribute selector ] ---
                    if (ch === "[") {
                        let j = i + 1;
                        while (j < n && line[j] !== "]")
                            j++;
                        if (j < n)
                            j++;
                        b.push(TokenType.Selector, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // --- Combinators and operators ---
                    if (ch === ">" || ch === "+" || ch === "~" || ch === "=" ||
                        ch === "*" || ch === "|" || ch === "^" || ch === "$") {
                        b.push(TokenType.Operator, ch);
                        i++;
                        continue;
                    }
                    // --- Comma ---
                    if (ch === ",") {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // --- Fallback ---
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                return { tokens: b.result, state: s };
            }
            // ========== Helpers ==========
            isNumberStart(ch, line, i, n) {
                if (ch >= "0" && ch <= "9")
                    return true;
                if (ch === "-" || ch === "+") {
                    // Negative/positive number: only if followed by digit or dot.
                    if (i + 1 < n) {
                        const next = line[i + 1];
                        if (next >= "0" && next <= "9")
                            return true;
                        if (next === "." && i + 2 < n && line[i + 2] >= "0" && line[i + 2] <= "9")
                            return true;
                    }
                }
                if (ch === ".") {
                    return i + 1 < n && line[i + 1] >= "0" && line[i + 1] <= "9";
                }
                return false;
            }
            scanNumber(line, i, n) {
                let j = i;
                // Optional sign.
                if (line[j] === "-" || line[j] === "+")
                    j++;
                // Integer part.
                while (j < n && line[j] >= "0" && line[j] <= "9")
                    j++;
                // Fractional part.
                if (j < n && line[j] === ".") {
                    j++;
                    while (j < n && line[j] >= "0" && line[j] <= "9")
                        j++;
                }
                // Exponent.
                if (j < n && (line[j] === "e" || line[j] === "E")) {
                    j++;
                    if (j < n && (line[j] === "+" || line[j] === "-"))
                        j++;
                    while (j < n && line[j] >= "0" && line[j] <= "9")
                        j++;
                }
                return j;
            }
        }
        CssHighlighter.AT_RULES = new Set([
            "media", "import", "keyframes", "supports", "font-face",
            "charset", "namespace", "page", "font-feature-values",
            "counter-style", "property", "layer", "container", "scope",
            "starting-style", "viewport", "document"
        ]);
        CssHighlighter.NAMED_COLORS = new Set([
            "transparent", "currentcolor", "black", "white", "red", "green",
            "blue", "yellow", "cyan", "magenta", "gray", "grey", "silver",
            "maroon", "olive", "lime", "aqua", "teal", "navy", "purple",
            "orange", "aliceblue", "antiquewhite", "aquamarine", "azure",
            "beige", "bisque", "blanchedalmond", "blueviolet", "brown",
            "burlywood", "cadetblue", "chartreuse", "chocolate", "coral",
            "cornflowerblue", "cornsilk", "crimson", "darkblue", "darkcyan",
            "darkgoldenrod", "darkgray", "darkgreen", "darkgrey",
            "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange",
            "darkorchid", "darkred", "darksalmon", "darkseagreen",
            "darkslateblue", "darkslategray", "darkslategrey",
            "darkturquoise", "darkviolet", "deeppink", "deepskyblue",
            "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite",
            "forestgreen", "gainsboro", "ghostwhite", "gold", "goldenrod",
            "greenyellow", "honeydew", "hotpink", "indianred", "indigo",
            "ivory", "khaki", "lavender", "lavenderblush", "lawngreen",
            "lemonchiffon", "lightblue", "lightcoral", "lightcyan",
            "lightgoldenrodyellow", "lightgray", "lightgreen", "lightgrey",
            "lightpink", "lightsalmon", "lightseagreen", "lightskyblue",
            "lightslategray", "lightslategrey", "lightsteelblue",
            "lightyellow", "limegreen", "linen", "mediumaquamarine",
            "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen",
            "mediumslateblue", "mediumspringgreen", "mediumturquoise",
            "mediumvioletred", "midnightblue", "mintcream", "mistyrose",
            "moccasin", "navajowhite", "oldlace", "olivedrab", "orangered",
            "orchid", "palegoldenrod", "palegreen", "paleturquoise",
            "palevioletred", "papayawhip", "peachpuff", "peru", "pink",
            "plum", "powderblue", "rebeccapurple", "rosybrown", "royalblue",
            "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell",
            "sienna", "skyblue", "slateblue", "slategray", "slategrey",
            "snow", "springgreen", "steelblue", "tan", "thistle", "tomato",
            "turquoise", "violet", "wheat", "whitesmoke", "yellowgreen"
        ]);
        CssHighlighter.UNITS = new Set([
            "px", "em", "rem", "ex", "ch", "vw", "vh", "vmin", "vmax",
            "%", "cm", "mm", "in", "pt", "pc", "q", "fr", "deg", "grad",
            "rad", "turn", "s", "ms", "hz", "khz", "dpi", "dpcm", "dppx",
            "x", "vi", "vb", "ic", "rlh", "lh", "cap", "rcap", "rch", "ric",
            "rex", "svh", "svw", "svmin", "svmax", "lvh", "lvw", "lvmin",
            "lvmax", "dvh", "dvw", "dvmin", "dvmax"
        ]);
        Highlighters.CssHighlighter = CssHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = CodeEditor.Utils.TokenType;
        var TokenBuilder = CodeEditor.Utils.TokenBuilder;
        /**
         * HTML syntax highlighter with embedded-language delegation.
         *
         * Handles:
         *   - DOCTYPE declaration
         *   - HTML comments (multi-line)
         *   - Tag names, attribute names, attribute values
         *   - Entity references (&amp; &lt; etc.)
         *   - Text content
         *   - &lt;script&gt; blocks: delegated to {@link JavaScriptHighlighter}
         *   - &lt;style&gt; blocks: delegated to {@link CssHighlighter}
         *
         * The sub-highlighters are instantiated once in the constructor and reused
         * for every line. Their state is stored in `HtmlState.subState` and carried
         * across lines, enabling multi-line constructs (block comments, template
         * literals, etc.) inside embedded script/style to work correctly.
         */
        class HtmlHighlighter {
            constructor() {
                this.language = "html";
                this.jsHighlighter = new Highlighters.JavaScriptHighlighter();
                this.cssHighlighter = new Highlighters.CssHighlighter();
            }
            initialState() {
                return {
                    mode: "html",
                    inComment: false,
                    subState: null
                };
            }
            tokenizeLine(line, state) {
                let s = state ? state : this.initialState();
                s = { ...s, subState: s.subState };
                if (s.mode === "script") {
                    return this.tokenizeEmbedded(line, s, "script", "</script>", this.jsHighlighter);
                }
                if (s.mode === "style") {
                    return this.tokenizeEmbedded(line, s, "style", "</style>", this.cssHighlighter);
                }
                // mode === "html"
                return this.tokenizeHtml(line, s);
            }
            // ========== HTML mode ==========
            tokenizeHtml(line, s) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // ---- Continue multi-line comment ----
                if (s.inComment) {
                    const endIdx = line.indexOf("-->");
                    if (endIdx < 0) {
                        b.push(TokenType.Comment, line);
                        return { tokens: b.result, state: s };
                    }
                    const close = endIdx + 3;
                    b.push(TokenType.Comment, line.substring(0, close));
                    i = close;
                    s.inComment = false;
                }
                while (i < n) {
                    const ch = line[i];
                    // --- Comment start <!-- ---
                    if (line.substr(i, 4) === "<!--") {
                        const endIdx = line.indexOf("-->", i + 4);
                        if (endIdx < 0) {
                            b.push(TokenType.Comment, line.substring(i));
                            s.inComment = true;
                            return { tokens: b.result, state: s };
                        }
                        const close = endIdx + 3;
                        b.push(TokenType.Comment, line.substring(i, close));
                        i = close;
                        continue;
                    }
                    // --- DOCTYPE ---
                    if (line.substr(i, 9).toUpperCase() === "<!DOCTYPE" ||
                        line.substr(i, 2) === "<!") {
                        const endIdx = line.indexOf(">", i);
                        if (endIdx < 0) {
                            b.push(TokenType.Preprocessor, line.substring(i));
                            i = n;
                            break;
                        }
                        b.push(TokenType.Preprocessor, line.substring(i, endIdx + 1));
                        i = endIdx + 1;
                        continue;
                    }
                    // --- Tag start < ---
                    if (ch === "<") {
                        const tagResult = this.parseTag(line, i, n, b);
                        i = tagResult.i;
                        // Check if this tag switches mode to script or style.
                        if (tagResult.tagName && tagResult.isOpening && !tagResult.selfClosed) {
                            const tagNameLower = tagResult.tagName.toLowerCase();
                            if (tagNameLower === "script") {
                                s.mode = "script";
                                s.subState = this.jsHighlighter.initialState();
                                // Check if there's content on this line after the tag.
                                if (i < n) {
                                    const embedded = this.tokenizeEmbedded(line.substring(i), s, "script", "</script>", this.jsHighlighter, i);
                                    return { tokens: b.result.concat(embedded.tokens), state: embedded.state };
                                }
                                return { tokens: b.result, state: s };
                            }
                            if (tagNameLower === "style") {
                                s.mode = "style";
                                s.subState = this.cssHighlighter.initialState();
                                if (i < n) {
                                    const embedded = this.tokenizeEmbedded(line.substring(i), s, "style", "</style>", this.cssHighlighter, i);
                                    return { tokens: b.result.concat(embedded.tokens), state: embedded.state };
                                }
                                return { tokens: b.result, state: s };
                            }
                        }
                        continue;
                    }
                    // --- Entity reference &name; ---
                    if (ch === "&") {
                        let j = i + 1;
                        while (j < n && line[j] !== ";" && /[A-Za-z0-9#]/.test(line[j]))
                            j++;
                        if (j < n && line[j] === ";") {
                            j++;
                            b.push(TokenType.Constant, line.substring(i, j));
                            i = j;
                            continue;
                        }
                    }
                    // --- Text content (read until next < or &) ---
                    {
                        let j = i;
                        while (j < n && line[j] !== "<" && line[j] !== "&")
                            j++;
                        if (j > i) {
                            b.push(TokenType.XmlText, line.substring(i, j));
                            i = j;
                            continue;
                        }
                        // j === i, meaning line[i] is '<' or '&' — but we already
                        // handled those above. Fall through to avoid infinite loop.
                        b.push(TokenType.XmlText, line[i]);
                        i++;
                    }
                }
                return { tokens: b.result, state: s };
            }
            /**
             * Parse an HTML tag starting at `i` (the '<').
             * Pushes delimiter, tag name, attributes, and closing '>' tokens.
             * Returns the index past the tag and the tag name (lowercased).
             */
            parseTag(line, i, n, b) {
                let j = i + 1;
                let isOpening = true;
                let selfClosed = false;
                // </ for closing tag.
                if (line[j] === "/") {
                    isOpening = false;
                    j++;
                }
                // Emit '<' or '</' delimiter.
                const delimLen = isOpening ? 1 : 2;
                b.push(TokenType.XmlDelimiter, line.substring(i, i + delimLen));
                // Tag name.
                let nameStart = j;
                while (j < n && /[A-Za-z0-9_:\-.]/.test(line[j]))
                    j++;
                const tagName = line.substring(nameStart, j);
                if (tagName.length > 0) {
                    b.push(TokenType.Tag, tagName);
                }
                // Parse attributes until '>' or '/>'.
                while (j < n && line[j] !== ">") {
                    // Whitespace.
                    if (line[j] === " " || line[j] === "\t" || line[j] === "\r" || line[j] === "\n") {
                        let k = j;
                        while (k < n && /\s/.test(line[k]))
                            k++;
                        b.push(TokenType.Plain, line.substring(j, k));
                        j = k;
                        continue;
                    }
                    // Self-close />.
                    if (line[j] === "/" && line[j + 1] === ">") {
                        b.push(TokenType.XmlDelimiter, "/>");
                        selfClosed = true;
                        j += 2;
                        break;
                    }
                    // Attribute name.
                    if (/[A-Za-z_:@\-]/.test(line[j])) {
                        let k = j;
                        while (k < n && /[A-Za-z0-9_:\-.]/.test(line[k]))
                            k++;
                        b.push(TokenType.AttrName, line.substring(j, k));
                        j = k;
                        continue;
                    }
                    // '='.
                    if (line[j] === "=") {
                        b.push(TokenType.Operator, "=");
                        j++;
                        continue;
                    }
                    // Attribute value (quoted).
                    if (line[j] === '"' || line[j] === "'") {
                        const q = line[j];
                        let k = j + 1;
                        while (k < n && line[k] !== q)
                            k++;
                        if (k < n)
                            k++;
                        b.push(TokenType.AttrValue, line.substring(j, k));
                        j = k;
                        continue;
                    }
                    // Unknown char inside tag.
                    b.push(TokenType.Plain, line[j]);
                    j++;
                }
                if (j < n && line[j] === ">") {
                    b.push(TokenType.XmlDelimiter, ">");
                    j++;
                }
                return { i: j, tagName, isOpening, selfClosed };
            }
            // ========== Embedded mode (script/style) ==========
            /**
             * Tokenize a line that is (at least partially) inside a &lt;script&gt; or
             * &lt;style&gt; block.
             *
             * The closing tag (e.g. &lt;/script&gt;) is searched for case-insensitively.
             * If found, the text before it is delegated to the sub-highlighter (with
             * an offset if this is a continuation), and the closing tag plus any
             * remaining text is parsed as HTML.
             *
             * @param line      The full line text.
             * @param s         The current HTML state (mode = script|style).
             * @param mode      "script" or "style".
             * @param closeTag  The closing tag literal, e.g. "</script>".
             * @param sub       The sub-highlighter instance.
             * @param offset    If this call is for a substring of the original line,
             *                  the start offset of that substring. Defaults to 0.
             */
            tokenizeEmbedded(line, s, mode, closeTag, sub, offset = 0) {
                const b = new TokenBuilder();
                const n = line.length;
                const closeTagLower = closeTag.toLowerCase();
                // Search for the closing tag (case-insensitive).
                let closeIdx = -1;
                for (let k = 0; k <= n - closeTag.length; k++) {
                    if (line.substr(k, closeTag.length).toLowerCase() === closeTagLower) {
                        closeIdx = k;
                        break;
                    }
                }
                if (closeIdx < 0) {
                    // Entire line is embedded content.
                    const subResult = sub.tokenizeLine(line, s.subState);
                    // Offset all tokens.
                    const tokens = this.offsetTokens(subResult.tokens, offset);
                    s.subState = subResult.state;
                    // mode stays the same.
                    return { tokens, state: s };
                }
                // There's a closing tag on this line.
                // Delegate the fragment before the closing tag.
                const fragment = line.substring(0, closeIdx);
                if (fragment.length > 0) {
                    const subResult = sub.tokenizeLine(fragment, s.subState);
                    const tokens = this.offsetTokens(subResult.tokens, offset);
                    for (const t of tokens) {
                        b.push(t.type, t.value);
                    }
                    s.subState = subResult.state;
                }
                // Switch back to HTML mode.
                s.mode = "html";
                s.subState = null;
                // Parse the closing tag and remaining HTML.
                const remaining = line.substring(closeIdx);
                const htmlResult = this.tokenizeHtml(remaining, { ...s, mode: "html" });
                // Offset the HTML tokens by closeIdx + offset.
                const htmlTokens = this.offsetTokens(htmlResult.tokens, closeIdx + offset);
                for (const t of htmlTokens) {
                    b.push(t.type, t.value);
                }
                return { tokens: b.result, state: htmlResult.state };
            }
            /**
             * Add `delta` to every token's start and end offsets.
             * Used when a sub-highlighter processes a fragment of a line.
             */
            offsetTokens(tokens, delta) {
                if (delta === 0)
                    return tokens;
                return tokens.map(t => ({
                    type: t.type,
                    value: t.value,
                    start: t.start + delta,
                    end: t.end + delta
                }));
            }
        }
        Highlighters.HtmlHighlighter = HtmlHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        /**
         * Registry that maps language identifiers and file extensions to
         * highlighter instances.
         */
        class HighlighterRegistry {
            static register(highlighter, extensions) {
                this.byLanguage.set(highlighter.language, highlighter);
                for (const ext of extensions) {
                    this.byExtension.set(ext.toLowerCase(), highlighter.language);
                }
            }
            static get(language) {
                return this.byLanguage.get(language) || null;
            }
            static getByExtension(ext) {
                const lang = this.byExtension.get(ext.toLowerCase());
                if (!lang)
                    return null;
                return this.get(lang);
            }
            static getLanguageForExtension(ext) {
                return this.byExtension.get(ext.toLowerCase()) || null;
            }
            static detectFromFilename(filename) {
                const dotIdx = filename.lastIndexOf(".");
                if (dotIdx < 0) {
                    return null;
                }
                else {
                    console.log(`[debug] load source file: ${filename}`);
                }
                const ext = filename.substring(dotIdx + 1);
                return this.getByExtension(ext);
            }
            static listLanguages() {
                return Array.from(this.byLanguage.keys());
            }
            /** Register all built-in highlighters. */
            static registerDefaults() {
                this.register(new Highlighters.VbNetHighlighter(), ["vb", "vbnet"]);
                this.register(new Highlighters.RHighlighter(), ["r", "rmd"]);
                this.register(new Highlighters.JsonHighlighter(), ["json", "jsonc"]);
                this.register(new Highlighters.XmlHighlighter(), ["xml", "xsd", "xsl", "xslt", "csproj", "vbproj", "props", "targets", "config"]);
                this.register(new Highlighters.MarkdownHighlighter(), ["md", "markdown"]);
                this.register(new Highlighters.YamlHighlighter(), ["yaml", "yml"]);
                this.register(new Highlighters.JavaScriptHighlighter(), ["js", "mjs", "cjs", "jsx"]);
                this.register(new Highlighters.TypeScriptHighlighter(), ["ts", "tsx", "mts", "cts"]);
                this.register(new Highlighters.CssHighlighter(), ["css"]);
                this.register(new Highlighters.HtmlHighlighter(), ["html", "htm", "xhtml"]);
            }
        }
        HighlighterRegistry.byLanguage = new Map();
        HighlighterRegistry.byExtension = new Map();
        Highlighters.HighlighterRegistry = HighlighterRegistry;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        /**
         * Computes foldable regions for a document based on language.
         *
         * The approach is heuristic but works well for the supported languages:
         *   - Indentation-based folding (any line whose indent is less than the
         *     next non-blank line starts a foldable region).
         *   - Bracket-based folding for { }, ( ), [ ], Begin/End, etc.
         *   - Region/End Region markers for VB.NET.
         *   - Markdown headings.
         */
        class CodeFolder {
            /**
             * Compute fold ranges for the given document.
             */
            computeFoldRanges(lines, language) {
                switch (language) {
                    case "vbnet":
                        return this.computeVbNet(lines);
                    case "r":
                    case "json":
                    case "yaml":
                        return this.computeBraceBased(lines);
                    case "javascript":
                    case "typescript":
                    case "css":
                        return this.computeCStyleBraces(lines);
                    case "xml":
                    case "html":
                        return this.computeXml(lines);
                    case "markdown":
                        return this.computeMarkdown(lines);
                    default:
                        return this.computeIndentation(lines);
                }
            }
            computeVbNet(lines) {
                const ranges = [];
                // Stack entry records the opener line, normalized text used for
                // matching closers, a display kind, and (for If blocks) the list
                // of Else/ElseIf branch start lines so each branch can be folded
                // independently.
                const stack = [];
                // Patterns that open a block. "For Each" precedes "For" and
                // "Select Case" precedes "Select" so the longer form wins.
                const openers = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Get|Set|AddHandler|RemoveHandler|RaiseEvent|Using|While|For Each|For|If|Select Case|Select|Try|SyncLock|With|Do)\b/i;
                // Region is special: closed by End Region.
                const regionOpen = /#\s*Region\b/i;
                const regionClose = /#\s*End\s+Region\b/i;
                // Explicit "End X" closers.
                const endKindRegex = /\bEnd\s+(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Get|Set|AddHandler|RemoveHandler|RaiseEvent|Using|While|For|If|Select|Try|SyncLock|With|Do)\b/i;
                // Finalize an If block: emit the main range plus a sub-range for
                // each Else/ElseIf branch.
                const closeIfBlock = (top, endLine) => {
                    if (endLine <= top.line)
                        return;
                    ranges.push({
                        startLine: top.line,
                        endLine: endLine,
                        collapsedText: "End If ...",
                        kind: "if"
                    });
                    // Sub-ranges: [ifLine..else1-1], [else1..else2-1], ..., [elseN..endIf-1].
                    const points = [top.line, ...top.elseStarts, endLine];
                    for (let e = 0; e < points.length - 1; e++) {
                        const subStart = points[e];
                        const subEnd = points[e + 1] - 1;
                        if (subEnd > subStart) {
                            ranges.push({
                                startLine: subStart,
                                endLine: subEnd,
                                collapsedText: "...",
                                kind: "if-block"
                            });
                        }
                    }
                };
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const trimmed = line.trim();
                    // Skip comments and strings (basic).
                    if (trimmed.startsWith("'") || trimmed.toLowerCase().startsWith("rem ")) {
                        continue;
                    }
                    // Code part without trailing comment, for safer keyword detection.
                    const codePart = trimmed.replace(/'.*$/, "").trimEnd();
                    if (codePart.length === 0)
                        continue;
                    // Region markers (handled independently of the opener/closer order).
                    if (regionOpen.test(trimmed)) {
                        stack.push({ line: i, text: "#region", kind: "region", elseStarts: [] });
                        continue;
                    }
                    if (regionClose.test(trimmed)) {
                        const top = stack.pop();
                        if (top && top.text === "#region" && i > top.line) {
                            ranges.push({
                                startLine: top.line,
                                endLine: i,
                                collapsedText: "...",
                                kind: "region"
                            });
                        }
                        continue;
                    }
                    // Else / ElseIf: record into the nearest enclosing If so each
                    // branch can be folded on its own.
                    if (/^(Else|ElseIf)\b/i.test(codePart)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "if") {
                                stack[k].elseStarts.push(i);
                                break;
                            }
                        }
                        continue;
                    }
                    // Explicit "End X" closers (End Function, End Class, ...).
                    // Detected BEFORE openers so "End Function" is never mis-read
                    // as a new Function opener.
                    const endMatch = endKindRegex.exec(codePart);
                    if (endMatch) {
                        const kind = endMatch[1].toLowerCase();
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === kind) {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    if (top.text === "if") {
                                        closeIfBlock(top, i);
                                    }
                                    else {
                                        ranges.push({
                                            startLine: top.line,
                                            endLine: i,
                                            collapsedText: "End " + endMatch[1] + " ...",
                                            kind: top.kind
                                        });
                                    }
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Old-style "EndIf" (no space) closes an If.
                    if (/^EndIf\b/i.test(codePart)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "if") {
                                const top = stack.splice(k)[0];
                                closeIfBlock(top, i);
                                break;
                            }
                        }
                        continue;
                    }
                    // Standalone End (for If/Select/For/While/Do/etc. without kind).
                    if (/^End\s*$/i.test(codePart)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            const t = stack[k].text;
                            if (t === "if" || t === "select" || t === "for" || t === "while" || t === "do" || t === "using" || t === "try" || t === "with" || t === "synclock") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    if (top.text === "if") {
                                        closeIfBlock(top, i);
                                    }
                                    else {
                                        ranges.push({
                                            startLine: top.line,
                                            endLine: i,
                                            collapsedText: "End ...",
                                            kind: top.kind
                                        });
                                    }
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Next (closes For, including For Each).
                    if (/^Next\b/i.test(codePart)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "for") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "Next ...",
                                        kind: top.kind
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Loop (closes Do/While).
                    if (/^Loop\b/i.test(codePart)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "do" || stack[k].text === "while") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "Loop ...",
                                        kind: top.kind
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Opener detection runs LAST, so "End X" lines are already
                    // consumed above and cannot be mis-detected as new openers.
                    const openMatch = openers.exec(codePart);
                    if (openMatch) {
                        // Single-line If: "If x Then <stmt>" (code after Then).
                        // Use codePart so a trailing comment does not look like code.
                        if (openMatch[1].toLowerCase() === "if") {
                            if (/\bThen\b/i.test(codePart) && !/\bThen\s*$/i.test(codePart)) {
                                continue;
                            }
                        }
                        // Single-line Sub/Function (e.g. "Sub Foo() : End Sub").
                        if (/\bEnd\s+Sub\b/i.test(codePart) || /\bEnd\s+Function\b/i.test(codePart)) {
                            continue;
                        }
                        const matched = openMatch[1].toLowerCase();
                        // Normalize the stack text so closers match, but keep a
                        // distinct kind label for display.
                        let text = matched;
                        let kind = matched;
                        if (matched === "for each") {
                            text = "for";
                            kind = "for each";
                        }
                        if (matched === "select case") {
                            text = "select";
                            kind = "select";
                        }
                        stack.push({ line: i, text, kind, elseStarts: [] });
                    }
                }
                return ranges;
            }
            computeBraceBased(lines) {
                const ranges = [];
                const stack = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    let inString = null;
                    let inComment = false;
                    for (let j = 0; j < line.length; j++) {
                        const c = line[j];
                        if (inComment)
                            continue;
                        if (inString) {
                            if (c === "\\") {
                                j++;
                                continue;
                            }
                            if (c === inString)
                                inString = null;
                            continue;
                        }
                        if (c === '"' || c === "'") {
                            // For R/YAML, ' is also a string. For JSON, only ".
                            inString = c;
                            continue;
                        }
                        if (c === "#") {
                            inComment = true;
                            continue;
                        }
                        if (c === "{" || c === "(" || c === "[") {
                            stack.push({ line: i, col: j });
                        }
                        else if (c === "}" || c === ")" || c === "]") {
                            const top = stack.pop();
                            if (top && top.line < i) {
                                ranges.push({
                                    startLine: top.line,
                                    endLine: i,
                                    collapsedText: "...",
                                    kind: "block"
                                });
                            }
                        }
                    }
                }
                // Merge: keep only outermost ranges per start line.
                return this.dedupeRanges(ranges);
            }
            /**
             * Brace-based folding for C-style languages (JS/TS/CSS).
             * Handles // and /* *‌/ comments, strings, and template literals.
             * Unlike computeBraceBased, does NOT treat # as a comment start.
             */
            computeCStyleBraces(lines) {
                const ranges = [];
                const stack = [];
                let inBlockComment = false;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    let inString = null;
                    let inLineComment = false;
                    for (let j = 0; j < line.length; j++) {
                        const c = line[j];
                        // Inside block comment — look for */.
                        if (inBlockComment) {
                            if (c === "*" && line[j + 1] === "/") {
                                inBlockComment = false;
                                j++;
                            }
                            continue;
                        }
                        if (inLineComment)
                            continue;
                        if (inString) {
                            if (c === "\\") {
                                j++;
                                continue;
                            }
                            if (c === inString)
                                inString = null;
                            continue;
                        }
                        // Line comment //.
                        if (c === "/" && line[j + 1] === "/") {
                            inLineComment = true;
                            continue;
                        }
                        // Block comment /*.
                        if (c === "/" && line[j + 1] === "*") {
                            inBlockComment = true;
                            j++;
                            continue;
                        }
                        // Strings.
                        if (c === '"' || c === "'" || c === "`") {
                            inString = c;
                            continue;
                        }
                        // Braces.
                        if (c === "{" || c === "(" || c === "[") {
                            stack.push({ line: i, col: j });
                        }
                        else if (c === "}" || c === ")" || c === "]") {
                            const top = stack.pop();
                            if (top && top.line < i) {
                                ranges.push({
                                    startLine: top.line,
                                    endLine: i,
                                    collapsedText: "...",
                                    kind: "block"
                                });
                            }
                        }
                    }
                    // Line comment and string state reset at end of line.
                    // Block comment state persists across lines.
                }
                return this.dedupeRanges(ranges);
            }
            computeXml(lines) {
                const ranges = [];
                const stack = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Find tags.
                    const tagRegex = /<\/?([A-Za-z_][\w\-.:]*)\b[^>]*?(\/?)>/g;
                    let m;
                    while ((m = tagRegex.exec(line)) !== null) {
                        const isClose = m[0][1] === "/";
                        const isSelfClose = m[2] === "/";
                        const tag = m[1];
                        if (isClose) {
                            // Pop until matching.
                            for (let k = stack.length - 1; k >= 0; k--) {
                                if (stack[k].tag === tag) {
                                    const top = stack.splice(k)[0];
                                    if (top.line < i) {
                                        ranges.push({
                                            startLine: top.line,
                                            endLine: i,
                                            collapsedText: "</" + tag + ">",
                                            kind: "tag"
                                        });
                                    }
                                    break;
                                }
                            }
                        }
                        else if (!isSelfClose) {
                            stack.push({ line: i, tag });
                        }
                    }
                }
                return ranges;
            }
            computeMarkdown(lines) {
                const ranges = [];
                let currentHeading = null;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const h = /^(#{1,6})\s+/.exec(line);
                    if (h) {
                        if (currentHeading) {
                            ranges.push({
                                startLine: currentHeading.line,
                                endLine: i - 1,
                                collapsedText: "...",
                                kind: "heading"
                            });
                        }
                        currentHeading = { line: i, level: h[1].length };
                    }
                }
                if (currentHeading && currentHeading.line < lines.length - 1) {
                    ranges.push({
                        startLine: currentHeading.line,
                        endLine: lines.length - 1,
                        collapsedText: "...",
                        kind: "heading"
                    });
                }
                return ranges;
            }
            computeIndentation(lines) {
                const ranges = [];
                const indents = lines.map(l => {
                    const m = /^(\s*)/.exec(l);
                    return m ? m[1].length : 0;
                });
                for (let i = 0; i < lines.length - 1; i++) {
                    if (lines[i].trim().length === 0)
                        continue;
                    const curIndent = indents[i];
                    // Find next non-blank line.
                    let j = i + 1;
                    while (j < lines.length && lines[j].trim().length === 0)
                        j++;
                    if (j >= lines.length)
                        continue;
                    if (indents[j] > curIndent) {
                        // Find end: last consecutive line with indent > curIndent.
                        let end = j;
                        while (end + 1 < lines.length && (lines[end + 1].trim().length === 0 || indents[end + 1] > curIndent)) {
                            end++;
                        }
                        ranges.push({
                            startLine: i,
                            endLine: end,
                            collapsedText: "...",
                            kind: "indent"
                        });
                    }
                }
                return ranges;
            }
            dedupeRanges(ranges) {
                // Keep only the outermost range for each start line.
                const byStart = new Map();
                for (const r of ranges) {
                    const existing = byStart.get(r.startLine);
                    if (!existing || r.endLine > existing.endLine) {
                        byStart.set(r.startLine, r);
                    }
                }
                return Array.from(byStart.values()).sort((a, b) => a.startLine - b.startLine);
            }
        }
        Features.CodeFolder = CodeFolder;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        let SymbolKind;
        (function (SymbolKind) {
            SymbolKind["Function"] = "Function";
            SymbolKind["Sub"] = "Sub";
            SymbolKind["Property"] = "Property";
            SymbolKind["Class"] = "Class";
            SymbolKind["Module"] = "Module";
            SymbolKind["Structure"] = "Structure";
            SymbolKind["Interface"] = "Interface";
            SymbolKind["Enum"] = "Enum";
            SymbolKind["Namespace"] = "Namespace";
            SymbolKind["Variable"] = "Variable";
            SymbolKind["Heading"] = "Heading";
            SymbolKind["Tag"] = "Tag";
            SymbolKind["Key"] = "Key";
            SymbolKind["Field"] = "Field";
        })(SymbolKind = Features.SymbolKind || (Features.SymbolKind = {}));
        /**
         * Extracts navigable symbols from a document based on language.
         */
        class SymbolNavigator {
            extractSymbols(lines, language) {
                switch (language) {
                    case "vbnet":
                        return this.extractVbNet(lines);
                    case "r":
                        return this.extractR(lines);
                    case "json":
                        return this.extractJson(lines);
                    case "xml":
                    case "html":
                        return this.extractXml(lines);
                    case "markdown":
                        return this.extractMarkdown(lines);
                    case "yaml":
                        return this.extractYaml(lines);
                    case "javascript":
                        return this.extractJsTs(lines, false);
                    case "typescript":
                        return this.extractJsTs(lines, true);
                    case "css":
                        return this.extractCss(lines);
                    default:
                        return [];
                }
            }
            /**
             * Builds a hierarchical tree from a flat symbol list. The nesting level
             * is derived per-language:
             *  - vbnet / r: by declaration kind (Namespace > Type > Member).
             *  - markdown: by heading level (H1 > H2 > ...).
             *  - yaml / json / xml: by leading indentation (column position).
             */
            buildSymbolTree(symbols, language) {
                const root = [];
                const stack = [];
                for (const sym of symbols) {
                    const level = this.levelOf(sym, language);
                    while (stack.length > 0 && stack[stack.length - 1].level >= level) {
                        stack.pop();
                    }
                    const parentNode = stack.length > 0 ? stack[stack.length - 1].node : null;
                    const node = { symbol: sym, level, children: [] };
                    if (parentNode) {
                        parentNode.children.push(node);
                    }
                    else {
                        root.push(node);
                    }
                    stack.push({ node, level });
                }
                return root;
            }
            levelOf(sym, language) {
                switch (language) {
                    case "markdown": {
                        const m = /^H(\d+)$/.exec(sym.detail || "");
                        return m ? parseInt(m[1], 10) : 1;
                    }
                    case "yaml":
                    case "json":
                    case "xml":
                    case "html":
                    case "css":
                        // Indentation-based nesting: deeper keys sit further right.
                        return sym.column;
                    case "javascript":
                    case "typescript":
                        switch (sym.kind) {
                            case SymbolKind.Namespace:
                                return 1;
                            case SymbolKind.Class:
                            case SymbolKind.Interface:
                            case SymbolKind.Enum:
                            case SymbolKind.Module:
                                return 2;
                            default:
                                return 3;
                        }
                    case "vbnet":
                    case "r":
                    default:
                        switch (sym.kind) {
                            case SymbolKind.Namespace:
                                return 1;
                            case SymbolKind.Module:
                            case SymbolKind.Class:
                            case SymbolKind.Structure:
                            case SymbolKind.Interface:
                            case SymbolKind.Enum:
                                return 2;
                            default:
                                return 3;
                        }
                }
            }
            extractVbNet(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const trimmed = line.trim();
                    if (trimmed.startsWith("'") || trimmed.toLowerCase().startsWith("rem "))
                        continue;
                    // Match declarations.
                    const m = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Delegate)\s+([A-Za-z_][A-Za-z0-9_]*)/i.exec(trimmed);
                    if (m) {
                        const kindStr = m[1].charAt(0).toUpperCase() + m[1].slice(1).toLowerCase();
                        let kind;
                        switch (m[1].toLowerCase()) {
                            case "class":
                                kind = SymbolKind.Class;
                                break;
                            case "module":
                                kind = SymbolKind.Module;
                                break;
                            case "structure":
                                kind = SymbolKind.Structure;
                                break;
                            case "interface":
                                kind = SymbolKind.Interface;
                                break;
                            case "enum":
                                kind = SymbolKind.Enum;
                                break;
                            case "namespace":
                                kind = SymbolKind.Namespace;
                                break;
                            case "sub":
                                kind = SymbolKind.Sub;
                                break;
                            case "function":
                                kind = SymbolKind.Function;
                                break;
                            case "property":
                                kind = SymbolKind.Property;
                                break;
                            case "operator":
                                kind = SymbolKind.Function;
                                break;
                            case "event":
                                kind = SymbolKind.Function;
                                break;
                            case "delegate":
                                kind = SymbolKind.Function;
                                break;
                            default: kind = SymbolKind.Function;
                        }
                        const col = line.indexOf(m[2]);
                        symbols.push({
                            name: m[2],
                            kind,
                            line: i,
                            column: col >= 0 ? col : 0,
                            detail: m[1]
                        });
                    }
                }
                return symbols;
            }
            extractR(lines) {
                const symbols = [];
                // Function definitions: name <- function(...) or function name(...) {...}
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Pattern: name <- function
                    const m1 = /^([A-Za-z_.][A-Za-z0-9_.]*)\s*(<-|=)\s*function\b/.exec(line);
                    if (m1) {
                        symbols.push({
                            name: m1[1],
                            kind: SymbolKind.Function,
                            line: i,
                            column: 0,
                            detail: "function"
                        });
                        continue;
                    }
                    // Pattern: function(name)
                    const m2 = /\bfunction\s*\(\s*([A-Za-z_.][A-Za-z0-9_.]*)\s*\)/.exec(line);
                    if (m2) {
                        symbols.push({
                            name: m2[1],
                            kind: SymbolKind.Function,
                            line: i,
                            column: m2.index,
                            detail: "function"
                        });
                        continue;
                    }
                    // Variable assignment: name <- value (top-level only, simple heuristic).
                    const m3 = /^([A-Za-z_.][A-Za-z0-9_.]*)\s*<-\s*(?!function\b)/.exec(line);
                    if (m3) {
                        symbols.push({
                            name: m3[1],
                            kind: SymbolKind.Variable,
                            line: i,
                            column: 0,
                            detail: "variable"
                        });
                    }
                }
                return symbols;
            }
            extractJson(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Match "key":
                    const m = /^\s*"([^"]+)"\s*:/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[1],
                            kind: SymbolKind.Key,
                            line: i,
                            column: line.indexOf('"'),
                            detail: "key"
                        });
                    }
                }
                return symbols;
            }
            extractXml(lines) {
                const symbols = [];
                const seen = new Set();
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /<([A-Za-z_][\w\-.:]*)\b[^>]*>/g;
                    let match;
                    while ((match = m.exec(line)) !== null) {
                        const tag = match[1];
                        if (tag.startsWith("/"))
                            continue;
                        if (seen.has(tag))
                            continue;
                        seen.add(tag);
                        symbols.push({
                            name: tag,
                            kind: SymbolKind.Tag,
                            line: i,
                            column: match.index,
                            detail: "tag"
                        });
                    }
                }
                return symbols;
            }
            extractMarkdown(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /^(#{1,6})\s+(.+?)\s*$/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[2],
                            kind: SymbolKind.Heading,
                            line: i,
                            column: 0,
                            detail: "H" + m[1].length
                        });
                    }
                }
                return symbols;
            }
            extractYaml(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /^(\s*)([A-Za-z_][A-Za-z0-9_\-\.]*)\s*:/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[2],
                            kind: SymbolKind.Key,
                            line: i,
                            column: m[1].length,
                            detail: "key"
                        });
                    }
                }
                return symbols;
            }
            /**
             * Extract symbols from JavaScript / TypeScript source.
             * Finds: namespace, class, interface, enum, type alias, function,
             * and arrow-function/const declarations.
             */
            extractJsTs(lines, isTypeScript) {
                const symbols = [];
                const indentRegex = /^(\s*)/;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Strip line comments for cleaner matching.
                    const commentIdx = line.indexOf("//");
                    const code = commentIdx >= 0 ? line.substring(0, commentIdx) : line;
                    const indent = indentRegex.exec(line)[1].length;
                    // namespace Name {
                    let m = /\b(?:declare\s+)?namespace\s+([A-Za-z_$][\w$]*)/.exec(code);
                    if (m) {
                        symbols.push({ name: m[1], kind: SymbolKind.Namespace, line: i, column: indent, detail: "namespace" });
                        continue;
                    }
                    // class Name
                    m = /\b(?:export\s+)?(?:default\s+)?(?:abstract\s+)?class\s+([A-Za-z_$][\w$]*)/.exec(code);
                    if (m) {
                        symbols.push({ name: m[1], kind: SymbolKind.Class, line: i, column: indent, detail: "class" });
                        continue;
                    }
                    // interface Name (TS only)
                    if (isTypeScript) {
                        m = /\binterface\s+([A-Za-z_$][\w$]*)/.exec(code);
                        if (m) {
                            symbols.push({ name: m[1], kind: SymbolKind.Interface, line: i, column: indent, detail: "interface" });
                            continue;
                        }
                        // enum Name
                        m = /\b(?:const\s+)?enum\s+([A-Za-z_$][\w$]*)/.exec(code);
                        if (m) {
                            symbols.push({ name: m[1], kind: SymbolKind.Enum, line: i, column: indent, detail: "enum" });
                            continue;
                        }
                        // type Name =
                        m = /\btype\s+([A-Za-z_$][\w$]*)\s*=/.exec(code);
                        if (m) {
                            symbols.push({ name: m[1], kind: SymbolKind.Structure, line: i, column: indent, detail: "type" });
                            continue;
                        }
                    }
                    // function name(
                    m = /\b(?:export\s+)?(?:async\s+)?function\s+([A-Za-z_$][\w$]*)/.exec(code);
                    if (m) {
                        symbols.push({ name: m[1], kind: SymbolKind.Function, line: i, column: indent, detail: "function" });
                        continue;
                    }
                    // const/let/var name = (arrow function or function expression)
                    m = /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*(?:async\s+)?(?:\([^)]*\)|function|[\w$]+\s*=>)/.exec(code);
                    if (m) {
                        symbols.push({ name: m[1], kind: SymbolKind.Variable, line: i, column: indent, detail: "variable" });
                        continue;
                    }
                    // Method: indented name( inside a class (heuristic)
                    m = /^(\s+)([A-Za-z_$][\w$]*)\s*\(/.exec(line);
                    if (m && indent > 0) {
                        // Skip if it's a keyword (if, for, while, switch, etc.)
                        const controlWords = ["if", "for", "while", "switch", "catch", "return", "throw", "function"];
                        if (controlWords.indexOf(m[2]) < 0) {
                            symbols.push({ name: m[2], kind: SymbolKind.Function, line: i, column: indent, detail: "method" });
                        }
                    }
                }
                return symbols;
            }
            /**
             * Extract symbols from CSS source.
             * Finds: at-rules (@media, @keyframes, etc.) and selector rules.
             */
            extractCss(lines) {
                const symbols = [];
                const indentRegex = /^(\s*)/;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const trimmed = line.trim();
                    if (!trimmed || trimmed.startsWith("/*") || trimmed.startsWith("*"))
                        continue;
                    const indent = indentRegex.exec(line)[1].length;
                    // At-rule: @media, @keyframes, @supports, @font-face, @page, etc.
                    const atM = /^(@[A-Za-z\-]+)\s+([A-Za-z_][\w\-]*)/.exec(trimmed);
                    if (atM) {
                        symbols.push({
                            name: atM[1] + " " + atM[2],
                            kind: SymbolKind.Namespace,
                            line: i,
                            column: indent,
                            detail: "at-rule"
                        });
                        continue;
                    }
                    // Standalone at-rule without name (@font-face, @import).
                    const atOnly = /^(@[A-Za-z\-]+)/.exec(trimmed);
                    if (atOnly) {
                        symbols.push({
                            name: atOnly[1],
                            kind: SymbolKind.Namespace,
                            line: i,
                            column: indent,
                            detail: "at-rule"
                        });
                        continue;
                    }
                    // Selector rule: line ending with { and not starting with -- (custom property).
                    if (trimmed.endsWith("{") && !trimmed.startsWith("--")) {
                        // Remove trailing { and whitespace.
                        const selector = trimmed.replace(/\s*\{$/, "").trim();
                        if (selector.length > 0) {
                            symbols.push({
                                name: selector,
                                kind: SymbolKind.Tag,
                                line: i,
                                column: indent,
                                detail: "selector"
                            });
                        }
                    }
                }
                return symbols;
            }
        }
        Features.SymbolNavigator = SymbolNavigator;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        var computeLineDiff = CodeEditor.Utils.computeLineDiff;
        var summarizeDiff = CodeEditor.Utils.summarizeDiff;
        /**
         * Manages git-style diff visualization between the original loaded text
         * and the current edited text.
         */
        class DiffViewer {
            constructor() {
                this.originalText = "";
                this.currentText = "";
                this.cachedDiff = null;
            }
            setOriginal(text) {
                this.originalText = text;
                this.cachedDiff = null;
            }
            setCurrent(text) {
                this.currentText = text;
                this.cachedDiff = null;
            }
            getDiff() {
                if (this.cachedDiff === null) {
                    this.cachedDiff = computeLineDiff(this.originalText, this.currentText);
                }
                return this.cachedDiff;
            }
            getSummary() {
                return summarizeDiff(this.getDiff());
            }
            /**
             * Returns the diff line index that corresponds to the given new-line
             * number, or -1 if not found. Used to scroll the diff view to match
             * the editor caret.
             */
            findDiffLineForNewLine(newLine) {
                const diff = this.getDiff();
                for (let i = 0; i < diff.length; i++) {
                    if (diff[i].newLineNumber === newLine) {
                        return i;
                    }
                }
                return -1;
            }
            /**
             * Build HTML for the diff view. Each line is a <div> with class
             * diff-equal, diff-added, or diff-removed.
             */
            renderDiffHtml() {
                const diff = this.getDiff();
                const parts = [];
                for (const d of diff) {
                    const cls = d.type === "added" ? "diff-added" : d.type === "removed" ? "diff-removed" : "diff-equal";
                    const sign = d.type === "added" ? "+" : d.type === "removed" ? "-" : " ";
                    const oldNum = d.oldLineNumber > 0 ? String(d.oldLineNumber) : "";
                    const newNum = d.newLineNumber > 0 ? String(d.newLineNumber) : "";
                    const escaped = CodeEditor.Utils.escapeHtml(d.content);
                    parts.push(`<div class="diff-line ${cls}">` +
                        `<span class="diff-oldnum">${oldNum}</span>` +
                        `<span class="diff-newnum">${newNum}</span>` +
                        `<span class="diff-sign">${sign}</span>` +
                        `<span class="diff-content">${escaped}</span>` +
                        `</div>`);
                }
                return parts.join("");
            }
        }
        Features.DiffViewer = DiffViewer;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        /**
         * Provides intelligent code completion by calling a REST endpoint.
         *
         * The endpoint URL is configurable. If the endpoint is unreachable or
         * returns an error, a small built-in fallback suggestion list is used
         * based on the current language.
         */
        class CompletionProvider {
            constructor() {
                this.endpoint = "";
                this.enabled = true;
            }
            setEndpoint(url) {
                this.endpoint = url;
            }
            getEndpoint() {
                return this.endpoint;
            }
            setEnabled(enabled) {
                this.enabled = enabled;
            }
            /**
             * Request completions asynchronously. Returns a promise that resolves
             * to a list of completion items.
             */
            async requestCompletions(req) {
                if (!this.enabled) {
                    return [];
                }
                if (this.endpoint) {
                    try {
                        const items = await this.callEndpoint(req);
                        return items;
                    }
                    catch (e) {
                        // Fall through to fallback.
                        console.warn("Completion endpoint failed, using fallback:", e);
                    }
                }
                return this.fallbackCompletions(req);
            }
            async callEndpoint(req) {
                const response = await fetch(this.endpoint, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(req)
                });
                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }
                const data = await response.json();
                if (data.items && Array.isArray(data.items)) {
                    return data.items;
                }
                if (data.suggestions && Array.isArray(data.suggestions)) {
                    return data.suggestions.map(s => ({ label: s, kind: "text" }));
                }
                return [];
            }
            /**
             * Built-in fallback suggestions per language. These are static
             * keyword lists that are useful when no backend is configured.
             */
            fallbackCompletions(req) {
                switch (req.language) {
                    case "vbnet":
                        return this.vbNetCompletions();
                    case "r":
                        return this.rCompletions();
                    case "json":
                        return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                    case "yaml":
                        return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                    case "javascript":
                        return this.jsCompletions();
                    case "typescript":
                        return this.tsCompletions();
                    case "css":
                        return this.cssCompletions();
                    case "html":
                        return this.htmlCompletions();
                    default:
                        return [];
                }
            }
            vbNetCompletions() {
                const keywords = [
                    "Public", "Private", "Protected", "Friend", "Shared", "ReadOnly", "WriteOnly",
                    "Class", "Module", "Structure", "Interface", "Enum", "Namespace",
                    "Sub", "Function", "Property", "Operator", "Event", "Delegate",
                    "Dim", "Const", "Static", "If", "Then", "Else", "ElseIf", "End If",
                    "For", "Each", "In", "Next", "While", "Do", "Loop", "Until",
                    "Try", "Catch", "Finally", "Throw", "Using", "SyncLock",
                    "Return", "Yield", "Await", "Async", "Iterator", "Partial",
                    "Inherits", "Implements", "Of", "As", "New", "Me", "MyBase",
                    "Nothing", "True", "False", "And", "Or", "Not", "AndAlso", "OrElse",
                    "ByVal", "ByRef", "Optional", "ParamArray", "Handles", "AddressOf",
                    "GetType", "TypeOf", "DirectCast", "TryCast", "CType"
                ];
                return keywords.map(k => ({ label: k, kind: "keyword", insertText: k }));
            }
            rCompletions() {
                const items = [
                    { label: "if", kind: "keyword" },
                    { label: "else", kind: "keyword" },
                    { label: "for", kind: "keyword" },
                    { label: "while", kind: "keyword" },
                    { label: "function", kind: "keyword", insertText: "function() {\n  \n}" },
                    { label: "return", kind: "keyword" },
                    { label: "library", kind: "function", insertText: "library()" },
                    { label: "require", kind: "function", insertText: "require()" },
                    { label: "source", kind: "function", insertText: "source('')" },
                    { label: "print", kind: "function", insertText: "print()" },
                    { label: "cat", kind: "function", insertText: "cat()" },
                    { label: "paste", kind: "function", insertText: "paste()" },
                    { label: "paste0", kind: "function", insertText: "paste0()" },
                    { label: "sprintf", kind: "function", insertText: "sprintf()" },
                    { label: "c", kind: "function", insertText: "c()" },
                    { label: "list", kind: "function", insertText: "list()" },
                    { label: "vector", kind: "function", insertText: "vector()" },
                    { label: "matrix", kind: "function", insertText: "matrix()" },
                    { label: "data.frame", kind: "function", insertText: "data.frame()" },
                    { label: "factor", kind: "function", insertText: "factor()" },
                    { label: "length", kind: "function", insertText: "length()" },
                    { label: "nrow", kind: "function", insertText: "nrow()" },
                    { label: "ncol", kind: "function", insertText: "ncol()" },
                    { label: "names", kind: "function", insertText: "names()" },
                    { label: "rownames", kind: "function", insertText: "rownames()" },
                    { label: "colnames", kind: "function", insertText: "colnames()" },
                    { label: "head", kind: "function", insertText: "head()" },
                    { label: "tail", kind: "function", insertText: "tail()" },
                    { label: "summary", kind: "function", insertText: "summary()" },
                    { label: "str", kind: "function", insertText: "str()" },
                    { label: "mean", kind: "function", insertText: "mean()" },
                    { label: "median", kind: "function", insertText: "median()" },
                    { label: "sd", kind: "function", insertText: "sd()" },
                    { label: "var", kind: "function", insertText: "var()" },
                    { label: "sum", kind: "function", insertText: "sum()" },
                    { label: "min", kind: "function", insertText: "min()" },
                    { label: "max", kind: "function", insertText: "max()" },
                    { label: "range", kind: "function", insertText: "range()" },
                    { label: "TRUE", kind: "constant" },
                    { label: "FALSE", kind: "constant" },
                    { label: "NULL", kind: "constant" },
                    { label: "NA", kind: "constant" },
                    { label: "Inf", kind: "constant" },
                    { label: "NaN", kind: "constant" },
                    { label: "pi", kind: "constant" }
                ];
                return items;
            }
            jsCompletions() {
                const items = [
                    // Control flow
                    { label: "if", kind: "keyword" },
                    { label: "else", kind: "keyword" },
                    { label: "for", kind: "keyword" },
                    { label: "while", kind: "keyword" },
                    { label: "do", kind: "keyword" },
                    { label: "switch", kind: "keyword" },
                    { label: "case", kind: "keyword" },
                    { label: "break", kind: "keyword" },
                    { label: "continue", kind: "keyword" },
                    { label: "return", kind: "keyword" },
                    { label: "throw", kind: "keyword" },
                    { label: "try", kind: "keyword" },
                    { label: "catch", kind: "keyword" },
                    { label: "finally", kind: "keyword" },
                    // Declarations
                    { label: "var", kind: "keyword" },
                    { label: "let", kind: "keyword" },
                    { label: "const", kind: "keyword" },
                    { label: "function", kind: "keyword", insertText: "function () {\n  \n}" },
                    { label: "class", kind: "keyword", insertText: "class  {\n  \n}" },
                    { label: "extends", kind: "keyword" },
                    { label: "new", kind: "keyword" },
                    { label: "this", kind: "keyword" },
                    { label: "super", kind: "keyword" },
                    { label: "import", kind: "keyword" },
                    { label: "export", kind: "keyword" },
                    { label: "default", kind: "keyword" },
                    { label: "from", kind: "keyword" },
                    { label: "as", kind: "keyword" },
                    { label: "async", kind: "keyword" },
                    { label: "await", kind: "keyword" },
                    { label: "typeof", kind: "keyword" },
                    { label: "instanceof", kind: "keyword" },
                    { label: "in", kind: "keyword" },
                    { label: "of", kind: "keyword" },
                    { label: "delete", kind: "keyword" },
                    { label: "void", kind: "keyword" },
                    // Constants
                    { label: "true", kind: "constant" },
                    { label: "false", kind: "constant" },
                    { label: "null", kind: "constant" },
                    { label: "undefined", kind: "constant" },
                    { label: "NaN", kind: "constant" },
                    { label: "Infinity", kind: "constant" },
                    // Built-ins
                    { label: "console", kind: "variable" },
                    { label: "console.log", kind: "function", insertText: "console.log()" },
                    { label: "console.error", kind: "function", insertText: "console.error()" },
                    { label: "console.warn", kind: "function", insertText: "console.warn()" },
                    { label: "Math", kind: "class" },
                    { label: "JSON", kind: "class" },
                    { label: "JSON.parse", kind: "function", insertText: "JSON.parse()" },
                    { label: "JSON.stringify", kind: "function", insertText: "JSON.stringify()" },
                    { label: "Promise", kind: "class" },
                    { label: "Array", kind: "class" },
                    { label: "Object", kind: "class" },
                    { label: "String", kind: "class" },
                    { label: "Number", kind: "class" },
                    { label: "Boolean", kind: "class" },
                    { label: "Date", kind: "class" },
                    { label: "RegExp", kind: "class" },
                    { label: "Map", kind: "class" },
                    { label: "Set", kind: "class" },
                    { label: "setTimeout", kind: "function", insertText: "setTimeout()" },
                    { label: "setInterval", kind: "function", insertText: "setInterval()" },
                    { label: "parseInt", kind: "function", insertText: "parseInt()" },
                    { label: "parseFloat", kind: "function", insertText: "parseFloat()" }
                ];
                return items;
            }
            tsCompletions() {
                const items = this.jsCompletions();
                // TypeScript-specific keywords.
                items.push({ label: "interface", kind: "keyword", insertText: "interface  {\n  \n}" }, { label: "type", kind: "keyword", insertText: "type  = " }, { label: "enum", kind: "keyword", insertText: "enum  {\n  \n}" }, { label: "implements", kind: "keyword" }, { label: "declare", kind: "keyword" }, { label: "namespace", kind: "keyword" }, { label: "readonly", kind: "keyword" }, { label: "abstract", kind: "keyword" }, { label: "public", kind: "keyword" }, { label: "private", kind: "keyword" }, { label: "protected", kind: "keyword" }, { label: "static", kind: "keyword" }, { label: "override", kind: "keyword" }, { label: "keyof", kind: "keyword" }, { label: "infer", kind: "keyword" }, { label: "is", kind: "keyword" }, { label: "as", kind: "keyword" }, { label: "satisfies", kind: "keyword" }, 
                // Built-in types
                { label: "string", kind: "type" }, { label: "number", kind: "type" }, { label: "boolean", kind: "type" }, { label: "any", kind: "type" }, { label: "unknown", kind: "type" }, { label: "never", kind: "type" }, { label: "void", kind: "type" }, { label: "object", kind: "type" }, { label: "symbol", kind: "type" }, { label: "bigint", kind: "type" }, { label: "Record", kind: "type", insertText: "Record<,>" }, { label: "Partial", kind: "type", insertText: "Partial<>" }, { label: "Readonly", kind: "type", insertText: "Readonly<>" }, { label: "Pick", kind: "type", insertText: "Pick<,>" }, { label: "Omit", kind: "type", insertText: "Omit<,>" }, { label: "ReturnType", kind: "type", insertText: "ReturnType<>" }, { label: "Parameters", kind: "type", insertText: "Parameters<>" });
                return items;
            }
            cssCompletions() {
                const items = [
                    // At-rules
                    { label: "@media", kind: "keyword", insertText: "@media () {\n  \n}" },
                    { label: "@keyframes", kind: "keyword", insertText: "@keyframes  {\n  \n}" },
                    { label: "@import", kind: "keyword", insertText: "@import '';" },
                    { label: "@supports", kind: "keyword", insertText: "@supports () {\n  \n}" },
                    { label: "@font-face", kind: "keyword", insertText: "@font-face {\n  \n}" },
                    { label: "@charset", kind: "keyword", insertText: "@charset '';" },
                    { label: "@page", kind: "keyword" },
                    { label: "@layer", kind: "keyword" },
                    { label: "@container", kind: "keyword" },
                    // Common properties
                    { label: "display", kind: "property" },
                    { label: "position", kind: "property" },
                    { label: "top", kind: "property" },
                    { label: "right", kind: "property" },
                    { label: "bottom", kind: "property" },
                    { label: "left", kind: "property" },
                    { label: "width", kind: "property" },
                    { label: "height", kind: "property" },
                    { label: "min-width", kind: "property" },
                    { label: "max-width", kind: "property" },
                    { label: "margin", kind: "property" },
                    { label: "padding", kind: "property" },
                    { label: "border", kind: "property" },
                    { label: "border-radius", kind: "property" },
                    { label: "color", kind: "property" },
                    { label: "background", kind: "property" },
                    { label: "background-color", kind: "property" },
                    { label: "font-family", kind: "property" },
                    { label: "font-size", kind: "property" },
                    { label: "font-weight", kind: "property" },
                    { label: "line-height", kind: "property" },
                    { label: "text-align", kind: "property" },
                    { label: "text-decoration", kind: "property" },
                    { label: "flex", kind: "property" },
                    { label: "flex-direction", kind: "property" },
                    { label: "flex-wrap", kind: "property" },
                    { label: "justify-content", kind: "property" },
                    { label: "align-items", kind: "property" },
                    { label: "grid", kind: "property" },
                    { label: "grid-template-columns", kind: "property" },
                    { label: "grid-template-rows", kind: "property" },
                    { label: "gap", kind: "property" },
                    { label: "z-index", kind: "property" },
                    { label: "opacity", kind: "property" },
                    { label: "overflow", kind: "property" },
                    { label: "cursor", kind: "property" },
                    { label: "transition", kind: "property" },
                    { label: "transform", kind: "property" },
                    { label: "animation", kind: "property" },
                    { label: "box-shadow", kind: "property" },
                    // Common values
                    { label: "none", kind: "value" },
                    { label: "auto", kind: "value" },
                    { label: "inherit", kind: "value" },
                    { label: "initial", kind: "value" },
                    { label: "unset", kind: "value" },
                    { label: "block", kind: "value" },
                    { label: "inline", kind: "value" },
                    { label: "flex", kind: "value" },
                    { label: "grid", kind: "value" },
                    { label: "absolute", kind: "value" },
                    { label: "relative", kind: "value" },
                    { label: "fixed", kind: "value" },
                    { label: "sticky", kind: "value" },
                    { label: "center", kind: "value" },
                    { label: "space-between", kind: "value" },
                    { label: "space-around", kind: "value" },
                    { label: "wrap", kind: "value" },
                    { label: "nowrap", kind: "value" },
                    { label: "solid", kind: "value" },
                    { label: "dashed", kind: "value" },
                    { label: "dotted", kind: "value" },
                    { label: "bold", kind: "value" },
                    { label: "italic", kind: "value" },
                    { label: "pointer", kind: "value" },
                    { label: "default", kind: "value" },
                    // Functions
                    { label: "var()", kind: "function", insertText: "var(--)" },
                    { label: "calc()", kind: "function", insertText: "calc()" },
                    { label: "rgb()", kind: "function", insertText: "rgb(, , )" },
                    { label: "rgba()", kind: "function", insertText: "rgba(, , , )" },
                    { label: "hsl()", kind: "function", insertText: "hsl(, , )" },
                    { label: "url()", kind: "function", insertText: "url('')" },
                    { label: "linear-gradient()", kind: "function", insertText: "linear-gradient()" },
                    // !important
                    { label: "!important", kind: "keyword" }
                ];
                return items;
            }
            htmlCompletions() {
                const items = [
                    // Structural tags
                    { label: "html", kind: "keyword", insertText: "<html>\n  \n</html>" },
                    { label: "head", kind: "keyword", insertText: "<head>\n  \n</head>" },
                    { label: "body", kind: "keyword", insertText: "<body>\n  \n</body>" },
                    { label: "div", kind: "keyword", insertText: "<div>\n  \n</div>" },
                    { label: "span", kind: "keyword", insertText: "<span></span>" },
                    { label: "section", kind: "keyword", insertText: "<section>\n  \n</section>" },
                    { label: "header", kind: "keyword", insertText: "<header>\n  \n</header>" },
                    { label: "footer", kind: "keyword", insertText: "<footer>\n  \n</footer>" },
                    { label: "nav", kind: "keyword", insertText: "<nav>\n  \n</nav>" },
                    { label: "main", kind: "keyword", insertText: "<main>\n  \n</main>" },
                    { label: "article", kind: "keyword", insertText: "<article>\n  \n</article>" },
                    { label: "aside", kind: "keyword", insertText: "<aside>\n  \n</aside>" },
                    // Text tags
                    { label: "h1", kind: "keyword", insertText: "<h1></h1>" },
                    { label: "h2", kind: "keyword", insertText: "<h2></h2>" },
                    { label: "h3", kind: "keyword", insertText: "<h3></h3>" },
                    { label: "p", kind: "keyword", insertText: "<p></p>" },
                    { label: "a", kind: "keyword", insertText: "<a href=\"\"></a>" },
                    { label: "strong", kind: "keyword", insertText: "<strong></strong>" },
                    { label: "em", kind: "keyword", insertText: "<em></em>" },
                    { label: "br", kind: "keyword", insertText: "<br>" },
                    { label: "hr", kind: "keyword", insertText: "<hr>" },
                    // Lists
                    { label: "ul", kind: "keyword", insertText: "<ul>\n  \n</ul>" },
                    { label: "ol", kind: "keyword", insertText: "<ol>\n  \n</ol>" },
                    { label: "li", kind: "keyword", insertText: "<li></li>" },
                    // Media
                    { label: "img", kind: "keyword", insertText: "<img src=\"\" alt=\"\">" },
                    { label: "video", kind: "keyword", insertText: "<video src=\"\"></video>" },
                    { label: "audio", kind: "keyword", insertText: "<audio src=\"\"></audio>" },
                    { label: "canvas", kind: "keyword", insertText: "<canvas></canvas>" },
                    { label: "svg", kind: "keyword", insertText: "<svg></svg>" },
                    // Forms
                    { label: "form", kind: "keyword", insertText: "<form action=\"\"></form>" },
                    { label: "input", kind: "keyword", insertText: "<input type=\"\">" },
                    { label: "button", kind: "keyword", insertText: "<button></button>" },
                    { label: "label", kind: "keyword", insertText: "<label></label>" },
                    { label: "select", kind: "keyword", insertText: "<select>\n  \n</select>" },
                    { label: "option", kind: "keyword", insertText: "<option value=\"\"></option>" },
                    { label: "textarea", kind: "keyword", insertText: "<textarea></textarea>" },
                    // Tables
                    { label: "table", kind: "keyword", insertText: "<table>\n  \n</table>" },
                    { label: "tr", kind: "keyword", insertText: "<tr>\n  \n</tr>" },
                    { label: "td", kind: "keyword", insertText: "<td></td>" },
                    { label: "th", kind: "keyword", insertText: "<th></th>" },
                    { label: "thead", kind: "keyword", insertText: "<thead>\n  \n</thead>" },
                    { label: "tbody", kind: "keyword", insertText: "<tbody>\n  \n</tbody>" },
                    // Script / Style
                    { label: "script", kind: "keyword", insertText: "<script>\n  \n</script>" },
                    { label: "style", kind: "keyword", insertText: "<style>\n  \n</style>" },
                    { label: "link", kind: "keyword", insertText: "<link rel=\"\" href=\"\">" },
                    { label: "meta", kind: "keyword", insertText: "<meta >" },
                    // Common attributes
                    { label: "class", kind: "property" },
                    { label: "id", kind: "property" },
                    { label: "style", kind: "property" },
                    { label: "href", kind: "property" },
                    { label: "src", kind: "property" },
                    { label: "alt", kind: "property" },
                    { label: "title", kind: "property" },
                    { label: "type", kind: "property" },
                    { label: "name", kind: "property" },
                    { label: "value", kind: "property" },
                    { label: "placeholder", kind: "property" },
                    { label: "onclick", kind: "property" },
                    { label: "onload", kind: "property" },
                    { label: "disabled", kind: "property" },
                    { label: "readonly", kind: "property" },
                    { label: "checked", kind: "property" },
                    { label: "required", kind: "property" },
                    { label: "data-", kind: "property" },
                    { label: "aria-", kind: "property" }
                ];
                return items;
            }
        }
        Features.CompletionProvider = CompletionProvider;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        /**
         * Implements the "Go to Line" command. The UI is a small modal dialog
         * with a line number input; this class handles validation and the
         * callback to the editor.
         */
        class GoToLine {
            constructor() {
                this.maxLine = 1;
            }
            setMaxLine(max) {
                this.maxLine = max;
            }
            getMaxLine() {
                return this.maxLine;
            }
            /**
             * Validate a line number string. Returns the parsed line number
             * (1-based) or -1 if invalid.
             */
            validate(input) {
                const n = parseInt(input, 10);
                if (isNaN(n) || n < 1 || n > this.maxLine) {
                    return -1;
                }
                return n;
            }
        }
        Features.GoToLine = GoToLine;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        var TokenType = CodeEditor.Utils.TokenType;
        var CodeFolder = CodeEditor.Features.CodeFolder;
        var SymbolNavigator = CodeEditor.Features.SymbolNavigator;
        var DiffViewer = CodeEditor.Features.DiffViewer;
        var CompletionProvider = CodeEditor.Features.CompletionProvider;
        var GoToLine = CodeEditor.Features.GoToLine;
        /**
         * The main editor controller. Owns the text buffer, cursor, highlighter,
         * and renders into a set of layered DOM elements:
         *   - gutter (line numbers + fold markers)
         *   - code view (highlighted lines)
         *   - caret overlay
         *   - selection overlay
         *   - completion popup
         */
        class Editor {
            constructor(container, options) {
                this.buffer = new Core.TextBuffer();
                this.cursor = new Core.Cursor();
                this.highlighter = new Core.Highlighter();
                this.folder = new CodeFolder();
                this.symbolNav = new SymbolNavigator();
                this.diffViewer = new DiffViewer();
                this.completionProvider = new CompletionProvider();
                this.goToLine = new GoToLine();
                this.options = {
                    tabSize: 4,
                    useSpaces: true,
                    fontSize: 14,
                    fontFamily: "'Cascadia Code', 'Consolas', 'Courier New', monospace",
                    lineNumbers: true,
                    wordWrap: false
                };
                this.language = "plain";
                this.currentHighlighter = null;
                this.foldRanges = [];
                this.collapsedLines = new Set();
                this.symbols = [];
                this.filename = "untitled";
                this.charWidth = 8;
                this.lineHeight = 20;
                this.firstVisibleLine = 0;
                this.visibleLineCount = 40;
                this.completionItems = [];
                this.completionActive = false;
                this.completionIndex = 0;
                this.completionAnchor = { line: 0, column: 0 };
                this.minimapVisible = false;
                this.minimapLineHeight = 3;
                this.minimapDirty = true;
                this.minimapDragging = false;
                // While setting the textarea selection the browser triggers a native
                // "caret scrolling" side-effect. Because the textarea is transformed
                // to the cursor line, this double-counts the line offset and scrolls
                // the container to the wrong position. This flag gates the scroll
                // handler so the caret-scroll does not cause a render loop.
                this._suppressCaretScroll = false;
                this.onChangeCallbacks = [];
                this.onCursorChangeCallbacks = [];
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
                    this.minimapDirty = true;
                    this.render();
                    this.fireChange();
                });
                this.recomputeFolds();
                this.recomputeSymbols();
                this.render();
            }
            buildDom() {
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
                // Minimap (right-side code thumbnail / scrollbar proxy).
                this.minimap = document.createElement("div");
                this.minimap.className = "editor-minimap";
                this.minimapContent = document.createElement("div");
                this.minimapContent.className = "minimap-content";
                this.minimapViewport = document.createElement("div");
                this.minimapViewport.className = "minimap-viewport";
                this.minimap.appendChild(this.minimapContent);
                this.minimap.appendChild(this.minimapViewport);
                this.container.appendChild(this.minimap);
                // Measure char width.
                this.measureCharWidth();
            }
            measureCharWidth() {
                const measure = document.createElement("span");
                measure.className = "editor-measure";
                measure.textContent = "M".repeat(100);
                this.codeView.appendChild(measure);
                const rect = measure.getBoundingClientRect();
                this.charWidth = rect.width / 100;
                this.lineHeight = rect.height || 20;
                this.codeView.removeChild(measure);
            }
            attachEvents() {
                this.scrollContainer.addEventListener("scroll", () => {
                    if (this._suppressCaretScroll)
                        return;
                    this.firstVisibleLine = Math.floor(this.scrollContainer.scrollTop / this.lineHeight);
                    this.render();
                });
                // Minimap acts like a scrollbar: click/drag scrolls the editor.
                this.minimap.addEventListener("mousedown", (e) => {
                    if (!this.minimapVisible)
                        return;
                    this.minimapDragging = true;
                    this.scrollFromMinimap(e);
                    e.preventDefault();
                });
                window.addEventListener("mousemove", (e) => {
                    if (this.minimapDragging)
                        this.scrollFromMinimap(e);
                });
                window.addEventListener("mouseup", () => {
                    this.minimapDragging = false;
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
                    this.minimapDirty = true;
                    this.render();
                });
            }
            handleInput() {
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
            textareaToBufferPos(pos) {
                const text = this.textarea.value;
                let line = 0;
                let col = 0;
                for (let i = 0; i < pos && i < text.length; i++) {
                    if (text[i] === "\n") {
                        line++;
                        col = 0;
                    }
                    else {
                        col++;
                    }
                }
                return { line, column: col };
            }
            bufferToTextareaPos(line, column) {
                const text = this.textarea.value;
                let l = 0;
                let pos = 0;
                while (l < line && pos < text.length) {
                    if (text[pos] === "\n")
                        l++;
                    pos++;
                }
                return pos + column;
            }
            /**
             * Sets the textarea caret position without triggering the browser's
             * native caret-scrolling side-effect. The textarea is visually moved
             * to the cursor line via transform in renderCaret(), so letting the
             * browser scroll the container to bring the caret into view would
             * double-count the line offset and scroll to the wrong position.
             */
            setTextareaSelection(start, end) {
                const scrollTop = this.scrollContainer.scrollTop;
                const scrollLeft = this.scrollContainer.scrollLeft;
                this._suppressCaretScroll = true;
                try {
                    this.textarea.focus({ preventScroll: true });
                    this.textarea.selectionStart = start;
                    this.textarea.selectionEnd = end !== undefined ? end : start;
                }
                finally {
                    // Undo any synchronous scroll the browser performed.
                    this.scrollContainer.scrollTop = scrollTop;
                    this.scrollContainer.scrollLeft = scrollLeft;
                }
                // Caret scrolling may be applied asynchronously on the next frame.
                // Restore again, then release the flag (order matters: restore
                // first while still suppressed, then clear the flag).
                requestAnimationFrame(() => {
                    this.scrollContainer.scrollTop = scrollTop;
                    this.scrollContainer.scrollLeft = scrollLeft;
                    this._suppressCaretScroll = false;
                });
            }
            updateCaretFromTextarea() {
                const pos = this.textarea.selectionStart;
                this.cursor.setPosition(this.textareaToBufferPos(pos));
                this.render();
                this.fireCursorChange();
            }
            handleKeyDown(e) {
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
                    this.setTextareaSelection(start + insertStr.length);
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
            handleCodeViewClick(e) {
                const rect = this.codeView.getBoundingClientRect();
                const x = e.clientX - rect.left + this.codeView.scrollLeft;
                const y = e.clientY - rect.top + this.codeView.scrollTop;
                const line = Math.floor(y / this.lineHeight);
                const column = Math.floor(x / this.charWidth);
                if (line >= 0 && line < this.buffer.lineCount) {
                    const lineText = this.buffer.getLine(line);
                    const clampedCol = Math.min(column, lineText.length);
                    const pos = this.bufferToTextareaPos(line, clampedCol);
                    this.setTextareaSelection(pos);
                    this.cursor.setPosition({ line, column: clampedCol });
                    this.render();
                    this.fireCursorChange();
                }
            }
            handleGutterClick(e) {
                const target = e.target;
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
                    this.setTextareaSelection(startPos, endPos);
                    this.render();
                    this.fireCursorChange();
                }
            }
            // ---- Public API ----
            getText() {
                return this.buffer.getText();
            }
            setText(text, filename) {
                this.buffer.setText(text);
                if (filename) {
                    try {
                        let decode_name = JSON.parse(filename);
                        filename = decode_name;
                    }
                    catch (ex) {
                        // just do nothing
                    }
                    this.setFilename(filename);
                }
                this.diffViewer.setOriginal(text);
                this.diffViewer.setCurrent(text);
                this.collapsedLines.clear();
                this.cursor.setPosition({ line: 0, column: 0 });
                this.textarea.value = text;
                this.setTextareaSelection(0);
                this.render();
                this.fireCursorChange();
            }
            setFilename(filename) {
                this.filename = filename;
                const h = CodeEditor.Highlighters.HighlighterRegistry.detectFromFilename(filename);
                if (h) {
                    this.setLanguage(h.language);
                }
            }
            getFilename() {
                return this.filename;
            }
            setLanguage(language) {
                this.language = language;
                this.currentHighlighter = CodeEditor.Highlighters.HighlighterRegistry.get(language);
                this.highlighter.setHighlighter(this.currentHighlighter);
                this.highlighter.invalidateAll();
                this.minimapDirty = true;
                this.recomputeFolds();
                this.recomputeSymbols();
                this.render();
            }
            getLanguage() {
                return this.language;
            }
            setTheme(theme) {
                document.body.setAttribute("data-theme", theme);
            }
            getTheme() {
                return document.body.getAttribute("data-theme");
            }
            onChange(cb) {
                this.onChangeCallbacks.push(cb);
            }
            onCursorChange(cb) {
                this.onCursorChangeCallbacks.push(cb);
            }
            fireChange() {
                for (const cb of this.onChangeCallbacks)
                    cb();
            }
            fireCursorChange() {
                for (const cb of this.onCursorChangeCallbacks)
                    cb();
            }
            // ---- Folding ----
            recomputeFolds() {
                this.foldRanges = this.folder.computeFoldRanges(this.buffer.getLines(), this.language);
            }
            toggleFold(line) {
                if (this.collapsedLines.has(line)) {
                    this.collapsedLines.delete(line);
                }
                else {
                    this.collapsedLines.add(line);
                }
                this.minimapDirty = true;
                this.render();
            }
            isLineCollapsed(line) {
                return this.collapsedLines.has(line);
            }
            isLineHiddenByFold(line) {
                for (const startLine of this.collapsedLines) {
                    const range = this.foldRanges.find(r => r.startLine === startLine);
                    if (range && line > range.startLine && line <= range.endLine) {
                        return true;
                    }
                }
                return false;
            }
            // ---- Symbols ----
            recomputeSymbols() {
                this.symbols = this.symbolNav.extractSymbols(this.buffer.getLines(), this.language);
            }
            getSymbols() {
                return this.symbols;
            }
            getSymbolTree() {
                return this.symbolNav.buildSymbolTree(this.symbols, this.language);
            }
            goToSymbol(symbol) {
                this.cursor.setPosition({ line: symbol.line, column: symbol.column });
                const pos = this.bufferToTextareaPos(symbol.line, symbol.column);
                // Scroll first so that setTextareaSelection() captures the already
                // scrolled position. Otherwise its async scroll-restore (rAF)
                // would undo the jump and revert to the previous viewport.
                this.scrollToLine(symbol.line);
                this.setTextareaSelection(pos);
                this.render();
                this.fireCursorChange();
            }
            // ---- Diff ----
            getDiffViewer() {
                return this.diffViewer;
            }
            toggleDiffView() {
                const event = new CustomEvent("editor:toggleDiff");
                this.container.dispatchEvent(event);
            }
            // ---- Completion ----
            getCompletionProvider() {
                return this.completionProvider;
            }
            maybeTriggerCompletion() {
                const pos = this.cursor.position;
                const lineText = this.buffer.getLine(pos.line);
                const before = lineText.substring(0, pos.column);
                // Trigger when user types a letter or dot after an identifier.
                const triggerMatch = /[A-Za-z_][A-Za-z0-9_]*\.?$/;
                if (triggerMatch.test(before) && before.length >= 2) {
                    this.triggerCompletion();
                }
                else {
                    this.hideCompletion();
                }
            }
            async triggerCompletion() {
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
                    this.completionItems = this.completionItems.filter(item => item.label.toLowerCase().startsWith(currentWord.toLowerCase()));
                }
                this.completionIndex = 0;
                if (this.completionItems.length > 0) {
                    this.renderCompletion();
                }
                else {
                    this.hideCompletion();
                }
            }
            renderCompletion() {
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
                        `<span class="completion-label">${CodeEditor.Utils.escapeHtml(item.label)}</span>` +
                        (item.detail ? `<span class="completion-detail">${CodeEditor.Utils.escapeHtml(item.detail)}</span>` : "") +
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
            kindIcon(kind) {
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
            hideCompletion() {
                this.completionActive = false;
                this.completionPopup.style.display = "none";
            }
            acceptCompletion() {
                if (!this.completionActive || this.completionItems.length === 0)
                    return;
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
                this.setTextareaSelection(taPos);
                this.hideCompletion();
                this.render();
                this.fireCursorChange();
            }
            // ---- Go to line ----
            getGoToLine() {
                return this.goToLine;
            }
            openGoToLineDialog() {
                const event = new CustomEvent("editor:gotoLine");
                this.container.dispatchEvent(event);
            }
            goToLineNumber(line) {
                const zeroBased = Math.max(0, Math.min(line - 1, this.buffer.lineCount - 1));
                this.cursor.setPosition({ line: zeroBased, column: 0 });
                const pos = this.bufferToTextareaPos(zeroBased, 0);
                // Scroll first (see goToSymbol for the reason).
                this.scrollToLine(zeroBased);
                this.setTextareaSelection(pos);
                this.render();
                this.fireCursorChange();
            }
            scrollToLine(line) {
                const targetTop = line * this.lineHeight;
                const viewTop = this.scrollContainer.scrollTop;
                const viewHeight = this.scrollContainer.clientHeight;
                if (targetTop < viewTop) {
                    this.scrollContainer.scrollTop = targetTop;
                }
                else if (targetTop > viewTop + viewHeight - this.lineHeight * 2) {
                    this.scrollContainer.scrollTop = targetTop - viewHeight + this.lineHeight * 2;
                }
            }
            // ---- Export ----
            exportFile() {
                const event = new CustomEvent("editor:export");
                this.container.dispatchEvent(event);
            }
            // ---- Rendering ----
            render() {
                this.renderGutter();
                this.renderCodeView();
                this.renderCaret();
                this.renderMinimap();
            }
            // ---- Minimap (right-side code thumbnail / scrollbar proxy) ----
            setMinimapVisible(visible) {
                this.minimapVisible = visible;
                this.container.classList.toggle("show-minimap", visible);
                if (visible) {
                    this.minimapDirty = true;
                    this.renderMinimap();
                }
            }
            toggleMinimap() {
                this.setMinimapVisible(!this.minimapVisible);
            }
            isMinimapVisible() {
                return this.minimapVisible;
            }
            renderMinimap() {
                if (!this.minimapVisible)
                    return;
                if (this.minimapDirty) {
                    this.renderMinimapContent();
                    this.minimapDirty = false;
                }
                this.updateMinimapViewport();
            }
            renderMinimapContent() {
                const lineCount = this.buffer.lineCount;
                const parts = [];
                for (let i = 0; i < lineCount; i++) {
                    if (this.isLineHiddenByFold(i))
                        continue;
                    const lineText = this.buffer.getLine(i);
                    const lineHtml = this.renderLine(i, lineText);
                    parts.push(`<div class="minimap-line">${lineHtml}</div>`);
                }
                this.minimapContent.innerHTML = parts.join("");
                // Fixed small scale: every line keeps the same tiny height/font so the
                // minimap always reads as a thumbnail, regardless of document size.
                const lineH = this.minimapLineHeight;
                const lineEls = this.minimapContent.querySelectorAll(".minimap-line");
                lineEls.forEach((el) => {
                    el.style.height = lineH + "px";
                });
            }
            updateMinimapViewport() {
                const lineH = this.minimapLineHeight;
                const mmH = this.minimap.clientHeight;
                // Scroll the thumbnail in sync with the editor so the minimap always
                // shows the lines currently in view.
                const contentTop = (this.scrollContainer.scrollTop / this.lineHeight) * lineH;
                this.minimapContent.style.transform = `translateY(${-contentTop}px)`;
                // Proportional indicator (scrollbar thumb) showing the overall position.
                const sh = this.scrollContainer.scrollHeight;
                const ch = this.scrollContainer.clientHeight;
                const top = sh > 0 ? (this.scrollContainer.scrollTop / sh) * mmH : 0;
                const height = sh > 0 ? (ch / sh) * mmH : mmH;
                this.minimapViewport.style.top = top + "px";
                this.minimapViewport.style.height = Math.max(4, height) + "px";
            }
            scrollFromMinimap(e) {
                if (!this.minimapVisible)
                    return;
                const rect = this.minimap.getBoundingClientRect();
                let y = e.clientY - rect.top;
                y = Math.max(0, Math.min(y, rect.height));
                const fraction = y / rect.height;
                const sh = this.scrollContainer.scrollHeight;
                const ch = this.scrollContainer.clientHeight;
                const maxScroll = Math.max(0, sh - ch);
                // Center the clicked point under the cursor, like dragging a thumb.
                const target = fraction * sh - ch / 2;
                this.scrollContainer.scrollTop = Math.max(0, Math.min(target, maxScroll));
            }
            renderGutter() {
                const lineCount = this.buffer.lineCount;
                const parts = [];
                const maxNumWidth = String(lineCount).length;
                for (let i = 0; i < lineCount; i++) {
                    if (this.isLineHiddenByFold(i))
                        continue;
                    const num = i + 1;
                    const foldRange = this.foldRanges.find(r => r.startLine === i);
                    const isCollapsed = this.collapsedLines.has(i);
                    let foldMarker = "";
                    if (foldRange) {
                        foldMarker = `<span class="fold-marker ${isCollapsed ? "collapsed" : "expanded"}" data-line="${i}">${isCollapsed ? "+" : "−"}</span>`;
                    }
                    else {
                        foldMarker = `<span class="fold-spacer"></span>`;
                    }
                    const numStr = String(num).padStart(maxNumWidth, " ");
                    parts.push(`<div class="gutter-line">` +
                        `<span class="line-number" data-line="${i}">${numStr}</span>` +
                        foldMarker +
                        `</div>`);
                }
                this.gutter.innerHTML = parts.join("");
            }
            renderCodeView() {
                const lineCount = this.buffer.lineCount;
                const parts = [];
                const cursorLine = this.cursor.position.line;
                for (let i = 0; i < lineCount; i++) {
                    if (this.isLineHiddenByFold(i))
                        continue;
                    const lineText = this.buffer.getLine(i);
                    const isCursorLine = i === cursorLine;
                    const lineHtml = this.renderLine(i, lineText);
                    parts.push(`<div class="code-line${isCursorLine ? " cursor-line" : ""}" data-line="${i}">${lineHtml}</div>`);
                }
                this.codeView.innerHTML = parts.join("");
            }
            renderLine(line, text) {
                const tokens = this.highlighter.getTokens(line, text);
                const parts = [];
                for (const t of tokens) {
                    const cls = this.tokenClass(t.type);
                    const escaped = CodeEditor.Utils.escapeHtml(t.value);
                    if (cls) {
                        parts.push(`<span class="${cls}">${escaped}</span>`);
                    }
                    else {
                        parts.push(escaped);
                    }
                }
                // Ensure line has some height even when empty.
                if (parts.length === 0) {
                    parts.push("&nbsp;");
                }
                return parts.join("");
            }
            tokenClass(type) {
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
                    case TokenType.PrimitiveFunction: return "tok-primitive";
                    case TokenType.StatementTerminator: return "tok-stmtterminator";
                    case TokenType.Error: return "tok-error";
                    case TokenType.Regex: return "tok-regex";
                    case TokenType.TemplateString: return "tok-template";
                    case TokenType.TemplateDelimiter: return "tok-templatedelim";
                    case TokenType.Decorator: return "tok-decorator";
                    case TokenType.Selector: return "tok-selector";
                    case TokenType.PseudoClass: return "tok-pseudo";
                    case TokenType.Unit: return "tok-unit";
                    case TokenType.ColorValue: return "tok-colorvalue";
                    case TokenType.AtRule: return "tok-atrule";
                    case TokenType.Variable: return "tok-variable";
                    case TokenType.Builtin: return "tok-builtin";
                    case TokenType.TypeParameter: return "tok-typeparam";
                    default: return "";
                }
            }
            renderCaret() {
                // The caret is rendered by the textarea itself (we keep it focused).
                // Position the textarea so its caret aligns with the code view.
                const pos = this.cursor.position;
                const top = pos.line * this.lineHeight;
                const left = pos.column * this.charWidth;
                this.textarea.style.transform = `translate(${left}px, ${top}px)`;
            }
            // ---- Focus ----
            focus() {
                this.textarea.focus();
            }
            getCursor() {
                return this.cursor;
            }
            getBuffer() {
                return this.buffer;
            }
        }
        Core.Editor = Editor;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
var CodeEditor;
(function (CodeEditor) {
    var Editor = CodeEditor.Core.Editor;
    var HighlighterRegistry = CodeEditor.Highlighters.HighlighterRegistry;
    var SymbolKind = CodeEditor.Features.SymbolKind;
    CodeEditor.sample_vb = `
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
                case "vbnet": return "VisualBasic.NET";
                case "r": return "R";
                case "json": return "JSON";
                case "xml": return "XML";
                case "markdown": return "Markdown";
                case "yaml": return "YAML";
                case "javascript": return "JavaScript";
                case "typescript": return "TypeScript";
                case "css": return "CSS";
                case "html": return "HTML";
                default: return lang;
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
            }
            else {
                this.setTheme("light");
            }
        }
        toggleMinimap() {
            this.editor.toggleMinimap();
        }
        attachEvents() {
            // File load.
            document.getElementById("btn-open").addEventListener("click", () => {
                this.fileInput.click();
            });
            this.fileInput.addEventListener("change", (e) => {
                var _a;
                const file = (_a = e.target.files) === null || _a === void 0 ? void 0 : _a[0];
                if (file)
                    this.loadFile(file);
            });
            // Export (save button).
            document.getElementById("btn-save").addEventListener("click", () => {
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
                this.editor.setTheme(this.themeSelect.value);
            });
            this.editor.setTheme("light");
            // Toggle symbols panel.
            document.getElementById("btn-toggle-symbols").addEventListener("click", () => {
                const panel = document.getElementById("symbol-sidebar");
                panel.classList.toggle("hidden");
                if (!panel.classList.contains("hidden")) {
                    this.refreshSymbols();
                }
            });
            // Toggle minimap (right-side code thumbnail / scrollbar proxy).
            const minimapBtn = document.getElementById("btn-toggle-minimap");
            minimapBtn.addEventListener("click", () => {
                this.editor.toggleMinimap();
                minimapBtn.classList.toggle("active", this.editor.isMinimapVisible());
            });
            // Delegated handler for the symbols tree: collapse/expand nodes and
            // navigate when a symbol row is clicked.
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
            // Toggle diff view.
            document.getElementById("btn-toggle-diff").addEventListener("click", () => {
                this.toggleDiffView();
            });
            // Go to line.
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
            // Go to line dialog buttons.
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
                }
                else if (e.key === "Escape") {
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
            document.getElementById("btn-test-completion").addEventListener("click", async () => {
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
        loadSampleContent() {
            this.editor.setText(CodeEditor.sample_vb, "sample.vb");
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
            const toggle = hasChildren
                ? `<span class="symbol-toggle symbol-toggle-expandable" title="Collapse / expand">&#9662;</span>`
                : `<span class="symbol-toggle symbol-toggle-leaf"></span>`;
            const childrenHtml = hasChildren
                ? `<div class="symbol-children">` +
                    this.renderSymbolNodes(node.children, depth + 1) +
                    `</div>`
                : "";
            const indent = depth * 16 + 10;
            return (`<div class="symbol-node">` +
                `<div class="symbol-item" data-line="${sym.line}" data-col="${sym.column}" ` +
                `data-kind="${sym.kind}" style="padding-left:${indent}px">` +
                toggle +
                `<span class="symbol-icon symbol-${sym.kind.toLowerCase()}">${icon}</span>` +
                `<span class="symbol-name">${CodeEditor.Utils.escapeHtml(sym.name)}</span>` +
                `<span class="symbol-kind">${sym.kind}</span>` +
                `<span class="symbol-line">:${sym.line + 1}</span>` +
                `</div>` +
                childrenHtml +
                `</div>`);
        }
        symbolIcon(kind) {
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
        toggleDiffView() {
            this.diffVisible = !this.diffVisible;
            if (this.diffVisible) {
                this.diffPanel.classList.remove("hidden");
                this.renderDiff();
            }
            else {
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
            }
            catch (e) {
                this.completionStatus.textContent = `Error: ${e.message}`;
            }
            setTimeout(() => { this.completionStatus.textContent = ""; }, 4000);
        }
        updateStatus() {
            const cursor = this.editor.getCursor().position;
            this.statusLine.textContent = String(cursor.line + 1);
            this.statusCol.textContent = String(cursor.column + 1);
            this.statusLang.textContent = this.editor.getLanguage();
            this.statusFile.textContent = this.editor.getFilename();
            if (CodeEditor.devkit) {
                CodeEditor.devkit.updateStatus(this.statusLine.textContent, this.statusCol.textContent, this.statusLang.textContent, this.statusFile.textContent);
            }
        }
    }
    CodeEditor.App = App;
    // Bootstrap.
    function bootstrap() {
        return new App();
    }
    CodeEditor.bootstrap = bootstrap;
})(CodeEditor || (CodeEditor = {}));
window.addEventListener("DOMContentLoaded", () => {
    const codeEditor = CodeEditor.bootstrap();
    // 监听来自 WinForm 的消息
    window.codeEditor = codeEditor;
    window.chrome.webview.addEventListener('message', function (event) {
        const message = event.data;
        if (message.type === 'loadFile') {
            // 直接使用传递过来的 text 和 filename
            codeEditor.loadFileText(JSON.parse(message.text), message.filename);
        }
    });
});
var CodeEditor;
(function (CodeEditor) {
    CodeEditor.devkit = (function () {
        try {
            return chrome.webview.hostObjects.devkit;
        }
        catch (ex) {
            return null;
        }
    })();
})(CodeEditor || (CodeEditor = {}));
//# sourceMappingURL=editor.bundle.js.map