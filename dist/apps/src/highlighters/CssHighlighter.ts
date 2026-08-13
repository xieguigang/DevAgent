namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    import TokenType = Utils.TokenType;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;

    /**
     * Tokenizer state for CSS, carried across lines.
     */
    export interface CssState {
        /** Inside a multi-line block comment. */
        inBlockComment: boolean;
        /**
         * Parser context:
         *  - "selector": outside a declaration block (expecting selectors or at-rules)
         *  - "property": inside a block, before the ':' (expecting property name)
         *  - "value": inside a block, after the ':' (expecting property value)
         */
        context: "selector" | "property" | "value";
        /** Brace nesting depth (0 = top level). */
        depth: number;
        /** Whether we are inside the parenthesised condition of an at-rule. */
        inAtRuleParens: boolean;
    }

    /**
     * CSS syntax highlighter with multi-line state tracking.
     *
     * Handles:
     *   - Block comments (multi-line)
     *   - At-rules (@media, @import, @keyframes, @supports, @font-face, etc.)
     *   - Selectors: element, .class, #id, :pseudo-class, ::pseudo-element,
     *     [attr], combinators (>, +, ~, space)
     *   - Property names and values (context-sensitive colouring)
     *   - Colour values (#hex, named colours)
     *   - Numbers with units (px, em, %, etc.)
     *   - Strings (single and double-quoted)
     *   - CSS custom properties (--var-name)
     *   - Functions: rgb(), var(), calc(), url(), etc.
     *   - !important
     */
    export class CssHighlighter implements ILanguageHighlighter {
        readonly language = "css";

        private static readonly AT_RULES = new Set<string>([
            "media", "import", "keyframes", "supports", "font-face",
            "charset", "namespace", "page", "font-feature-values",
            "counter-style", "property", "layer", "container", "scope",
            "starting-style", "viewport", "document"
        ]);

        private static readonly NAMED_COLORS = new Set<string>([
            "transparent", "currentcolor", "black", "white", "red", "green",
            "blue", "yellow", "cyan", "magenta", "gray", "grey", "silver",
            "maroon", "olive", "lime", "aqua", "teal", "navy", "purple",
            "orange", "aliceblue", "antiquewhite", "aquamarine", "azure",
            "beige", "bisque", "blanchedalmond", "blueviolet", "brown",
            "burlywood", "cadetblue", "chartreuse", "chocolate", "coral",
            "cornflowerblue", "cornsilk", "crimson", "darkblue", "darkcyan",
            "darkgoldenrod", "darkgray", "darkgreen", "darkgrey",
            "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange",
            "darkorchid", "darkred", "darksalmon", "darkseagreen",
            "darkslateblue", "darkslategray", "darkslategrey",
            "darkturquoise", "darkviolet", "deeppink", "deepskyblue",
            "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite",
            "forestgreen", "gainsboro", "ghostwhite", "gold", "goldenrod",
            "greenyellow", "honeydew", "hotpink", "indianred", "indigo",
            "ivory", "khaki", "lavender", "lavenderblush", "lawngreen",
            "lemonchiffon", "lightblue", "lightcoral", "lightcyan",
            "lightgoldenrodyellow", "lightgray", "lightgreen", "lightgrey",
            "lightpink", "lightsalmon", "lightseagreen", "lightskyblue",
            "lightslategray", "lightslategrey", "lightsteelblue",
            "lightyellow", "limegreen", "linen", "mediumaquamarine",
            "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen",
            "mediumslateblue", "mediumspringgreen", "mediumturquoise",
            "mediumvioletred", "midnightblue", "mintcream", "mistyrose",
            "moccasin", "navajowhite", "oldlace", "olivedrab", "orangered",
            "orchid", "palegoldenrod", "palegreen", "paleturquoise",
            "palevioletred", "papayawhip", "peachpuff", "peru", "pink",
            "plum", "powderblue", "rebeccapurple", "rosybrown", "royalblue",
            "saddlebrown", "salmon", "sandybrown", "seagreen", "seashell",
            "sienna", "skyblue", "slateblue", "slategray", "slategrey",
            "snow", "springgreen", "steelblue", "tan", "thistle", "tomato",
            "turquoise", "violet", "wheat", "whitesmoke", "yellowgreen"
        ]);

        private static readonly UNITS = new Set<string>([
            "px", "em", "rem", "ex", "ch", "vw", "vh", "vmin", "vmax",
            "%", "cm", "mm", "in", "pt", "pc", "q", "fr", "deg", "grad",
            "rad", "turn", "s", "ms", "hz", "khz", "dpi", "dpcm", "dppx",
            "x", "vi", "vb", "ic", "rlh", "lh", "cap", "rcap", "rch", "ric",
            "rex", "svh", "svw", "svmin", "svmax", "lvh", "lvw", "lvmin",
            "lvmax", "dvh", "dvw", "dvmin", "dvmax"
        ]);

        initialState(): CssState {
            return {
                inBlockComment: false,
                context: "selector",
                depth: 0,
                inAtRuleParens: false
            };
        }

        tokenizeLine(line: string, state: any): TokenizeResult {
            const b = new TokenBuilder();
            let i = 0;
            const n = line.length;
            let s: CssState = state ? state : this.initialState();
            s = { ...s };

            // ---- Continue multi-line block comment ----
            if (s.inBlockComment) {
                const endIdx = line.indexOf("*/");
                if (endIdx < 0) {
                    b.push(TokenType.Comment, line);
                    return { tokens: b.result, state: s };
                }
                const close = endIdx + 2;
                b.push(TokenType.Comment, line.substring(0, close));
                i = close;
                s.inBlockComment = false;
            }

            while (i < n) {
                const ch = line[i];

                // --- Whitespace ---
                if (ch === " " || ch === "\t" || ch === "\r") {
                    let j = i;
                    while (j < n && (line[j] === " " || line[j] === "\t" || line[j] === "\r")) j++;
                    b.push(TokenType.Plain, line.substring(i, j));
                    i = j;
                    continue;
                }

                // --- Block comment ---
                if (ch === "/" && line[i + 1] === "*") {
                    const endIdx = line.indexOf("*/", i + 2);
                    if (endIdx < 0) {
                        b.push(TokenType.Comment, line.substring(i));
                        s.inBlockComment = true;
                        return { tokens: b.result, state: s };
                    }
                    const close = endIdx + 2;
                    b.push(TokenType.Comment, line.substring(i, close));
                    i = close;
                    continue;
                }

                // --- !important ---
                if (ch === "!" && line.substr(i, 10).toLowerCase() === "!important") {
                    b.push(TokenType.Keyword, line.substring(i, i + 10));
                    i += 10;
                    continue;
                }

                // --- { : enter declaration block ---
                if (ch === "{") {
                    s.depth++;
                    s.context = "property";
                    s.inAtRuleParens = false;
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- } : leave declaration block ---
                if (ch === "}") {
                    if (s.depth > 0) s.depth--;
                    s.context = s.depth === 0 ? "selector" : "property";
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- ; : end declaration ---
                if (ch === ";") {
                    if (!s.inAtRuleParens) {
                        s.context = s.depth === 0 ? "selector" : "property";
                    }
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- : : switch from property to value context ---
                if (ch === ":") {
                    if (s.context === "property" && !s.inAtRuleParens) {
                        s.context = "value";
                        b.push(TokenType.Punctuation, ch);
                        i++;
                        continue;
                    }
                    // In selector context: pseudo-class/element (handled below via identifier scan).
                    // But we reach here only if the identifier scan didn't catch it.
                    // Fall through to pseudo handling.
                    let j = i + 1;
                    let isPseudoElement = false;
                    if (line[j] === ":") { isPseudoElement = true; j++; }
                    while (j < n && /[A-Za-z0-9_-]/.test(line[j])) j++;
                    if (j > i + 1) {
                        b.push(TokenType.PseudoClass, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- ( ) ---
                if (ch === "(" || ch === ")") {
                    if (ch === "(" && s.context === "selector") {
                        s.inAtRuleParens = true;
                    }
                    if (ch === ")" && s.inAtRuleParens) {
                        s.inAtRuleParens = false;
                    }
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- @ at-rule ---
                if (ch === "@") {
                    let j = i + 1;
                    while (j < n && /[A-Za-z0-9_-]/.test(line[j])) j++;
                    b.push(TokenType.AtRule, line.substring(i, j));
                    i = j;
                    continue;
                }

                // --- String ---
                if (ch === "'" || ch === '"') {
                    const q = ch;
                    let j = i + 1;
                    while (j < n) {
                        if (line[j] === "\\" && j + 1 < n) { j += 2; continue; }
                        if (line[j] === q) { j++; break; }
                        j++;
                    }
                    b.push(TokenType.String, line.substring(i, j));
                    i = j;
                    continue;
                }

                // --- # hex colour or ID selector ---
                if (ch === "#") {
                    let j = i + 1;
                    // Try hex colour first: # followed by hex digits (3, 4, 6, or 8)
                    while (j < n && /[0-9A-Fa-f]/.test(line[j])) j++;
                    if (j > i + 1 && (j - i - 1) <= 8) {
                        // Looks like a hex colour.
                        b.push(TokenType.ColorValue, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    // ID selector: #idName
                    j = i + 1;
                    while (j < n && /[A-Za-z0-9_-]/.test(line[j])) j++;
                    if (j > i + 1) {
                        b.push(TokenType.Selector, line.substring(i, j));
                        i = j;
                        continue;
                    }
                    b.push(TokenType.Operator, "#");
                    i++;
                    continue;
                }

                // --- Number + unit ---
                if (this.isNumberStart(ch, line, i, n)) {
                    let j = this.scanNumber(line, i, n);
                    // Check for unit suffix.
                    let u = j;
                    while (u < n && /[A-Za-z%]/.test(line[u])) u++;
                    const unit = line.substring(j, u);
                    if (unit.length > 0 && CssHighlighter.UNITS.has(unit.toLowerCase())) {
                        b.push(TokenType.Number, line.substring(i, j));
                        b.push(TokenType.Unit, unit);
                        i = u;
                    } else if (unit.length > 0) {
                        b.push(TokenType.Number, line.substring(i, j));
                        b.push(TokenType.Identifier, unit);
                        i = u;
                    } else {
                        b.push(TokenType.Number, line.substring(i, j));
                        i = j;
                    }
                    continue;
                }

                // --- Identifier / keyword / property / function ---
                if (/[A-Za-z_\-]/.test(ch)) {
                    let j = i;
                    while (j < n && /[A-Za-z0-9_\-]/.test(line[j])) j++;
                    const word = line.substring(i, j);
                    const wordLower = word.toLowerCase();
                    i = j;

                    // Check if followed by '(' → function call.
                    let k = i;
                    while (k < n && (line[k] === " " || line[k] === "\t")) k++;
                    const isFunction = line[k] === "(";

                    if (s.context === "selector" || s.inAtRuleParens) {
                        // Selector context: element type selector or at-rule keyword.
                        b.push(TokenType.Selector, word);
                    } else if (s.context === "property") {
                        // Property name or CSS custom property.
                        if (word.startsWith("--")) {
                            b.push(TokenType.Variable, word);
                        } else {
                            b.push(TokenType.Property, word);
                        }
                    } else if (s.context === "value") {
                        // Property value context.
                        if (isFunction) {
                            b.push(TokenType.Function, word);
                        } else if (CssHighlighter.NAMED_COLORS.has(wordLower)) {
                            b.push(TokenType.ColorValue, word);
                        } else {
                            b.push(TokenType.Identifier, word);
                        }
                    } else {
                        b.push(TokenType.Identifier, word);
                    }
                    continue;
                }

                // --- . class selector ---
                if (ch === ".") {
                    let j = i + 1;
                    while (j < n && /[A-Za-z0-9_-]/.test(line[j])) j++;
                    if (j > i + 1) {
                        b.push(TokenType.Selector, line.substring(i, j));
                        i = j;
                    } else {
                        b.push(TokenType.Operator, ".");
                        i++;
                    }
                    continue;
                }

                // --- [ attribute selector ] ---
                if (ch === "[") {
                    let j = i + 1;
                    while (j < n && line[j] !== "]") j++;
                    if (j < n) j++;
                    b.push(TokenType.Selector, line.substring(i, j));
                    i = j;
                    continue;
                }

                // --- Combinators and operators ---
                if (ch === ">" || ch === "+" || ch === "~" || ch === "=" ||
                    ch === "*" || ch === "|" || ch === "^" || ch === "$") {
                    b.push(TokenType.Operator, ch);
                    i++;
                    continue;
                }

                // --- Comma ---
                if (ch === ",") {
                    b.push(TokenType.Punctuation, ch);
                    i++;
                    continue;
                }

                // --- Fallback ---
                b.push(TokenType.Plain, ch);
                i++;
            }

            return { tokens: b.result, state: s };
        }

        // ========== Helpers ==========

        private isNumberStart(ch: string, line: string, i: number, n: number): boolean {
            if (ch >= "0" && ch <= "9") return true;
            if (ch === "-" || ch === "+") {
                // Negative/positive number: only if followed by digit or dot.
                if (i + 1 < n) {
                    const next = line[i + 1];
                    if (next >= "0" && next <= "9") return true;
                    if (next === "." && i + 2 < n && line[i + 2] >= "0" && line[i + 2] <= "9") return true;
                }
            }
            if (ch === ".") {
                return i + 1 < n && line[i + 1] >= "0" && line[i + 1] <= "9";
            }
            return false;
        }

        private scanNumber(line: string, i: number, n: number): number {
            let j = i;
            // Optional sign.
            if (line[j] === "-" || line[j] === "+") j++;
            // Integer part.
            while (j < n && line[j] >= "0" && line[j] <= "9") j++;
            // Fractional part.
            if (j < n && line[j] === ".") {
                j++;
                while (j < n && line[j] >= "0" && line[j] <= "9") j++;
            }
            // Exponent.
            if (j < n && (line[j] === "e" || line[j] === "E")) {
                j++;
                if (j < n && (line[j] === "+" || line[j] === "-")) j++;
                while (j < n && line[j] >= "0" && line[j] <= "9") j++;
            }
            return j;
        }
    }
}
