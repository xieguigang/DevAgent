namespace CodeEditor.Core {
    /**
     * A position within the document: line index and column (UTF-16 code unit).
     */
    export interface Position {
        line: number;
        column: number;
    }

    /**
     * A selection range. anchor is where the selection started, active is the
     * current caret position. When they are equal there is no selection.
     */
    export interface Selection {
        anchor: Position;
        active: Position;
    }

    /**
     * Manages a single primary selection (caret + optional range).
     */
    export class Cursor {
        private _selection: Selection = {
            anchor: { line: 0, column: 0 },
            active: { line: 0, column: 0 }
        };

        get selection(): Selection {
            return this._selection;
        }

        setSelection(anchor: Position, active: Position): void {
            this._selection = { anchor, active };
        }

        setPosition(pos: Position, keepAnchor: boolean = false): void {
            if (keepAnchor) {
                this._selection.active = pos;
            } else {
                this._selection.anchor = pos;
                this._selection.active = pos;
            }
        }

        get position(): Position {
            return this._selection.active;
        }

        get hasSelection(): boolean {
            const a = this._selection.anchor;
            const b = this._selection.active;
            return a.line !== b.line || a.column !== b.column;
        }

        /**
         * Return the selection as an ordered (start <= end) range.
         */
        getOrderedRange(): { start: Position; end: Position } {
            const a = this._selection.anchor;
            const b = this._selection.active;
            if (a.line < b.line || (a.line === b.line && a.column <= b.column)) {
                return { start: a, end: b };
            }
            return { start: b, end: a };
        }
    }
}
