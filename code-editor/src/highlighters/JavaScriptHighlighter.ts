namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

    /**
     * Tokenizer state carried across lines for JavaScript.
     * All fields are plain data so the state is serialisable.
     */
    export interface JsState {
        /** Inside a multi-line block comment. */
        inBlockComment: boolean;
        /** Inside a JSDoc block comment (/** ... *‌/). */
        inDocComment: boolean;
        /** Inside a single/double-quoted string that spans a line via \-continuation. */
        inString: boolean;
        /** The quote character of the ongoing string. */
        stringQuote: string;
        /** Inside the text portion of a template literal (between backticks, outside ${}). */
        inTemplate: boolean;
        /** Brace-depth values that would close each open ${} interpolation. */
        templateStack: number[];
        /** Current brace depth, used to match ${ } closers. */
        braceDepth: number;
        /** Last significant token class for regex / division disambiguation. */
        lastSignificant: "value" | "operator" | "none";
    }

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
    export class JavaScriptHighlighter implements ILanguageHighlighter {
        readonly language = "javascript";

        protected static readonly CONTROL_KEYWORDS = new Set<string>([
            "if", "else", "for", "while", "do", "switch", "case", "break",
            "continue", "return", "throw", "try", "catch", "finally",
            "yield", "await", "default"
        ]);

        protected static readonly KEYWORDS = new Set<string>([
            "var", "let", "const", "function", "class", "extends", "new",
            "delete", "typeof", "instanceof", "in", "of", "void", "import",
            "export", "from", "as", "static", "get", "set", "async", "with",
            "debugger"
        ]);

        /** Keywords that behave syntactically like values. */
        protected static readonly VALUE_KEYWORDS = new Set<string>([
            "true", "false", "null", "undefined", "this", "super", "NaN",
            "Infinity"
        ]);

        protected static readonly BUILTINS = new Set<string>([
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

        initialState(): JsState {
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
        protected isControlKeyword(word: string): boolean {
            return JavaScriptHighlighter.CONTROL_KEYWORDS.has(word);
        }

        protected isKeyword(word: string): boolean {
            return JavaScriptHighlighter.KEYWORDS.has(word);
        }

        protected isValueKeyword(word: string): boolean {
            return JavaScriptHighlighter.VALUE_KEYWORDS.has(word);
        }

        protected isBuiltin(word: string): boolean {
            return JavaScriptHighlighter.BUILTINS.has(word);
        }

        /** Whether the word is a built-in type (overridden by TS). */
        protected isType(word: string): boolean {
            return false;
        }

        /** Whether decorators (@name) should be parsed (enabled in TS). */
        protected parseDecorators(): boolean {
            return false;
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
            const b = new TokenBuilder();
            let i = 0;
            const n = line.length;
            let s: JsState = state ? state : this.initialState();
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
                    if (line[j] === "\\" && j + 1 < n) { j += 2; continue; }
                    if (line[j] === q) { j++; break; }
                    j++;
                }
                b.push(TokenType.String, line.substring(0, j));
                i = j;
                if (j < n) {
                    s.inString = false;
                    s.stringQuote = "";
                    s.lastSignificant = "value";
                } else {
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
                    while (j < n && (line[j] === " " || line[j] === "\t" || line[j] === "\r")) j++;
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
                    while (j < n && /[A-Za-z0-9_]/.test(line[j])) j++;
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
                    while (j < n && this.isIdentPart(line[j])) j++;
                    const word = line.substring(i, j);
                    i = j;

                    if (this.isControlKeyword(word)) {
                        b.push(TokenType.ControlKeyword, word);
                        s.lastSignificant = "operator";
                    } else if (this.isKeyword(word)) {
                        b.push(TokenType.Keyword, word);
                        s.lastSignificant = "operator";
                    } else if (this.isValueKeyword(word)) {
                        b.push(TokenType.Constant, word);
                        s.lastSignificant = "value";
                    } else if (this.isType(word)) {
                        b.push(TokenType.Type, word);
                        s.lastSignificant = "value";
                    } else if (this.isBuiltin(word)) {
                        b.push(TokenType.Builtin, word);
                        s.lastSignificant = "value";
                    } else {
                        // Check if this is a function call: identifier followed by (
                        let k = i;
                        while (k < n && (line[k] === " " || line[k] === "\t")) k++;
                        if (line[k] === "(") {
                            b.push(TokenType.Function, word);
                        } else if (k < n && line[k] === "." ) {
                            // Property access — keep as identifier
                            b.push(TokenType.Identifier, word);
                        } else {
                            // Capitalized identifier → heuristic type/class name
                            if (word.length > 0 && word[0] >= "A" && word[0] <= "Z") {
                                b.push(TokenType.Type, word);
                            } else {
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
                    while (j < n && this.isOperatorChar(line[j])) j++;
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

        protected isIdentStart(ch: string): boolean {
            return (ch >= "a" && ch <= "z") || (ch >= "A" && ch <= "Z") || ch === "_" || ch === "$";
        }

        protected isIdentPart(ch: string): boolean {
            return this.isIdentStart(ch) || (ch >= "0" && ch <= "9");
        }

        protected isOperatorChar(ch: string): boolean {
            return "+-*/%=<>!&|~^?".indexOf(ch) >= 0;
        }

        protected isNumberStart(line: string, i: number, n: number): boolean {
            const ch = line[i];
            if (ch >= "0" && ch <= "9") return true;
            if (ch === ".") {
                // Only if followed by a digit (otherwise it's member access).
                return i + 1 < n && line[i + 1] >= "0" && line[i + 1] <= "9";
            }
            return false;
        }

        protected scanNumber(line: string, i: number, n: number): number {
            let j = i;
            // Hex / binary / octal prefix.
            if (line[i] === "0" && i + 1 < n) {
                const p = line[i + 1];
                if (p === "x" || p === "X") {
                    j = i + 2;
                    while (j < n && /[0-9A-Fa-f_]/.test(line[j])) j++;
                    if (j < n && line[j] === "n") j++; // BigInt
                    return j;
                }
                if (p === "b" || p === "B") {
                    j = i + 2;
                    while (j < n && /[01_]/.test(line[j])) j++;
                    if (j < n && line[j] === "n") j++;
                    return j;
                }
                if (p === "o" || p === "O") {
                    j = i + 2;
                    while (j < n && /[0-7_]/.test(line[j])) j++;
                    if (j < n && line[j] === "n") j++;
                    return j;
                }
            }
            // Decimal / float.
            while (j < n && /[0-9_]/.test(line[j])) j++;
            if (j < n && line[j] === ".") {
                j++;
                while (j < n && /[0-9_]/.test(line[j])) j++;
            }
            // Exponent.
            if (j < n && (line[j] === "e" || line[j] === "E")) {
                j++;
                if (j < n && (line[j] === "+" || line[j] === "-")) j++;
                while (j < n && /[0-9_]/.test(line[j])) j++;
            }
            // BigInt suffix.
            if (j < n && line[j] === "n") j++;
            return j;
        }

        /**
         * Scan a regex literal starting at `i` (the opening `/`).
         * Returns the index past the closing `/` (and any flags), or `i`
         * if this doesn't look like a regex.
         */
        protected scanRegex(line: string, i: number, n: number): number {
            let j = i + 1; // skip opening /
            let inClass = false;
            while (j < n) {
                const c = line[j];
                if (c === "\\" && j + 1 < n) {
                    j += 2;
                    continue;
                }
                if (c === "[") { inClass = true; j++; continue; }
                if (c === "]" && inClass) { inClass = false; j++; continue; }
                if (c === "/" && !inClass) {
                    j++;
                    // Consume flags.
                    while (j < n && /[gimsuyd]/.test(line[j])) j++;
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
        protected scanString(
            line: string, i: number, n: number, quote: string,
            b: TokenBuilder
        ): { i: number; continues: boolean } {
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
        protected scanTemplateText(
            line: string, i: number, n: number, s: JsState, b: TokenBuilder
        ): { i: number; state: JsState } {
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
}
