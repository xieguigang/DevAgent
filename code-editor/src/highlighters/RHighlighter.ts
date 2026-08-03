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

        /**
         * R's internal primitive functions (prefix form), sourced from
         * base:::primaries / get("__Primitives__", baseenv()). These are
         * highlighted distinctly from user-defined (third-party) functions.
         * Control-flow keywords and constants are intentionally excluded here
         * so they keep their existing keyword/constant styling.
         */
        private static PRIMITIVES = new Set<string>([
            ".subset", ".subset2", ".External", ".Call", ".Fortran", ".C",
            "c", "list", "vector", "numeric", "character", "logical", "integer",
            "double", "complex", "raw", "structure", "attributes", "attr",
            "class", "unclass", "names", "dim", "dimnames", "length", "levels",
            "typeof", "storage.mode", "mode", "oldClass", "comment",
            "as.character", "as.numeric", "as.integer", "as.logical",
            "as.complex", "as.double", "as.raw", "as.vector", "as.list",
            "is.null", "is.na", "is.nan", "is.finite", "is.infinite",
            "is.numeric", "is.character", "is.logical", "is.complex",
            "is.raw", "is.list", "is.function", "is.expression", "is.object",
            "is.single", "is.environment", "is.pairlist", "is.language",
            "is.symbol", "is.matrix", "is.array", "is.atomic", "is.recursive",
            "is.call", "is.expression", "is.primitive", "is.s4",
            "substitute", "quote", "enquote", "eval", "evalq", "call",
            "expression", "force", "on.exit", "environment", "globalenv",
            "baseenv", "emptyenv", "new.env", "parent.env", "parent.frame",
            "sys.frame", "sys.function", "sys.call", "sys.parent", "sys.nframe",
            "sys.calls", "sys.frames", "sys.parents", "sys.body", "sys.function",
            "missing", "nargs", "Recall", "UseMethod", "NextMethod",
            "standardGeneric", "body", "formals", "args", "invisible",
            "withVisible", "delayedAssign", "bindenv", "lockBinding",
            "unlockBinding", "lockEnvironment", "makelazy", "pos.to.env",
            "proc.time", "gc", "gc.time", "memory.profile", "tracemem",
            "retracemem", "untracemem", "gctorture", "gctorture2",
            "file", "textConnection", "gzfile", "bzfile", "xzfile", "unz",
            "pipe", "socketConnection", "url", "stdin", "stdout", "stderr",
            "readLines", "writeLines", "cat", "print", "format", "format.info",
            "paste", "paste0", "sprintf", "strsplit", "sub", "gsub", "match",
            "pmatch", "charmatch", "startsWith", "endsWith", "grep", "grepl",
            "regexpr", "gregexpr", "agrep", "tolower", "toupper", "chartr",
            "abbreviate", "nchar", "nzchar", "substr", "substring", "strtrim",
            "make.names", "make.unique", "all", "any", "sum", "prod", "min",
            "max", "range", "mean", "median", "var", "sd", "cov", "cor",
            "diff", "cumsum", "cumprod", "cummax", "cummin", "round", "signif",
            "trunc", "floor", "ceiling", "abs", "sign", "sqrt", "exp", "log",
            "expm1", "log1p", "cos", "sin", "tan", "acos", "asin", "atan",
            "cosh", "sinh", "tanh", "acos", "acosh", "asinh", "atanh",
            "gamma", "lgamma", "digamma", "trigamma", "choose", "factorial",
            "beta", "lbeta", "rowSums", "colSums", "rowMeans", "colMeans",
            "apply", "lapply", "sapply", "vapply", "tapply", "mapply", "Map",
            "Reduce", "Filter", "Find", "Position", "Negate", "eapply",
            "rapply", "outer", "kronecker", "sweep", "scale", "rowsum",
            "aggregate", "by", "split", "unsplit", "rbind", "cbind",
            "data.frame", "as.data.frame", "expand.grid", "order", "sort",
            "sort.list", "rank", "unique", "duplicated", "union", "intersect",
            "setdiff", "setequal", "is.element", "which", "which.min",
            "which.max", "array", "matrix", "diag", "upper.tri", "lower.tri",
            "t", "crossprod", "tcrossprod", "solve", "eigen", "svd", "qr",
            "det", "determinant", "fft", "nextn", "convolve", "filter", "poly",
            "sample", "rnorm", "runif", "rpois", "rexp", "rbinom", "rbeta",
            "rgamma", "rchisq", "rt", "rf", "rgeom", "rhyper", "rnbinom",
            "rweibull", "rwilcox", "rsignrank", "set.seed", "date", "Sys.time",
            "Sys.Date", "as.POSIXct", "as.Date", "difftime", "julian",
            "months", "quarters", "weekdays", "try", "tryCatch",
            "withCallingHandlers", "stop", "warning", "message", "gettext",
            "gettextf", "ngettext", "options", "getOption", "par", "dev.off",
            "plot", "hist", "boxplot", "points", "lines", "abline", "title",
            "axis", "legend", "text", "arrows", "segments", "polygon", "curve",
            "pairs", "coplot", "image", "contour", "persp", "barplot",
            "dotchart", "identify", "locator", "stem", "qqnorm", "qqline",
            "rep", "seq", "seq.int", "seq_len", "seq_along", "rev", "rep.int",
            "rep_len", "table", "xtabs", "prop.table", "margin.table",
            "ftable", "as.table", "aperm", "trace", "untrace", "browser",
            "recover", "capabilities", "machine", "commandArgs", "getwd",
            "setwd", "getenv", "setenv", "unsetenv", "Sys.getenv",
            "Sys.setenv", "Sys.unsetenv", "list.files", "dir", "file.path",
            "normalizePath", "basename", "dirname", "file.exists",
            "file.choose", "file.copy", "file.create", "file.remove",
            "file.rename", "file.append", "file.symlink", "dir.create",
            "tempfile", "tempdir", "R.home", "system", "system2", "shell",
            "shell.exec", "Sys.which", "Sys.info", "load", "save", "saveRDS",
            "readRDS", "serialize", "unserialize", "serializeToConn",
            "assign", "get", "exists", "remove", "rm", "ls", "objects",
            "laply", "mget", "eapply", "attach", "detach", "with", "within",
            "local", "do.call", "do.call", "browser", "interactive",
            "is.loaded", "dyn.load", "dyn.unload", "getLoadedDLLs",
            "noquote", "dQuote", "sQuote", "encodeString", "iconv",
            "iconvlist", "utf8ToInt", "intToUtf8", "charToRaw", "rawToChar",
            "rawShift", "intToBits", "rawConnection", "rawConnectionValue",
            "seek", "truncate", "flush", "close", "open", "isOpen",
            "readChar", "writeChar", "readBin", "writeBin", "readLines",
            "pushBack", "clearPushBack", "getConnection", "summary",
            "print", "cat", "format", "str", "ls", "dump", "dput", "dget",
            "withRestarts", "signalCondition", "simpleCondition",
            "errorCondition", "warningCondition", "restart", "invokeRestart",
            "computeRestarts", "findRestart", "conditionCall",
            "conditionMessage", "geterrmessage", "gregexpr", "sub", "gsub"
        ]);

        /**
         * R's internal primitive operators (infix/symbol form). When a run of
         * operator characters or an identifier-style operator matches one of
         * these, it is highlighted as a primitive.
         */
        private static PRIMITIVE_OPS = new Set<string>([
            "+", "-", "*", "/", "^", "%%", "%/%", "%*%", "%o%", "%x%", "%in%",
            ":", ">", "<", ">=", "<=", "==", "!=", "!", "&", "&&", "|", "||",
            "~", "<-", "<<-", "->", "->>", "$", "@", "[[", "]]", "[", "]", "="
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
                    const op = line.substring(i, j);
                    b.push(RHighlighter.PRIMITIVE_OPS.has(op) ? TokenType.PrimitiveFunction : TokenType.Operator, op);
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
                    const asg = line.substring(i, j);
                    b.push(RHighlighter.PRIMITIVE_OPS.has(asg) ? TokenType.PrimitiveFunction : TokenType.Operator, asg);
                    i = j;
                    continue;
                }
                // Bracket-index operators [[ ]] (R primitives). Single [ ] are
                // handled as punctuation below.
                if (ch === "[" || ch === "]") {
                    if (line[i + 1] === ch) {
                        b.push(TokenType.PrimitiveFunction, line.substring(i, i + 2));
                        i += 2;
                    } else {
                        b.push(TokenType.Punctuation, ch);
                        i++;
                    }
                    continue;
                }

                if (/[+\-*/^<>=!&|~$@?:]/.test(ch)) {
                    let j = i;
                    while (j < n && /[+\-*/^<>=!&|~$@?:]/.test(line[j])) j++;
                    const op = line.substring(i, j);
                    if (RHighlighter.PRIMITIVE_OPS.has(op)) {
                        b.push(TokenType.PrimitiveFunction, op);
                    } else {
                        b.push(TokenType.Operator, op);
                    }
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
                    } else if (RHighlighter.PRIMITIVES.has(word)) {
                        // Primitive function call detection.
                        let k = j;
                        while (k < n && /\s/.test(line[k])) k++;
                        if (line[k] === "(") {
                            b.push(TokenType.PrimitiveFunction, word);
                        } else {
                            b.push(TokenType.Identifier, word);
                        }
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
