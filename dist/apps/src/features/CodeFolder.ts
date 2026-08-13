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

        private computeVbNet(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            // Stack entry records the opener line, normalized text used for
            // matching closers, a display kind, and (for If blocks) the list
            // of Else/ElseIf branch start lines so each branch can be folded
            // independently.
            const stack: { line: number; text: string; kind: string; elseStarts: number[] }[] = [];

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
            const closeIfBlock = (top: { line: number; kind: string; elseStarts: number[] }, endLine: number): void => {
                if (endLine <= top.line) return;
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
                if (codePart.length === 0) continue;

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
                                } else {
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
                                } else {
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
                    if (matched === "for each") { text = "for"; kind = "for each"; }
                    if (matched === "select case") { text = "select"; kind = "select"; }
                    stack.push({ line: i, text, kind, elseStarts: [] });
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

        /**
         * Brace-based folding for C-style languages (JS/TS/CSS).
         * Handles // and /* *‌/ comments, strings, and template literals.
         * Unlike computeBraceBased, does NOT treat # as a comment start.
         */
        private computeCStyleBraces(lines: string[]): FoldRange[] {
            const ranges: FoldRange[] = [];
            const stack: { line: number; col: number }[] = [];
            let inBlockComment = false;

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                let inString: string | null = null;
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
                    if (inLineComment) continue;
                    if (inString) {
                        if (c === "\\") { j++; continue; }
                        if (c === inString) inString = null;
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
                // Line comment and string state reset at end of line.
                // Block comment state persists across lines.
            }

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
