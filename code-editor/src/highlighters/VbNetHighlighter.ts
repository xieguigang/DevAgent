namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

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
    export class VbNetHighlighter implements ILanguageHighlighter {
        readonly language = "vbnet";

        // Keywords that begin/control statements.
        private static CONTROL_KEYWORDS = new Set<string>([
            "If", "Then", "Else", "ElseIf", "End", "Select", "Case", "For", "Each",
            "In", "While", "Until", "Loop", "Do", "Next", "Exit", "Continue", "Return",
            "Yield", "Try", "Catch", "Finally", "Throw", "When", "Using", "SyncLock",
            "With", "Step", "To", "GoTo", "Stop", "End"
        ]);

        // Declaration / modifier keywords.
        private static KEYWORDS = new Set<string>([
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
            "Open", "Close", "Put", "Get", "Imports", "Option", "Region", "End",
            "ExternalSource", "ExternalChecksum", "If", "EndIf"
        ]);

        // Built-in value types and common framework types.
        private static TYPES = new Set<string>([
            "Boolean", "Byte", "SByte", "Char", "Date", "Decimal", "Double", "Single",
            "Integer", "UInteger", "Long", "ULong", "Short", "UShort", "String", "Object",
            "Void", "IntPtr", "UIntPtr"
        ]);

        initialState(): any {
            return { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0, stringChar: "", interp: false };
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
            const b = new TokenBuilder();
            let i = 0;
            const n = line.length;

            // Continue a multi-line string opened on a previous line.
            // VB.NET escapes a quote by doubling it (""), so a string closes only on a
            // single non-escaped quote. Scan until an odd (non-doubled) '"' is found.
            if (state && state.inString) {
                let j = i;
                let closed = false;
                while (j < n) {
                    if (line[j] === '"') {
                        if (line[j + 1] === '"') {
                            j += 2; // escaped quote, skip both
                            continue;
                        }
                        j++; // closing quote
                        closed = true;
                        break;
                    }
                    j++;
                }
                // Trailing '\r' at end-of-line inside a string body is tolerated.
                b.push(TokenType.String, line.substring(i, j));
                i = j;
                if (closed) {
                    state = { inBlockComment: false, inXmlLiteral: false, inString: false, stringDepth: 0, stringChar: "", interp: false };
                } else {
                    return { tokens: b.result, state };
                }
            }

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

                // String literal (may span multiple lines). VB.NET escapes a quote by doubling it.
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
                    // Char literal: a single character between quotes followed by 'c', e.g. "a"c.
                    // Must be closed on this line and the char type suffix 'c' present.
                    let isCharLiteral = false;
                    if (j < n && line[j] === "c") {
                        const inner = line.substring(i + 1, j - 1);
                        // A char literal is a single code point (or escaped "") followed by c.
                        if (inner.length === 1 || (inner.length === 2 && inner[0] === '"' && inner[1] === '"')) {
                            isCharLiteral = true;
                        }
                    }
                    if (isCharLiteral) {
                        const str = line.substring(i, j + 1);
                        b.push(TokenType.String, str);
                        i = j + 1;
                        continue;
                    }
                    // Plain string: emit what we have; if unclosed carry state across lines.
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    if (j >= n) {
                        return { tokens: b.result, state: { inBlockComment: false, inXmlLiteral: false, inString: true, stringDepth: 0, stringChar: '"', interp: false } };
                    }
                    continue;
                }

                // Interpolated string: $"..." or $@"..." (supports multiple lines).
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
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    if (j >= n) {
                        return { tokens: b.result, state: { inBlockComment: false, inXmlLiteral: false, inString: true, stringDepth: 0, stringChar: '"', interp: true } };
                    }
                    continue;
                }

                // Number (including &H hex, &B binary, &O octal).
                if (ch === "&" && (line[i + 1] === "H" || line[i + 1] === "h" || line[i + 1] === "B" || line[i + 1] === "b" || line[i + 1] === "O" || line[i + 1] === "o")) {
                    let j = i + 2;
                    while (j < n && /[0-9A-Fa-f]/.test(line[j])) j++;
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
                            if (prev !== "e" && prev !== "E") break;
                        }
                        j++;
                    }
                    // Type suffix.
                    if (j < n && /[A-Za-z%&#@!]/.test(line[j])) {
                        const sm = /^[A-Za-z%&#@!]+/.exec(line.substr(j));
                        if (sm) j += sm[0].length;
                    }
                    b.push(TokenType.Number, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Identifier or keyword.
                if (/[A-Za-z_]/.test(ch)) {
                    let j = i;
                    while (j < n && /[A-Za-z0-9_]/.test(line[j])) j++;
                    // Type character suffix.
                    if (j < n && /[%&@!#$]/.test(line[j])) j++;
                    const word = line.substring(i, j);
                    const wordNoSuffix = word.replace(/[%&@!#$]+$/, "");

                    if (VbNetHighlighter.CONTROL_KEYWORDS.has(wordNoSuffix)) {
                        b.push(TokenType.ControlKeyword, word);
                    } else if (VbNetHighlighter.KEYWORDS.has(wordNoSuffix)) {
                        b.push(TokenType.Keyword, word);
                    } else if (VbNetHighlighter.TYPES.has(wordNoSuffix)) {
                        b.push(TokenType.Type, word);
                    } else {
                        // Check if it's a function/sub call (followed by '(').
                        let k = j;
                        while (k < n && /\s/.test(line[k])) k++;
                        if (line[k] === "(") {
                            b.push(TokenType.Function, word);
                        } else {
                            b.push(TokenType.Identifier, word);
                        }
                    }
                    i = j;
                    continue;
                }

                // Operators and punctuation.
                if (/[+\-*/\\^<>=&!]/.test(ch)) {
                    let j = i;
                    while (j < n && /[+\-*/\\^<>=&!]/.test(line[j])) j++;
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
}
