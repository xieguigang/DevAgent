namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

    /**
     * R syntax highlighter.
     *
     * Handles:
     *   - Comments (# to end of line)
     *   - Roxygen comments (#' ...)
     *   - Strings: single, double, and backtick-quoted raw strings r"(...)"
     *   - Numbers (incl. scientific, hex 0x..., and 1L integer suffix)
     *   - Control-flow keywords (if, for, while, function, ...)
     *   - Built-in constants (TRUE, FALSE, NULL, NA, Inf, NaN, ...)
     *   - Function call detection (identifier followed by '(')
     *   - Infix operators (%>%, %in%, etc.)
     *   - Assignment operators (<-, ->, <<-, ->>, =)
     */
    export class RHighlighter implements ILanguageHighlighter {
        readonly language = "r";

        private static CONTROL_KEYWORDS = new Set<string>([
            "if", "else", "for", "while", "repeat", "function", "return", "break",
            "next", "in", "switch"
        ]);

        private static KEYWORDS = new Set<string>([
            "local", "global", "library", "require", "source", "invisible",
            "on", "exit"
        ]);

        private static CONSTANTS = new Set<string>([
            "TRUE", "FALSE", "NULL", "NA", "NA_integer_", "NA_real_", "NA_complex_",
            "NA_character_", "Inf", "-Inf", "NaN", "T", "F", "pi", "LETTERS", "letters",
            "month.abb", "month.name"
        ]);

        initialState(): any {
            return {};
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
            const b = new TokenBuilder();
            let i = 0;
            const n = line.length;

            while (i < n) {
                const ch = line[i];

                // Roxygen comment.
                if (ch === "#" && line[i + 1] === "'") {
                    b.push(TokenType.DocComment, line.substr(i));
                    i = n;
                    break;
                }

                // Regular comment.
                if (ch === "#") {
                    b.push(TokenType.Comment, line.substr(i));
                    i = n;
                    break;
                }

                // Raw string: r"(...)", r"[...]", r"{...}".
                if ((ch === "r" || ch === "R") && line[i + 1] === '"') {
                    const open = line[i + 2];
                    if (open === "(" || open === "[" || open === "{") {
                        const close = open === "(" ? ")" : open === "[" ? "]" : "}";
                        let j = i + 3;
                        while (j < n) {
                            if (line[j] === close && line[j + 1] === '"') {
                                j += 2;
                                break;
                            }
                            j++;
                        }
                        b.push(TokenType.String, line.substring(i, j));
                        i = j;
                        continue;
                    }
                }

                // Double-quoted string.
                if (ch === '"') {
                    let j = i + 1;
                    while (j < n) {
                        if (line[j] === "\\" && j + 1 < n) {
                            j += 2;
                            continue;
                        }
                        if (line[j] === '"') {
                            j++;
                            break;
                        }
                        j++;
                    }
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Single-quoted string.
                if (ch === "'") {
                    let j = i + 1;
                    while (j < n) {
                        if (line[j] === "\\" && j + 1 < n) {
                            j += 2;
                            continue;
                        }
                        if (line[j] === "'") {
                            j++;
                            break;
                        }
                        j++;
                    }
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Backtick identifier.
                if (ch === "`") {
                    let j = i + 1;
                    while (j < n && line[j] !== "`") j++;
                    if (j < n) j++;
                    b.push(TokenType.Identifier, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Number (hex, decimal, scientific, integer suffix L, complex i).
                if (/[0-9]/.test(ch) || (ch === "." && /[0-9]/.test(line[i + 1] || ""))) {
                    let j = i;
                    if (ch === "0" && (line[i + 1] === "x" || line[i + 1] === "X")) {
                        j = i + 2;
                        while (j < n && /[0-9A-Fa-f]/.test(line[j])) j++;
                    } else {
                        while (j < n && /[0-9.]/.test(line[j])) j++;
                        if (j < n && (line[j] === "e" || line[j] === "E")) {
                            j++;
                            if (j < n && (line[j] === "+" || line[j] === "-")) j++;
                            while (j < n && /[0-9]/.test(line[j])) j++;
                        }
                    }
                    if (j < n && (line[j] === "L" || line[j] === "i")) j++;
                    b.push(TokenType.Number, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Infix operator %...%.
                if (ch === "%") {
                    let j = i + 1;
                    while (j < n && line[j] !== "%") j++;
                    if (j < n) j++;
                    b.push(TokenType.Operator, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Assignment and other operators.
                if (ch === "<" && line[i + 1] === "-" || ch === "-" && line[i + 1] === ">" ||
                    ch === "<" && line[i + 1] === "<" && line[i + 2] === "-" ||
                    ch === "-" && line[i + 1] === "-" && line[i + 2] === ">") {
                    let j = i;
                    if (line[j] === "<" && line[j + 1] === "<" && line[j + 2] === "-") j += 3;
                    else if (line[j] === "-" && line[j + 1] === "-" && line[j + 2] === ">") j += 3;
                    else j += 2;
                    b.push(TokenType.Operator, line.substring(i, j));
                    i = j;
                    continue;
                }
                if (/[+\-*/^<>=!&|~$@?:]/.test(ch)) {
                    let j = i;
                    while (j < n && /[+\-*/^<>=!&|~$@?:]/.test(line[j])) j++;
                    b.push(TokenType.Operator, line.substring(i, j));
                    i = j;
                    continue;
                }

                // Identifier or keyword.
                if (/[A-Za-z_.]/.test(ch)) {
                    let j = i;
                    while (j < n && /[A-Za-z0-9_.]/.test(line[j])) j++;
                    const word = line.substring(i, j);

                    if (RHighlighter.CONTROL_KEYWORDS.has(word)) {
                        b.push(TokenType.ControlKeyword, word);
                    } else if (RHighlighter.KEYWORDS.has(word)) {
                        b.push(TokenType.Keyword, word);
                    } else if (RHighlighter.CONSTANTS.has(word)) {
                        b.push(TokenType.Constant, word);
                    } else {
                        // Function call detection.
                        let k = j;
                        while (k < n && /\s/.test(line[k])) k++;
                        if (line[k] === "(" || line[k] === "<" && line[k + 1] === "-") {
                            if (line[k] === "(") {
                                b.push(TokenType.Function, word);
                            } else {
                                b.push(TokenType.Identifier, word);
                            }
                        } else {
                            b.push(TokenType.Identifier, word);
                        }
                    }
                    i = j;
                    continue;
                }

                // Punctuation.
                if (/[(){}\[\],;]/.test(ch)) {
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                b.push(TokenType.Plain, ch);
                i++;
            }

            return { tokens: b.result, state };
        }
    }
}
