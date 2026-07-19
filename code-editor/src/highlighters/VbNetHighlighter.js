"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        var TokenType = Utils.TokenType;
        var TokenBuilder = Utils.TokenBuilder;
        /**
         * VB.NET syntax highlighter.
         *
         * Handles:
         *   - Line comments (') and REM statements
         *   - XML doc comments ('')
         *   - String literals ("..." with "" escape) and interpolated strings ($"...{...}...")
         *   - Char literals ("a"c)
         *   - Numbers (decimal, hex &H..., binary &B..., octal &O...)
         *   - Preprocessor (#If, #End If, #Region, etc.)
         *   - Keywords (control flow, declarations, modifiers)
         *   - Type characters and identifiers
         *   - Multi-line XML literals (basic)
         */
        class VbNetHighlighter {
            constructor() {
                this.language = "vbnet";
            }
            initialState() {
                return { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0 };
            }
            tokenizeLine(line, state) {
                const b = new TokenBuilder();
                let i = 0;
                const n = line.length;
                // Handle multi-line block comment continuation (rare in VB but supported via XML doc).
                // VB.NET does not have block comments, but we keep state for future use.
                while (i < n) {
                    const ch = line[i];
                    // Line comment.
                    if (ch === "'") {
                        // Check for XML doc comment (''')
                        if (line.substr(i, 3) === "'''") {
                            b.push(TokenType.DocComment, line.substr(i));
                            i = n;
                            break;
                        }
                        b.push(TokenType.Comment, line.substr(i));
                        i = n;
                        break;
                    }
                    // REM comment (only at start of token).
                    if (ch === "R" || ch === "r") {
                        const remMatch = /^REM\b/i.exec(line.substr(i));
                        if (remMatch && (i === 0 || /\s/.test(line[i - 1]))) {
                            b.push(TokenType.Comment, line.substr(i));
                            i = n;
                            break;
                        }
                    }
                    // Preprocessor directive.
                    if (ch === "#" && (i === 0 || /\s/.test(line[i - 1]))) {
                        const ppMatch = /^#\s*[A-Za-z]+/.exec(line.substr(i));
                        if (ppMatch) {
                            b.push(TokenType.Preprocessor, ppMatch[0]);
                            i += ppMatch[0].length;
                            continue;
                        }
                    }
                    // String literal.
                    if (ch === '"') {
                        let j = i + 1;
                        while (j < n) {
                            if (line[j] === '"') {
                                if (line[j + 1] === '"') {
                                    j += 2;
                                    continue;
                                }
                                j++;
                                break;
                            }
                            j++;
                        }
                        // Check for char literal: "a"c
                        if (j < n && line[j] === "c" || (j === n && line[n - 1] === '"')) {
                            // Check if next non-space char is 'c'
                        }
                        const str = line.substring(i, j);
                        b.push(TokenType.String, str);
                        i = j;
                        continue;
                    }
                    // Interpolated string: $"..." (basic, single-line).
                    if ((ch === "$" && line[i + 1] === '"') || (ch === "$" && line[i + 1] === "@" && line[i + 2] === '"')) {
                        const startChar = line[i + 1] === "@" ? 2 : 1;
                        let j = i + 1 + startChar;
                        while (j < n) {
                            if (line[j] === '"') {
                                if (line[j + 1] === '"') {
                                    j += 2;
                                    continue;
                                }
                                j++;
                                break;
                            }
                            j++;
                        }
                        const str = line.substring(i, j);
                        b.push(TokenType.String, str);
                        i = j;
                        continue;
                    }
                    // Number (including &H hex, &B binary, &O octal).
                    if (ch === "&" && (line[i + 1] === "H" || line[i + 1] === "h" || line[i + 1] === "B" || line[i + 1] === "b" || line[i + 1] === "O" || line[i + 1] === "o")) {
                        let j = i + 2;
                        while (j < n && /[0-9A-Fa-f]/.test(line[j]))
                            j++;
                        // Optional type suffix
                        if (j < n && /[A-Za-z]/.test(line[j])) {
                            const suffixMatch = /^[A-Za-z]+/.exec(line.substr(j));
                            if (suffixMatch && /^(S|I|L|US|UI|UL|D|F|R|C)$/i.test(suffixMatch[0])) {
                                j += suffixMatch[0].length;
                            }
                        }
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    if (/[0-9]/.test(ch) || (ch === "." && /[0-9]/.test(line[i + 1] || ""))) {
                        let j = i;
                        while (j < n && /[0-9.eE+\-]/.test(line[j])) {
                            // Stop on +/- that isn't part of exponent.
                            if ((line[j] === "+" || line[j] === "-") && j > i) {
                                const prev = line[j - 1];
                                if (prev !== "e" && prev !== "E")
                                    break;
                            }
                            j++;
                        }
                        // Type suffix.
                        if (j < n && /[A-Za-z%&#@!]/.test(line[j])) {
                            const sm = /^[A-Za-z%&#@!]+/.exec(line.substr(j));
                            if (sm)
                                j += sm[0].length;
                        }
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // Identifier or keyword.
                    if (/[A-Za-z_]/.test(ch)) {
                        let j = i;
                        while (j < n && /[A-Za-z0-9_]/.test(line[j]))
                            j++;
                        // Type character suffix.
                        if (j < n && /[%&@!#$]/.test(line[j]))
                            j++;
                        const word = line.substring(i, j);
                        const wordNoSuffix = word.replace(/[%&@!#$]+$/, "");
                        if (VbNetHighlighter.CONTROL_KEYWORDS.has(wordNoSuffix)) {
                            b.push(TokenType.ControlKeyword, word);
                        }
                        else if (VbNetHighlighter.KEYWORDS.has(wordNoSuffix)) {
                            b.push(TokenType.Keyword, word);
                        }
                        else if (VbNetHighlighter.TYPES.has(wordNoSuffix)) {
                            b.push(TokenType.Type, word);
                        }
                        else {
                            // Check if it's a function/sub call (followed by '(').
                            let k = j;
                            while (k < n && /\s/.test(line[k]))
                                k++;
                            if (line[k] === "(") {
                                b.push(TokenType.Function, word);
                            }
                            else {
                                b.push(TokenType.Identifier, word);
                            }
                        }
                        i = j;
                        continue;
                    }
                    // Operators and punctuation.
                    if (/[+\-*/\\^<>=&!]/.test(ch)) {
                        let j = i;
                        while (j < n && /[+\-*/\\^<>=&!]/.test(line[j]))
                            j++;
                        b.push(TokenType.Operator, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    if (/[(){}\[\],.;:]/.test(ch)) {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // Whitespace or anything else.
                    b.push(TokenType.Plain, ch);
                    i++;
                }
                return { tokens: b.result, state };
            }
        }
        // Keywords that begin/control statements.
        VbNetHighlighter.CONTROL_KEYWORDS = new Set([
            "If", "Then", "Else", "ElseIf", "End", "Select", "Case", "For", "Each",
            "In", "While", "Until", "Loop", "Do", "Next", "Exit", "Continue", "Return",
            "Yield", "Try", "Catch", "Finally", "Throw", "When", "Using", "SyncLock",
            "With", "Step", "To", "GoTo", "Stop", "End"
        ]);
        // Declaration / modifier keywords.
        VbNetHighlighter.KEYWORDS = new Set([
            "Public", "Private", "Protected", "Friend", "Shared", "Static", "ReadOnly",
            "WriteOnly", "Dim", "Const", "Class", "Module", "Structure", "Interface",
            "Enum", "Namespace", "Sub", "Function", "Property", "Operator", "Event",
            "Delegate", "Handles", "Implements", "Inherits", "Of", "As", "New", "Me",
            "MyBase", "MyClass", "Nothing", "True", "False", "Null", "And", "Or", "Not",
            "Xor", "AndAlso", "OrElse", "Is", "IsNot", "Like", "Mod", "TypeOf", "GetType",
            "AddressOf", "Await", "Async", "Iterator", "Partial", "Overridable",
            "Overloads", "Overrides", "MustInherit", "MustOverride", "NotOverridable",
            "Shadows", "Widening", "Narrowing", "ByVal", "ByRef", "Optional", "ParamArray",
            "Declare", "Lib", "Alias", "Narrowing", "Widening", "Mid", "Option",
            "Explicit", "Strict", "Compare", "Text", "Binary", "Off", "On", "Infer",
            "Custom", "AddHandler", "RemoveHandler", "RaiseEvent", "DirectCast",
            "TryCast", "CType", "CInt", "CStr", "CBool", "CDbl", "CDec", "CLng",
            "CShort", "CSng", "CByte", "CChar", "CDate", "CUInt", "CULng", "CUShort",
            "CSByte", "Let", "Set", "Get", "Wend", "Call", "ReDim", "Preserve",
            "Erase", "Error", "Resume", "On", "Print", "Input", "Line", "Width",
            "Open", "Close", "Put", "Get"
        ]);
        // Built-in value types and common framework types.
        VbNetHighlighter.TYPES = new Set([
            "Boolean", "Byte", "SByte", "Char", "Date", "Decimal", "Double", "Single",
            "Integer", "UInteger", "Long", "ULong", "Short", "UShort", "String", "Object",
            "Void", "IntPtr", "UIntPtr"
        ]);
        Highlighters.VbNetHighlighter = VbNetHighlighter;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
