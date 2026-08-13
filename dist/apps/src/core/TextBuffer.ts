namespace CodeEditor.Core {
    import EventEmitter = Utils.EventEmitter;

    export interface TextChange {
        startLine: number;
        startColumn: number;
        endLine: number;
        endColumn: number;
        insertedText: string;
    }

    /**
     * Holds the document text as an array of lines (no trailing newlines).
     * Provides efficient line-based editing operations and emits change events.
     */
    export class TextBuffer {
        private lines: string[] = [""];
        private _changeEmitter = new EventEmitter<TextChange>();

        get onChange() {
            return this._changeEmitter;
        }

        get lineCount(): number {
            return this.lines.length;
        }

        getText(): string {
            return this.lines.join("\n");
        }

        setText(text: string): void {
            if (text.length === 0) {
                this.lines = [""];
            } else {
                this.lines = text.split(/\r\n|\r|\n/);
            }
            this._changeEmitter.emit({
                startLine: 0,
                startColumn: 0,
                endLine: this.lines.length,
                endColumn: 0,
                insertedText: text
            });
        }

        getLine(index: number): string {
            if (index < 0 || index >= this.lines.length) {
                return "";
            }
            return this.lines[index];
        }

        getLines(): string[] {
            return this.lines.slice();
        }

        /**
         * Insert text at the given position. Position is (line, column) where
         * column is a UTF-16 code unit offset within the line.
         */
        insert(line: number, column: number, text: string): void {
            if (line < 0 || line >= this.lines.length) {
                return;
            }
            const before = this.lines[line].substring(0, column);
            const after = this.lines[line].substring(column);
            const inserted = text.split(/\r\n|\r|\n/);
            inserted[0] = before + inserted[0];
            inserted[inserted.length - 1] = inserted[inserted.length - 1] + after;

            const newLines = this.lines.slice(0, line).concat(inserted).concat(this.lines.slice(line + 1));
            this.lines = newLines;

            this._changeEmitter.emit({
                startLine: line,
                startColumn: column,
                endLine: line + inserted.length - 1,
                endColumn: inserted[inserted.length - 1].length - after.length,
                insertedText: text
            });
        }

        /**
         * Delete text in the given inclusive range (startLine,startColumn) to
         * (endLine,endColumn).
         */
        deleteRange(startLine: number, startColumn: number, endLine: number, endColumn: number): string {
            if (startLine < 0 || startLine >= this.lines.length) {
                return "";
            }
            if (endLine < 0 || endLine >= this.lines.length) {
                return "";
            }
            // Capture deleted text.
            let deleted: string;
            if (startLine === endLine) {
                deleted = this.lines[startLine].substring(startColumn, endColumn);
            } else {
                deleted = this.lines[startLine].substring(startColumn);
                for (let i = startLine + 1; i < endLine; i++) {
                    deleted += "\n" + this.lines[i];
                }
                deleted += "\n" + this.lines[endLine].substring(0, endColumn);
            }

            // Rebuild.
            const merged = this.lines[startLine].substring(0, startColumn) + this.lines[endLine].substring(endColumn);
            const newLines = this.lines.slice(0, startLine);
            newLines.push(merged);
            for (let i = endLine + 1; i < this.lines.length; i++) {
                newLines.push(this.lines[i]);
            }
            this.lines = newLines;

            this._changeEmitter.emit({
                startLine,
                startColumn,
                endLine: startLine,
                endColumn: startColumn,
                insertedText: ""
            });

            return deleted;
        }

        /**
         * Replace the entire range with new text (combination of delete + insert).
         */
        replaceRange(startLine: number, startColumn: number, endLine: number, endColumn: number, text: string): void {
            this.deleteRange(startLine, startColumn, endLine, endColumn);
            this.insert(startLine, startColumn, text);
        }

        /**
         * Return the column index after indenting the given line by one tab
         * (or by tabSize spaces, depending on editor settings).
         */
        getLineLength(line: number): number {
            return this.getLine(line).length;
        }
    }
}
