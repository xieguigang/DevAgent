"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        var TokenType = Utils.TokenType;
        /**
         * Manages per-line tokenization cache for the current document.
         * Re-tokenizes lines when they change, carrying state across lines.
         */
        class Highlighter {
            constructor() {
                this.highlighter = null;
                this.cache = [];
                this.dirtyFromLine = 0;
                this.buffer = null;
            }
            setHighlighter(h) {
                this.highlighter = h;
                this.cache = [];
                this.dirtyFromLine = 0;
            }
            get language() {
                return this.highlighter ? this.highlighter.language : "plain";
            }
            invalidate(fromLine) {
                if (fromLine < this.dirtyFromLine) {
                    this.dirtyFromLine = fromLine;
                }
            }
            invalidateAll() {
                this.cache = [];
                this.dirtyFromLine = 0;
            }
            /**
             * Ensure cache is valid up to and including `line`. Returns the tokens
             * for that line.
             */
            getTokens(line, lineText) {
                if (!this.highlighter) {
                    return [{ type: TokenType.Plain, value: lineText, start: 0, end: lineText.length }];
                }
                // If cache is stale, re-tokenize from dirtyFromLine forward.
                if (this.dirtyFromLine <= line) {
                    this.retokenize(Math.max(0, this.dirtyFromLine));
                }
                if (line < this.cache.length && this.cache[line]) {
                    return this.cache[line].tokens;
                }
                return [];
            }
            retokenize(fromLine) {
                if (!this.highlighter)
                    return;
                // Determine starting state.
                let state;
                if (fromLine === 0 || this.cache.length === 0) {
                    state = this.highlighter.initialState();
                    fromLine = 0;
                    this.cache = [];
                }
                else {
                    state = this.cache[fromLine - 1] ? this.cache[fromLine - 1].state : this.highlighter.initialState();
                }
                // We need the full document text to retokenize. The caller (Editor)
                // will pass lines via getTokens, but for retokenization we need
                // access to all lines. We store a reference to the buffer.
                if (!this.buffer) {
                    return;
                }
                const lineCount = this.buffer.lineCount;
                for (let i = fromLine; i < lineCount; i++) {
                    const lineText = this.buffer.getLine(i);
                    const result = this.highlighter.tokenizeLine(lineText, state);
                    this.cache[i] = { tokens: result.tokens, state: result.state };
                    state = result.state;
                }
                // Truncate any stale entries beyond lineCount.
                this.cache.length = lineCount;
                this.dirtyFromLine = lineCount;
            }
            setBuffer(buffer) {
                this.buffer = buffer;
                this.invalidateAll();
            }
        }
        Core.Highlighter = Highlighter;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
