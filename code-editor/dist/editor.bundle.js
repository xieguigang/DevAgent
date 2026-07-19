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
                return { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0 };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Handle multi-line block comment continuation (rare in VB but supported via XML doc).
                // VB.NET does not have block comments, but we keep state for future use.
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
                    // String literal.
                    if (ch === '"') {
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === '"') {
                                if (line[j + 1] === '"') {
                                    j += 2;
                                    continue;
                                }
                                j++;
                                break;
                            }
                            j++;
                        }
                        // Check for char literal: "a"c
                        if (j < n && line[j] === "c" || (j === n && line[n - 1] === '"')) {
                            // Check if next non-space char is 'c'
                        }
                        const str = line.substring(i, j);
                        b.push(TokenType.String, str);
                        i = j;
                        continue;
                    }
                    // Interpolated string: $"..." (basic, single-line).
                    if ((ch === "$" && line[i + 1] === '"') || (ch === "$" && line[i + 1] === "@" && line[i + 2] === '"')) {
                        const startChar = line[i + 1] === "@" ? 2 : 1;
                        let j = i + 1 + startChar;
                        while (j < n) {
                            if (line[j] === '"') {
                                if (line[j + 1] === '"') {
                                    j += 2;
                                    continue;
                                }
                                j++;
                                break;
                            }
                            j++;
                        }
                        const str = line.substring(i, j);
                        b.push(TokenType.String, str);
                        i = j;
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
         */
        class RHighlighter {
            constructor() {
                this.language = "r";
            }
            initialState() {
                return {};
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
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
                    // Raw string: r"(...)", r"[...]", r"{...}".
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
                            continue;
                        }
                    }
                    // Double-quoted string.
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
                        continue;
                    }
                    // Single-quoted string.
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
                        b.push(TokenType.Operator, line.substring(i, j));
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
                        b.push(TokenType.Operator, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    if (/[+\-*/^<>=!&|~$@?:]/.test(ch)) {
                        let j = i;
                        while (j < n && /[+\-*/^<>=!&|~$@?:]/.test(line[j]))
                            j++;
                        b.push(TokenType.Operator, line.substring(i, j));
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
                    // Punctuation.
                    if (/[(){}\[\],;]/.test(ch)) {
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
                if (dotIdx < 0)
                    return null;
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
                    case "xml":
                        return this.computeXml(lines);
                    case "markdown":
                        return this.computeMarkdown(lines);
                    default:
                        return this.computeIndentation(lines);
                }
            }
            computeVbNet(lines) {
                const ranges = [];
                const stack = [];
                // Patterns that open a block.
                const openers = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Get|Set|AddHandler|RemoveHandler|RaiseEvent|Using|While|For|For Each|If|Select Case|Try|SyncLock|With|Do|Region)\b/i;
                // Region is special: closed by End Region.
                const regionOpen = /#\s*Region\b/i;
                const regionClose = /#\s*End\s+Region\b/i;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const trimmed = line.trim();
                    // Skip comments and strings (basic).
                    if (trimmed.startsWith("'") || trimmed.toLowerCase().startsWith("rem ")) {
                        continue;
                    }
                    // Region markers.
                    if (regionOpen.test(trimmed)) {
                        stack.push({ line: i, text: "#Region" });
                        continue;
                    }
                    if (regionClose.test(trimmed)) {
                        const top = stack.pop();
                        if (top && top.text === "#Region" && i > top.line) {
                            ranges.push({
                                startLine: top.line,
                                endLine: i,
                                collapsedText: "...",
                                kind: "region"
                            });
                        }
                        continue;
                    }
                    // Check for opener.
                    const openMatch = openers.exec(trimmed);
                    if (openMatch) {
                        // Make sure it's not a single-line form (e.g. "If x Then Return").
                        if (openMatch[1].toLowerCase() === "if") {
                            if (/\bThen\b/i.test(trimmed) && !/\bThen\s*$/i.test(trimmed)) {
                                // Single-line If, skip.
                                continue;
                            }
                        }
                        // For Sub/Function on one line (e.g. "Sub Foo() : End Sub").
                        if (/\bEnd\s+Sub\b/i.test(trimmed) || /\bEnd\s+Function\b/i.test(trimmed)) {
                            continue;
                        }
                        stack.push({ line: i, text: openMatch[1].toLowerCase() });
                    }
                    // End statements.
                    const endMatch = /\bEnd\s+(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Get|Set|AddHandler|RemoveHandler|RaiseEvent|Using|While|For|If|Select|Try|SyncLock|With|Do)\b/i.exec(trimmed);
                    if (endMatch) {
                        const kind = endMatch[1].toLowerCase();
                        // Pop until we find a matching opener.
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === kind) {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "End " + endMatch[1] + " ...",
                                        kind: endMatch[1].toLowerCase()
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Standalone End (for If/Select/etc. without explicit kind).
                    if (/^End\s*$/i.test(trimmed)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            const t = stack[k].text;
                            if (t === "if" || t === "select" || t === "for" || t === "while" || t === "do" || t === "using" || t === "try" || t === "with" || t === "synclock") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "End ...",
                                        kind: t
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Next (closes For).
                    if (/^Next\b/i.test(trimmed)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "for") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "Next ...",
                                        kind: "for"
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // Loop (closes Do/While).
                    if (/^Loop\b/i.test(trimmed)) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "do" || stack[k].text === "while") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "Loop ...",
                                        kind: top.text
                                    });
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    // EndIf / End Select / etc. written as single word.
                    const endIfMatch = /\bEndIf\b|\bEnd\s+If\b/i.exec(trimmed);
                    if (endIfMatch) {
                        for (let k = stack.length - 1; k >= 0; k--) {
                            if (stack[k].text === "if") {
                                const top = stack.splice(k)[0];
                                if (i > top.line) {
                                    ranges.push({
                                        startLine: top.line,
                                        endLine: i,
                                        collapsedText: "End If ...",
                                        kind: "if"
                                    });
                                }
                                break;
                            }
                        }
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
                        return this.extractXml(lines);
                    case "markdown":
                        return this.extractMarkdown(lines);
                    case "yaml":
                        return this.extractYaml(lines);
                    default:
                        return [];
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
                    this.textarea.focus();
                    this.textarea.selectionStart = this.textarea.selectionEnd = pos;
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
                    this.textarea.focus();
                    this.textarea.selectionStart = startPos;
                    this.textarea.selectionEnd = endPos;
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
            goToSymbol(symbol) {
                this.cursor.setPosition({ line: symbol.line, column: symbol.column });
                const pos = this.bufferToTextareaPos(symbol.line, symbol.column);
                this.textarea.focus();
                this.textarea.selectionStart = this.textarea.selectionEnd = pos;
                this.scrollToLine(symbol.line);
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
                const top = (pos.line - this.firstVisibleLine + 1) * this.lineHeight;
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
                this.textarea.selectionStart = this.textarea.selectionEnd = taPos;
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
                this.textarea.focus();
                this.textarea.selectionStart = this.textarea.selectionEnd = pos;
                this.scrollToLine(zeroBased);
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
                    case TokenType.Error: return "tok-error";
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
                default: return lang;
            }
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
        refreshSymbols() {
            const symbols = this.editor.getSymbols();
            if (symbols.length === 0) {
                this.symbolList.innerHTML = '<div class="symbol-empty">No symbols found</div>';
                return;
            }
            const parts = [];
            for (const sym of symbols) {
                const icon = this.symbolIcon(sym.kind);
                parts.push(`<div class="symbol-item" data-line="${sym.line}" data-col="${sym.column}">` +
                    `<span class="symbol-icon symbol-${sym.kind.toLowerCase()}">${icon}</span>` +
                    `<span class="symbol-name">${CodeEditor.Utils.escapeHtml(sym.name)}</span>` +
                    `<span class="symbol-kind">${sym.kind}</span>` +
                    `<span class="symbol-line">:${sym.line + 1}</span>` +
                    `</div>`);
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
        }
    }
    CodeEditor.App = App;
    // Bootstrap.
    function bootstrap() {
        new App();
    }
    CodeEditor.bootstrap = bootstrap;
})(CodeEditor || (CodeEditor = {}));
window.addEventListener("DOMContentLoaded", () => {
    window.codeEditor = CodeEditor.bootstrap();
});
//# sourceMappingURL=editor.bundle.js.map