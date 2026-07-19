"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = Utils.TokenType;
        var TokenBuilder = Utils.TokenBuilder;
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
