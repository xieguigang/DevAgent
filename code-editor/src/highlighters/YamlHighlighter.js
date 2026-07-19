"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = Utils.TokenType;
        var TokenBuilder = Utils.TokenBuilder;
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
