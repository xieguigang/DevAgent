namespace CodeEditor.Features {
    /**
     * A foldable region in the document.
     */
    export interface FoldRange {
        startLine: number;
        endLine: number;
        /** Display text shown when collapsed, e.g. "{...}" or "...". */
        collapsedText: string;
        /** Optional kind label for grouping. */
        kind: string;
    }

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
    export class CodeFolder {
        /**
         * Compute fold ranges for the given document.
         */
        computeFoldRanges(lines: string[], language: string): FoldRange[] {
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

        private computeVbNet(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            const stack: { line: number; text: string }[] = [];

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

        private computeBraceBased(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            const stack: { line: number; col: number }[] = [];

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                let inString: string | null = null;
                let inComment = false;
                for (let j = 0; j < line.length; j++) {
                    const c = line[j];
                    if (inComment) continue;
                    if (inString) {
                        if (c === "\\") { j++; continue; }
                        if (c === inString) inString = null;
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
                    } else if (c === "}" || c === ")" || c === "]") {
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

        private computeXml(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            const stack: { line: number; tag: string }[] = [];

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                // Find tags.
                const tagRegex = /<\/?([A-Za-z_][\w\-.:]*)\b[^>]*?(\/?)>/g;
                let m: RegExpExecArray | null;
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
                    } else if (!isSelfClose) {
                        stack.push({ line: i, tag });
                    }
                }
            }
            return ranges;
        }

        private computeMarkdown(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            let currentHeading: { line: number; level: number } | null = null;

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

        private computeIndentation(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            const indents = lines.map(l => {
                const m = /^(\s*)/.exec(l);
                return m ? m[1].length : 0;
            });

            for (let i = 0; i < lines.length - 1; i++) {
                if (lines[i].trim().length === 0) continue;
                const curIndent = indents[i];
                // Find next non-blank line.
                let j = i + 1;
                while (j < lines.length && lines[j].trim().length === 0) j++;
                if (j >= lines.length) continue;
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

        private dedupeRanges(ranges: FoldRange[]): FoldRange[] {
            // Keep only the outermost range for each start line.
            const byStart = new Map<number, FoldRange>();
            for (const r of ranges) {
                const existing = byStart.get(r.startLine);
                if (!existing || r.endLine > existing.endLine) {
                    byStart.set(r.startLine, r);
                }
            }
            return Array.from(byStart.values()).sort((a, b) => a.startLine - b.startLine);
        }
    }
}
