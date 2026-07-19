"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Computes a line-level diff between two text documents using the
         * classic dynamic-programming Longest Common Subsequence algorithm.
         * The result is a list of DiffLine entries that can be rendered
         * directly by the DiffViewer.
         */
        function computeLineDiff(oldText, newText) {
            const oldLines = oldText.length === 0 ? [] : oldText.split(/\r\n|\r|\n/);
            const newLines = newText.length === 0 ? [] : newText.split(/\r\n|\r|\n/);
            const m = oldLines.length;
            const n = newLines.length;
            // dp[i][j] = length of LCS of oldLines[i..] and newLines[j..]
            const dp = Array.from({ length: m + 1 }, () => new Array(n + 1).fill(0));
            for (let i = m - 1; i >= 0; i--) {
                for (let j = n - 1; j >= 0; j--) {
                    if (oldLines[i] === newLines[j]) {
                        dp[i][j] = dp[i + 1][j + 1] + 1;
                    }
                    else {
                        dp[i][j] = Math.max(dp[i + 1][j], dp[i][j + 1]);
                    }
                }
            }
            // Backtrack to build the diff.
            const result = [];
            let i = 0;
            let j = 0;
            let oldNum = 1;
            let newNum = 1;
            while (i < m && j < n) {
                if (oldLines[i] === newLines[j]) {
                    result.push({ type: "equal", oldLineNumber: oldNum++, newLineNumber: newNum++, content: oldLines[i] });
                    i++;
                    j++;
                }
                else if (dp[i + 1][j] >= dp[i][j + 1]) {
                    result.push({ type: "removed", oldLineNumber: oldNum++, newLineNumber: 0, content: oldLines[i] });
                    i++;
                }
                else {
                    result.push({ type: "added", oldLineNumber: 0, newLineNumber: newNum++, content: newLines[j] });
                    j++;
                }
            }
            while (i < m) {
                result.push({ type: "removed", oldLineNumber: oldNum++, newLineNumber: 0, content: oldLines[i] });
                i++;
            }
            while (j < n) {
                result.push({ type: "added", oldLineNumber: 0, newLineNumber: newNum++, content: newLines[j] });
                j++;
            }
            return result;
        }
        Utils.computeLineDiff = computeLineDiff;
        /**
         * Summarize a diff: number of added / removed lines.
         */
        function summarizeDiff(diff) {
            let added = 0;
            let removed = 0;
            for (const d of diff) {
                if (d.type === "added")
                    added++;
                else if (d.type === "removed")
                    removed++;
            }
            return { added, removed };
        }
        Utils.summarizeDiff = summarizeDiff;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
