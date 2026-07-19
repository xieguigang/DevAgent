namespace CodeEditor.Features {
    import DiffLine = Utils.DiffLine;
    import computeLineDiff = Utils.computeLineDiff;
    import summarizeDiff = Utils.summarizeDiff;

    /**
     * Manages git-style diff visualization between the original loaded text
     * and the current edited text.
     */
    export class DiffViewer {
        private originalText: string = "";
        private currentText: string = "";
        private cachedDiff: DiffLine[] | null = null;

        setOriginal(text: string): void {
            this.originalText = text;
            this.cachedDiff = null;
        }

        setCurrent(text: string): void {
            this.currentText = text;
            this.cachedDiff = null;
        }

        getDiff(): DiffLine[] {
            if (this.cachedDiff === null) {
                this.cachedDiff = computeLineDiff(this.originalText, this.currentText);
            }
            return this.cachedDiff;
        }

        getSummary(): { added: number; removed: number } {
            return summarizeDiff(this.getDiff());
        }

        /**
         * Returns the diff line index that corresponds to the given new-line
         * number, or -1 if not found. Used to scroll the diff view to match
         * the editor caret.
         */
        findDiffLineForNewLine(newLine: number): number {
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
        renderDiffHtml(): string {
            const diff = this.getDiff();
            const parts: string[] = [];
            for (const d of diff) {
                const cls = d.type === "added" ? "diff-added" : d.type === "removed" ? "diff-removed" : "diff-equal";
                const sign = d.type === "added" ? "+" : d.type === "removed" ? "-" : " ";
                const oldNum = d.oldLineNumber > 0 ? String(d.oldLineNumber) : "";
                const newNum = d.newLineNumber > 0 ? String(d.newLineNumber) : "";
                const escaped = Utils.escapeHtml(d.content);
                parts.push(
                    `<div class="diff-line ${cls}">` +
                    `<span class="diff-oldnum">${oldNum}</span>` +
                    `<span class="diff-newnum">${newNum}</span>` +
                    `<span class="diff-sign">${sign}</span>` +
                    `<span class="diff-content">${escaped}</span>` +
                    `</div>`
                );
            }
            return parts.join("");
        }
    }
}
