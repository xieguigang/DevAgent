"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Core;
    (function (Core) {
        /**
         * Manages a single primary selection (caret + optional range).
         */
        class Cursor {
            constructor() {
                this._selection = {
                    anchor: { line: 0, column: 0 },
                    active: { line: 0, column: 0 }
                };
            }
            get selection() {
                return this._selection;
            }
            setSelection(anchor, active) {
                this._selection = { anchor, active };
            }
            setPosition(pos, keepAnchor = false) {
                if (keepAnchor) {
                    this._selection.active = pos;
                }
                else {
                    this._selection.anchor = pos;
                    this._selection.active = pos;
                }
            }
            get position() {
                return this._selection.active;
            }
            get hasSelection() {
                const a = this._selection.anchor;
                const b = this._selection.active;
                return a.line !== b.line || a.column !== b.column;
            }
            /**
             * Return the selection as an ordered (start <= end) range.
             */
            getOrderedRange() {
                const a = this._selection.anchor;
                const b = this._selection.active;
                if (a.line < b.line || (a.line === b.line && a.column <= b.column)) {
                    return { start: a, end: b };
                }
                return { start: b, end: a };
            }
        }
        Core.Cursor = Cursor;
    })(Core = CodeEditor.Core || (CodeEditor.Core = {}));
})(CodeEditor || (CodeEditor = {}));
