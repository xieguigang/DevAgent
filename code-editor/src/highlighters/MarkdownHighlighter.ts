namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

    /**
     * Markdown syntax highlighter.
     * Handles headings, bold, italic, code, links, lists, blockquotes,
     * horizontal rules, and fenced code blocks (with state for multi-line).
     */
    export class MarkdownHighlighter implements ILanguageHighlighter {
        readonly language = "markdown";

        initialState(): any {
            return { inFence: false, fenceChar: "" };
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
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

        private tokenizeInline(text: string, b: TokenBuilder): void {
            let i = 0;
            const n = text.length;
            while (i < n) {
                const ch = text[i];

                // Inline code `code` or ``code``.
                if (ch === "`") {
                    let ticks = 0;
                    while (text[i + ticks] === "`") ticks++;
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
}
