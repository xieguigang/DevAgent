"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Common token types used across all highlighters.
         */
        let TokenType;
        (function (TokenType) {
            TokenType[TokenType["Plain"] = 0] = "Plain";
            TokenType[TokenType["Keyword"] = 1] = "Keyword";
            TokenType[TokenType["ControlKeyword"] = 2] = "ControlKeyword";
            TokenType[TokenType["Identifier"] = 3] = "Identifier";
            TokenType[TokenType["Type"] = 4] = "Type";
            TokenType[TokenType["String"] = 5] = "String";
            TokenType[TokenType["Number"] = 6] = "Number";
            TokenType[TokenType["Comment"] = 7] = "Comment";
            TokenType[TokenType["Operator"] = 8] = "Operator";
            TokenType[TokenType["Punctuation"] = 9] = "Punctuation";
            TokenType[TokenType["Preprocessor"] = 10] = "Preprocessor";
            TokenType[TokenType["Attribute"] = 11] = "Attribute";
            TokenType[TokenType["Tag"] = 12] = "Tag";
            TokenType[TokenType["AttrName"] = 13] = "AttrName";
            TokenType[TokenType["AttrValue"] = 14] = "AttrValue";
            TokenType[TokenType["XmlDelimiter"] = 15] = "XmlDelimiter";
            TokenType[TokenType["XmlText"] = 16] = "XmlText";
            TokenType[TokenType["Heading"] = 17] = "Heading";
            TokenType[TokenType["Bold"] = 18] = "Bold";
            TokenType[TokenType["Italic"] = 19] = "Italic";
            TokenType[TokenType["Code"] = 20] = "Code";
            TokenType[TokenType["Link"] = 21] = "Link";
            TokenType[TokenType["ListMarker"] = 22] = "ListMarker";
            TokenType[TokenType["Quote"] = 23] = "Quote";
            TokenType[TokenType["Property"] = 24] = "Property";
            TokenType[TokenType["Function"] = 25] = "Function";
            TokenType[TokenType["Constant"] = 26] = "Constant";
            TokenType[TokenType["Annotation"] = 27] = "Annotation";
            TokenType[TokenType["DocComment"] = 28] = "DocComment";
            TokenType[TokenType["Error"] = 29] = "Error";
        })(TokenType = Utils.TokenType || (Utils.TokenType = {}));
        /**
         * Helper for building token lists without manually tracking offsets.
         */
        class TokenBuilder {
            constructor() {
                this.tokens = [];
                this.pos = 0;
            }
            push(type, value) {
                const start = this.pos;
                this.pos += value.length;
                this.tokens.push({ type, value, start, end: this.pos });
            }
            advance(n) {
                this.pos += n;
            }
            get position() {
                return this.pos;
            }
            set position(value) {
                this.pos = value;
            }
            get result() {
                return this.tokens;
            }
        }
        Utils.TokenBuilder = TokenBuilder;
        /**
         * Match a regex at the current position; returns the match or null.
         */
        function matchAt(regex, text, pos) {
            // Use sticky flag emulation by anchoring with substring.
            const slice = text.substr(pos);
            const m = regex.exec(slice);
            if (m && m.index === 0) {
                return m;
            }
            return null;
        }
        Utils.matchAt = matchAt;
        /**
         * Escape HTML special characters for safe insertion into innerHTML.
         */
        function escapeHtml(text) {
            return text
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;");
        }
        Utils.escapeHtml = escapeHtml;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
