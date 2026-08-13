namespace CodeEditor.Features {
    /**
     * Implements the "Go to Line" command. The UI is a small modal dialog
     * with a line number input; this class handles validation and the
     * callback to the editor.
     */
    export class GoToLine {
        private maxLine: number = 1;

        setMaxLine(max: number): void {
            this.maxLine = max;
        }

        getMaxLine(): number {
            return this.maxLine;
        }

        /**
         * Validate a line number string. Returns the parsed line number
         * (1-based) or -1 if invalid.
         */
        validate(input: string): number {
            const n = parseInt(input, 10);
            if (isNaN(n) || n < 1 || n > this.maxLine) {
                return -1;
            }
            return n;
        }
    }
}
