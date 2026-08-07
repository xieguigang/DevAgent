namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

    /**
     * Tokenizer state for HTML, carried across lines.
     *
     * The `mode` field determines how the current line is parsed:
     *  - "html":   normal HTML markup
     *  - "script": inside &lt;script&gt; — delegated to JavaScriptHighlighter
     *  - "style":  inside &lt;style&gt; — delegated to CssHighlighter
     *
     * When in "script" or "style" mode, `subState` holds the sub-highlighter's
     * state object (pure data). Token offsets returned by the sub-highlighter
     * are relative to the fragment passed to it, so they must be offset by the
     * fragment's start position before merging.
     */
    export interface HtmlState {
        mode: "html" | "script" | "style";
        /** Inside a multi-line HTML comment. */
        inComment: boolean;
        /** Sub-highlighter state (for script/style delegation). */
        subState: any;
    }

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
    export class HtmlHighlighter implements ILanguageHighlighter {
        readonly language = "html";

        private readonly jsHighlighter: JavaScriptHighlighter;
        private readonly cssHighlighter: CssHighlighter;

        constructor() {
            this.jsHighlighter = new JavaScriptHighlighter();
            this.cssHighlighter = new CssHighlighter();
        }

        initialState(): HtmlState {
            return {
                mode: "html",
                inComment: false,
                subState: null
            };
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
            let s: HtmlState = state ? state : this.initialState();
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

        private tokenizeHtml(line: string, s: HtmlState): TokenizeResult {
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
                                const embedded = this.tokenizeEmbedded(
                                    line.substring(i), s, "script", "</script>", this.jsHighlighter, i
                                );
                                return { tokens: b.result.concat(embedded.tokens), state: embedded.state };
                            }
                            return { tokens: b.result, state: s };
                        }
                        if (tagNameLower === "style") {
                            s.mode = "style";
                            s.subState = this.cssHighlighter.initialState();
                            if (i < n) {
                                const embedded = this.tokenizeEmbedded(
                                    line.substring(i), s, "style", "</style>", this.cssHighlighter, i
                                );
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
                    while (j < n && line[j] !== ";" && /[A-Za-z0-9#]/.test(line[j])) j++;
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
                    while (j < n && line[j] !== "<" && line[j] !== "&") j++;
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
        private parseTag(
            line: string, i: number, n: number, b: TokenBuilder
        ): { i: number; tagName: string; isOpening: boolean; selfClosed: boolean } {
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
            while (j < n && /[A-Za-z0-9_:\-.]/.test(line[j])) j++;
            const tagName = line.substring(nameStart, j);
            if (tagName.length > 0) {
                b.push(TokenType.Tag, tagName);
            }

            // Parse attributes until '>' or '/>'.
            while (j < n && line[j] !== ">") {
                // Whitespace.
                if (line[j] === " " || line[j] === "\t" || line[j] === "\r" || line[j] === "\n") {
                    let k = j;
                    while (k < n && /\s/.test(line[k])) k++;
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
                    while (k < n && /[A-Za-z0-9_:\-.]/.test(line[k])) k++;
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
                    while (k < n && line[k] !== q) k++;
                    if (k < n) k++;
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
        private tokenizeEmbedded(
            line: string,
            s: HtmlState,
            mode: "script" | "style",
            closeTag: string,
            sub: ILanguageHighlighter,
            offset: number = 0
        ): TokenizeResult {
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
        private offsetTokens(tokens: Token[], delta: number): Token[] {
            if (delta === 0) return tokens;
            return tokens.map(t => ({
                type: t.type,
                value: t.value,
                start: t.start + delta,
                end: t.end + delta
            }));
        }
    }
}
