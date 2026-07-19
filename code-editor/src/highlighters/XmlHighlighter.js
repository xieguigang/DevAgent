"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = Utils.TokenType;
        var TokenBuilder = Utils.TokenBuilder;
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
