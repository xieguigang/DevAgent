"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        /**
         * Implements the "Go to Line" command. The UI is a small modal dialog
         * with a line number input; this class handles validation and the
         * callback to the editor.
         */
        class GoToLine {
            constructor() {
                this.maxLine = 1;
            }
            setMaxLine(max) {
                this.maxLine = max;
            }
            getMaxLine() {
                return this.maxLine;
            }
            /**
             * Validate a line number string. Returns the parsed line number
             * (1-based) or -1 if invalid.
             */
            validate(input) {
                const n = parseInt(input, 10);
                if (isNaN(n) || n < 1 || n > this.maxLine) {
                    return -1;
                }
                return n;
            }
        }
        Features.GoToLine = GoToLine;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
