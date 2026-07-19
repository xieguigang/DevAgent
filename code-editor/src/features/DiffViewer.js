"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        var computeLineDiff = Utils.computeLineDiff;
        var summarizeDiff = Utils.summarizeDiff;
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
