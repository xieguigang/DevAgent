declare namespace CodeEditor.Utils {
    /**
     * Common token types used across all highlighters.
     */
    enum TokenType {
        Plain = 0,
        Keyword = 1,
        ControlKeyword = 2,
        Identifier = 3,
        Type = 4,
        String = 5,
        Number = 6,
        Comment = 7,
        Operator = 8,
        Punctuation = 9,
        Preprocessor = 10,
        Attribute = 11,
        Tag = 12,
        AttrName = 13,
        AttrValue = 14,
        XmlDelimiter = 15,
        XmlText = 16,
        Heading = 17,
        Bold = 18,
        Italic = 19,
        Code = 20,
        Link = 21,
        ListMarker = 22,
        Quote = 23,
        Property = 24,
        Function = 25,
        Constant = 26,
        Annotation = 27,
        DocComment = 28,
        Error = 29,
        PrimitiveFunction = 30,
        StatementTerminator = 31,
        Regex = 32,
        TemplateString = 33,
        TemplateDelimiter = 34,
        Decorator = 35,
        Selector = 36,
        PseudoClass = 37,
        Unit = 38,
        ColorValue = 39,
        AtRule = 40,
        Variable = 41,
        Builtin = 42,
        TypeParameter = 43
    }
    /**
     * A single token produced by a highlighter.
     */
    interface Token {
        type: TokenType;
        value: string;
        start: number;
        end: number;
    }
    /**
     * Result of tokenizing a line: list of tokens plus optional state to carry
     * over to the next line (for multi-line constructs like block comments).
     */
    interface TokenizeResult {
        tokens: Token[];
        state: any;
    }
    /**
     * Interface that every language highlighter must implement.
     */
    interface ILanguageHighlighter {
        /** Language identifier (e.g. "vbnet", "r"). */
        readonly language: string;
        /** Tokenize a single line, carrying state between lines. */
        tokenizeLine(line: string, state: any): TokenizeResult;
        /** Initial state for tokenization. */
        initialState(): any;
    }
    /**
     * Helper for building token lists without manually tracking offsets.
     */
    class TokenBuilder {
        private tokens;
        private pos;
        push(type: TokenType, value: string): void;
        advance(n: number): void;
        get position(): number;
        set position(value: number);
        get result(): Token[];
    }
    /**
     * Match a regex at the current position; returns the match or null.
     */
    function matchAt(regex: RegExp, text: string, pos: number): RegExpExecArray | null;
    /**
     * Escape HTML special characters for safe insertion into innerHTML.
     */
    function escapeHtml(text: string): string;
}
declare namespace CodeEditor.Utils {
    /**
     * Minimal event emitter used internally for editor events.
     */
    class EventEmitter<T> {
        private listeners;
        on(listener: (data: T) => void): void;
        off(listener: (data: T) => void): void;
        emit(data: T): void;
    }
}
declare namespace CodeEditor.Utils {
    /**
     * Represents a single line in a diff result.
     */
    interface DiffLine {
        type: "equal" | "added" | "removed";
        oldLineNumber: number;
        newLineNumber: number;
        content: string;
    }
    /**
     * Computes a line-level diff between two text documents using the
     * classic dynamic-programming Longest Common Subsequence algorithm.
     * The result is a list of DiffLine entries that can be rendered
     * directly by the DiffViewer.
     */
    function computeLineDiff(oldText: string, newText: string): DiffLine[];
    /**
     * Summarize a diff: number of added / removed lines.
     */
    function summarizeDiff(diff: DiffLine[]): {
        added: number;
        removed: number;
    };
}
declare namespace CodeEditor.Core {
    import EventEmitter = Utils.EventEmitter;
    interface TextChange {
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
    class TextBuffer {
        private lines;
        private _changeEmitter;
        get onChange(): EventEmitter<TextChange>;
        get lineCount(): number;
        getText(): string;
        setText(text: string): void;
        getLine(index: number): string;
        getLines(): string[];
        /**
         * Insert text at the given position. Position is (line, column) where
         * column is a UTF-16 code unit offset within the line.
         */
        insert(line: number, column: number, text: string): void;
        /**
         * Delete text in the given inclusive range (startLine,startColumn) to
         * (endLine,endColumn).
         */
        deleteRange(startLine: number, startColumn: number, endLine: number, endColumn: number): string;
        /**
         * Replace the entire range with new text (combination of delete + insert).
         */
        replaceRange(startLine: number, startColumn: number, endLine: number, endColumn: number, text: string): void;
        /**
         * Return the column index after indenting the given line by one tab
         * (or by tabSize spaces, depending on editor settings).
         */
        getLineLength(line: number): number;
    }
}
declare namespace CodeEditor.Core {
    /**
     * A position within the document: line index and column (UTF-16 code unit).
     */
    interface Position {
        line: number;
        column: number;
    }
    /**
     * A selection range. anchor is where the selection started, active is the
     * current caret position. When they are equal there is no selection.
     */
    interface Selection {
        anchor: Position;
        active: Position;
    }
    /**
     * Manages a single primary selection (caret + optional range).
     */
    class Cursor {
        private _selection;
        get selection(): Selection;
        setSelection(anchor: Position, active: Position): void;
        setPosition(pos: Position, keepAnchor?: boolean): void;
        get position(): Position;
        get hasSelection(): boolean;
        /**
         * Return the selection as an ordered (start <= end) range.
         */
        getOrderedRange(): {
            start: Position;
            end: Position;
        };
    }
}
declare namespace CodeEditor.Core {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import Token = Utils.Token;
    /**
     * Manages per-line tokenization cache for the current document.
     * Re-tokenizes lines when they change, carrying state across lines.
     */
    class Highlighter {
        private highlighter;
        private cache;
        private dirtyFromLine;
        setHighlighter(h: ILanguageHighlighter | null): void;
        get language(): string;
        invalidate(fromLine: number): void;
        invalidateAll(): void;
        /**
         * Ensure cache is valid up to and including `line`. Returns the tokens
         * for that line.
         */
        getTokens(line: number, lineText: string): Token[];
        private retokenize;
        private buffer;
        setBuffer(buffer: TextBuffer): void;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
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
    class VbNetHighlighter implements ILanguageHighlighter {
        readonly language = "vbnet";
        private static CONTROL_KEYWORDS;
        private static KEYWORDS;
        private static TYPES;
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
        private static scanStringBody;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
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
     *   - Statement terminators (; highlighted distinctly in sky blue)
     */
    class RHighlighter implements ILanguageHighlighter {
        readonly language = "r";
        private static CONTROL_KEYWORDS;
        private static KEYWORDS;
        private static CONSTANTS;
        /**
         * R's internal primitive functions (prefix form), sourced from
         * base:::primaries / get("__Primitives__", baseenv()). These are
         * highlighted distinctly from user-defined (third-party) functions.
         * Control-flow keywords and constants are intentionally excluded here
         * so they keep their existing keyword/constant styling.
         */
        private static PRIMITIVES;
        /**
         * R's internal primitive operators (infix/symbol form). When a run of
         * operator characters or an identifier-style operator matches one of
         * these, it is highlighted as a primitive.
         */
        private static PRIMITIVE_OPS;
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * JSON syntax highlighter. Supports per-line state for multi-line strings
     * (rare but valid in some streaming parsers) and tracks whether we are
     * inside an object key context vs. value context for nicer coloring.
     */
    class JsonHighlighter implements ILanguageHighlighter {
        readonly language = "json";
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * XML syntax highlighter with multi-line state tracking.
     * Handles tags, attributes, comments, CDATA, processing instructions.
     */
    class XmlHighlighter implements ILanguageHighlighter {
        readonly language = "xml";
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * Markdown syntax highlighter.
     * Handles headings, bold, italic, code, links, lists, blockquotes,
     * horizontal rules, and fenced code blocks (with state for multi-line).
     */
    class MarkdownHighlighter implements ILanguageHighlighter {
        readonly language = "markdown";
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
        private tokenizeInline;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * YAML syntax highlighter.
     * Handles keys, scalars (string, number, bool, null), comments,
     * block scalars (| and > with multi-line state), anchors and aliases,
     * flow sequences/mappings, and document markers (---, ...).
     */
    class YamlHighlighter implements ILanguageHighlighter {
        readonly language = "yaml";
        initialState(): any;
        tokenizeLine(line: string, state: any): TokenizeResult;
        private tokenizeValue;
        private tokenizeInlineValue;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    import TokenBuilder = Utils.TokenBuilder;
    /**
     * Tokenizer state carried across lines for JavaScript.
     * All fields are plain data so the state is serialisable.
     */
    interface JsState {
        /** Inside a multi-line block comment. */
        inBlockComment: boolean;
        /** Inside a JSDoc block comment (/** ... *‌/). */
        inDocComment: boolean;
        /** Inside a single/double-quoted string that spans a line via \-continuation. */
        inString: boolean;
        /** The quote character of the ongoing string. */
        stringQuote: string;
        /** Inside the text portion of a template literal (between backticks, outside ${}). */
        inTemplate: boolean;
        /** Brace-depth values that would close each open ${} interpolation. */
        templateStack: number[];
        /** Current brace depth, used to match ${ } closers. */
        braceDepth: number;
        /** Last significant token class for regex / division disambiguation. */
        lastSignificant: "value" | "operator" | "none";
    }
    /**
     * JavaScript syntax highlighter with multi-line state tracking.
     *
     * Handles:
     *   - Block comments and JSDoc (multi-line)
     *   - Single-line comments
     *   - Single/double-quoted strings with escapes and \-line-continuation
     *   - Template literals with ${} interpolation (nested, multi-line)
     *   - Regex literals with division-ambiguity resolution
     *   - Numbers (decimal, hex, binary, octal, scientific, BigInt)
     *   - Keywords vs. control-flow keywords (separate colours)
     *   - Built-in global objects
     *   - Function call / definition name detection
     *   - Property access after a dot
     *   - Operators and punctuation
     *
     * The class is designed for inheritance: TypeScriptHighlighter extends it
     * and overrides the keyword/type sets and the language identifier.
     */
    class JavaScriptHighlighter implements ILanguageHighlighter {
        readonly language: string;
        protected static readonly CONTROL_KEYWORDS: Set<string>;
        protected static readonly KEYWORDS: Set<string>;
        /** Keywords that behave syntactically like values. */
        protected static readonly VALUE_KEYWORDS: Set<string>;
        protected static readonly BUILTINS: Set<string>;
        initialState(): JsState;
        /** Sub-classes may override to extend the keyword set. */
        protected isControlKeyword(word: string): boolean;
        protected isKeyword(word: string): boolean;
        protected isValueKeyword(word: string): boolean;
        protected isBuiltin(word: string): boolean;
        /** Whether the word is a built-in type (overridden by TS). */
        protected isType(_word: string): boolean;
        /** Whether decorators (@name) should be parsed (enabled in TS). */
        protected parseDecorators(): boolean;
        tokenizeLine(line: string, state: any): TokenizeResult;
        protected isIdentStart(ch: string): boolean;
        protected isIdentPart(ch: string): boolean;
        protected isOperatorChar(ch: string): boolean;
        protected isNumberStart(line: string, i: number, n: number): boolean;
        protected scanNumber(line: string, i: number, n: number): number;
        /**
         * Scan a regex literal starting at `i` (the opening `/`).
         * Returns the index past the closing `/` (and any flags), or `i`
         * if this doesn't look like a regex.
         */
        protected scanRegex(line: string, i: number, n: number): number;
        /**
         * Scan a single/double-quoted string starting at `i` (the quote).
         * Pushes the string token and returns the new index. If the string
         * is unterminated at end of line (with a trailing \), sets `continues`.
         */
        protected scanString(line: string, i: number, n: number, quote: string, b: TokenBuilder): {
            i: number;
            continues: boolean;
        };
        /**
         * Scan template literal text (the portion between `${}` segments or
         * after the opening backtick). Exits when it encounters `${` (entering
         * an expression) or a closing backtick. Pushes template text as a
         * single TemplateString token and delimiters as TemplateDelimiter.
         *
         * Returns the updated index and state.
         */
        protected scanTemplateText(line: string, i: number, n: number, s: JsState, b: TokenBuilder): {
            i: number;
            state: JsState;
        };
    }
}
declare namespace CodeEditor.Highlighters {
    /**
     * TypeScript syntax highlighter.
     *
     * Extends {@link JavaScriptHighlighter} by overriding the keyword and
     * type sets to inject TypeScript-specific vocabulary, and enabling
     * decorator parsing. No scan logic is duplicated.
     *
     * Additions over JavaScript:
     *   - Type-system keywords (interface, type, enum, implements, declare,
     *     namespace, readonly, abstract, public, private, protected,
     *     satisfies, asserts, keyof, infer, is, module, override, out)
     *   - Built-in primitive types (string, number, boolean, any, unknown,
     *     never, void, object, symbol, bigint)
     *   - Decorators (@Component, @Injectable, …)
     */
    class TypeScriptHighlighter extends JavaScriptHighlighter {
        readonly language = "typescript";
        private static readonly TS_EXTRA_KEYWORDS;
        private static readonly TS_TYPES;
        protected isKeyword(word: string): boolean;
        protected isType(word: string): boolean;
        protected parseDecorators(): boolean;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * Tokenizer state for CSS, carried across lines.
     */
    interface CssState {
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
    class CssHighlighter implements ILanguageHighlighter {
        readonly language = "css";
        private static readonly AT_RULES;
        private static readonly NAMED_COLORS;
        private static readonly UNITS;
        initialState(): CssState;
        tokenizeLine(line: string, state: any): TokenizeResult;
        private isNumberStart;
        private scanNumber;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    import TokenizeResult = Utils.TokenizeResult;
    /**
     * Tokenizer state for HTML, carried across lines.
     *
     * The `mode` field determines how the current line is parsed:
     *  - "html":   normal HTML markup
     *  - "script": inside &lt;script&gt; — delegated to JavaScriptHighlighter
     *  - "style":  inside &lt;style&gt; — delegated to CssHighlighter
     *
     * When in "script" or "style" mode, `subState` holds the sub-highlighter's
     * state object (pure data). Token offsets returned by the sub-highlighter
     * are relative to the fragment passed to it, so they must be offset by the
     * fragment's start position before merging.
     */
    interface HtmlState {
        mode: "html" | "script" | "style";
        /** Inside a multi-line HTML comment. */
        inComment: boolean;
        /** Sub-highlighter state (for script/style delegation). */
        subState: any;
    }
    /**
     * HTML syntax highlighter with embedded-language delegation.
     *
     * Handles:
     *   - DOCTYPE declaration
     *   - HTML comments (multi-line)
     *   - Tag names, attribute names, attribute values
     *   - Entity references (&amp; &lt; etc.)
     *   - Text content
     *   - &lt;script&gt; blocks: delegated to {@link JavaScriptHighlighter}
     *   - &lt;style&gt; blocks: delegated to {@link CssHighlighter}
     *
     * The sub-highlighters are instantiated once in the constructor and reused
     * for every line. Their state is stored in `HtmlState.subState` and carried
     * across lines, enabling multi-line constructs (block comments, template
     * literals, etc.) inside embedded script/style to work correctly.
     */
    class HtmlHighlighter implements ILanguageHighlighter {
        readonly language = "html";
        private readonly jsHighlighter;
        private readonly cssHighlighter;
        constructor();
        initialState(): HtmlState;
        tokenizeLine(line: string, state: any): TokenizeResult;
        private tokenizeHtml;
        /**
         * Parse an HTML tag starting at `i` (the '<').
         * Pushes delimiter, tag name, attributes, and closing '>' tokens.
         * Returns the index past the tag and the tag name (lowercased).
         */
        private parseTag;
        /**
         * Tokenize a line that is (at least partially) inside a &lt;script&gt; or
         * &lt;style&gt; block.
         *
         * The closing tag (e.g. &lt;/script&gt;) is searched for case-insensitively.
         * If found, the text before it is delegated to the sub-highlighter (with
         * an offset if this is a continuation), and the closing tag plus any
         * remaining text is parsed as HTML.
         *
         * @param line      The full line text.
         * @param s         The current HTML state (mode = script|style).
         * @param mode      "script" or "style".
         * @param closeTag  The closing tag literal, e.g. "</script>".
         * @param sub       The sub-highlighter instance.
         * @param offset    If this call is for a substring of the original line,
         *                  the start offset of that substring. Defaults to 0.
         */
        private tokenizeEmbedded;
        /**
         * Add `delta` to every token's start and end offsets.
         * Used when a sub-highlighter processes a fragment of a line.
         */
        private offsetTokens;
    }
}
declare namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;
    /**
     * Registry that maps language identifiers and file extensions to
     * highlighter instances.
     */
    class HighlighterRegistry {
        private static byLanguage;
        private static byExtension;
        static register(highlighter: ILanguageHighlighter, extensions: string[]): void;
        static get(language: string): ILanguageHighlighter | null;
        static getByExtension(ext: string): ILanguageHighlighter | null;
        static getLanguageForExtension(ext: string): string | null;
        static detectFromFilename(filename: string): ILanguageHighlighter | null;
        static listLanguages(): string[];
        /** Register all built-in highlighters. */
        static registerDefaults(): void;
    }
}
declare namespace CodeEditor.Features {
    /**
     * A foldable region in the document.
     */
    interface FoldRange {
        startLine: number;
        endLine: number;
        /** Display text shown when collapsed, e.g. "{...}" or "...". */
        collapsedText: string;
        /** Optional kind label for grouping. */
        kind: string;
    }
    /**
     * Computes foldable regions for a document based on language.
     *
     * The approach is heuristic but works well for the supported languages:
     *   - Indentation-based folding (any line whose indent is less than the
     *     next non-blank line starts a foldable region).
     *   - Bracket-based folding for { }, ( ), [ ], Begin/End, etc.
     *   - Region/End Region markers for VB.NET.
     *   - Markdown headings.
     */
    class CodeFolder {
        /**
         * Compute fold ranges for the given document.
         */
        computeFoldRanges(lines: string[], language: string): FoldRange[];
        private computeVbNet;
        private computeBraceBased;
        /**
         * Brace-based folding for C-style languages (JS/TS/CSS).
         * Handles // and /* *‌/ comments, strings, and template literals.
         * Unlike computeBraceBased, does NOT treat # as a comment start.
         */
        private computeCStyleBraces;
        private computeXml;
        private computeMarkdown;
        private computeIndentation;
        private dedupeRanges;
    }
}
declare namespace CodeEditor.Features {
    /**
     * A navigable symbol in the document.
     */
    interface Symbol {
        name: string;
        kind: SymbolKind;
        line: number;
        column: number;
        endLine?: number;
        detail?: string;
    }
    /**
     * A symbol together with its nested children, forming a tree that mirrors
     * the document's structural hierarchy (namespaces > types > members, etc.).
     */
    interface SymbolNode {
        symbol: Symbol;
        /** Structural nesting level (1 = outermost). Used for tree building. */
        level: number;
        children: SymbolNode[];
    }
    enum SymbolKind {
        Function = "Function",
        Sub = "Sub",
        Property = "Property",
        Class = "Class",
        Module = "Module",
        Structure = "Structure",
        Interface = "Interface",
        Enum = "Enum",
        Namespace = "Namespace",
        Variable = "Variable",
        Heading = "Heading",
        Tag = "Tag",
        Key = "Key",
        Field = "Field"
    }
    /**
     * Extracts navigable symbols from a document based on language.
     */
    class SymbolNavigator {
        extractSymbols(lines: string[], language: string): Symbol[];
        /**
         * Builds a hierarchical tree from a flat symbol list. The nesting level
         * is derived per-language:
         *  - vbnet / r: by declaration kind (Namespace > Type > Member).
         *  - markdown: by heading level (H1 > H2 > ...).
         *  - yaml / json / xml: by leading indentation (column position).
         */
        buildSymbolTree(symbols: Symbol[], language: string): SymbolNode[];
        private levelOf;
        private extractVbNet;
        private extractR;
        private extractJson;
        private extractXml;
        private extractMarkdown;
        private extractYaml;
        /**
         * Extract symbols from JavaScript / TypeScript source.
         * Finds: namespace, class, interface, enum, type alias, function,
         * and arrow-function/const declarations.
         */
        private extractJsTs;
        /**
         * Extract symbols from CSS source.
         * Finds: at-rules (@media, @keyframes, etc.) and selector rules.
         */
        private extractCss;
    }
}
declare namespace CodeEditor.Features {
    import DiffLine = Utils.DiffLine;
    /**
     * Manages git-style diff visualization between the original loaded text
     * and the current edited text.
     */
    class DiffViewer {
        private originalText;
        private currentText;
        private cachedDiff;
        setOriginal(text: string): void;
        setCurrent(text: string): void;
        getDiff(): DiffLine[];
        getSummary(): {
            added: number;
            removed: number;
        };
        /**
         * Returns the diff line index that corresponds to the given new-line
         * number, or -1 if not found. Used to scroll the diff view to match
         * the editor caret.
         */
        findDiffLineForNewLine(newLine: number): number;
        /**
         * Build HTML for the diff view. Each line is a <div> with class
         * diff-equal, diff-added, or diff-removed.
         */
        renderDiffHtml(): string;
    }
}
declare namespace CodeEditor.Features {
    /**
     * A single completion item returned by the REST endpoint.
     */
    interface CompletionItem {
        label: string;
        detail?: string;
        documentation?: string;
        kind: string;
        insertText?: string;
    }
    /**
     * Request payload sent to the REST endpoint.
     */
    interface CompletionRequest {
        language: string;
        text: string;
        line: number;
        column: number;
        trigger?: string;
    }
    /**
     * Response payload from the REST endpoint.
     */
    interface CompletionResponse {
        items: CompletionItem[];
        suggestions?: string[];
    }
    /**
     * Provides intelligent code completion by calling a REST endpoint.
     *
     * The endpoint URL is configurable. If the endpoint is unreachable or
     * returns an error, a small built-in fallback suggestion list is used
     * based on the current language.
     */
    class CompletionProvider {
        private endpoint;
        private enabled;
        setEndpoint(url: string): void;
        getEndpoint(): string;
        setEnabled(enabled: boolean): void;
        /**
         * Request completions asynchronously. Returns a promise that resolves
         * to a list of completion items.
         */
        requestCompletions(req: CompletionRequest): Promise<CompletionItem[]>;
        private callEndpoint;
        /**
         * Built-in fallback suggestions per language. These are static
         * keyword lists that are useful when no backend is configured.
         */
        private fallbackCompletions;
        private vbNetCompletions;
        private rCompletions;
        private jsCompletions;
        private tsCompletions;
        private cssCompletions;
        private htmlCompletions;
    }
}
declare namespace CodeEditor.Features {
    /**
     * Implements the "Go to Line" command. The UI is a small modal dialog
     * with a line number input; this class handles validation and the
     * callback to the editor.
     */
    class GoToLine {
        private maxLine;
        setMaxLine(max: number): void;
        getMaxLine(): number;
        /**
         * Validate a line number string. Returns the parsed line number
         * (1-based) or -1 if invalid.
         */
        validate(input: string): number;
    }
}
declare namespace CodeEditor.Core {
    import Symbol = Features.Symbol;
    import SymbolNode = Features.SymbolNode;
    import DiffViewer = Features.DiffViewer;
    import CompletionProvider = Features.CompletionProvider;
    import GoToLine = Features.GoToLine;
    interface EditorOptions {
        tabSize?: number;
        useSpaces?: boolean;
        fontSize?: number;
        fontFamily?: string;
        lineNumbers?: boolean;
        wordWrap?: boolean;
    }
    /**
     * The main editor controller. Owns the text buffer, cursor, highlighter,
     * and renders into a set of layered DOM elements:
     *   - gutter (line numbers + fold markers)
     *   - code view (highlighted lines)
     *   - caret overlay
     *   - selection overlay
     *   - completion popup
     */
    class Editor {
        private container;
        private gutter;
        private codeView;
        private textarea;
        private scrollContainer;
        private completionPopup;
        private buffer;
        private cursor;
        private highlighter;
        private folder;
        private symbolNav;
        private diffViewer;
        private completionProvider;
        private goToLine;
        private options;
        private language;
        private currentHighlighter;
        private foldRanges;
        private collapsedLines;
        private symbols;
        private filename;
        private charWidth;
        private lineHeight;
        private firstVisibleLine;
        private visibleLineCount;
        private completionItems;
        private completionActive;
        private completionIndex;
        private completionAnchor;
        private minimap;
        private minimapContent;
        private minimapViewport;
        private minimapVisible;
        private minimapLineHeight;
        private minimapDirty;
        private minimapDragging;
        private _suppressCaretScroll;
        private onChangeCallbacks;
        private onCursorChangeCallbacks;
        constructor(container: HTMLElement, options?: EditorOptions);
        private buildDom;
        private measureCharWidth;
        private attachEvents;
        private handleInput;
        private textareaToBufferPos;
        private bufferToTextareaPos;
        /**
         * Sets the textarea caret position without triggering the browser's
         * native caret-scrolling side-effect. The textarea is visually moved
         * to the cursor line via transform in renderCaret(), so letting the
         * browser scroll the container to bring the caret into view would
         * double-count the line offset and scroll to the wrong position.
         */
        private setTextareaSelection;
        private updateCaretFromTextarea;
        private handleKeyDown;
        private handleCodeViewClick;
        private handleGutterClick;
        getText(): string;
        setText(text: string, filename?: string): void;
        setFilename(filename: string): void;
        getFilename(): string;
        setLanguage(language: string): void;
        getLanguage(): string;
        setTheme(theme: "light" | "dark"): void;
        getTheme(): string | null;
        onChange(cb: () => void): void;
        onCursorChange(cb: () => void): void;
        private fireChange;
        private fireCursorChange;
        private recomputeFolds;
        toggleFold(line: number): void;
        isLineCollapsed(line: number): boolean;
        private isLineHiddenByFold;
        private recomputeSymbols;
        getSymbols(): Symbol[];
        getSymbolTree(): SymbolNode[];
        goToSymbol(symbol: Symbol): void;
        getDiffViewer(): DiffViewer;
        toggleDiffView(): void;
        getCompletionProvider(): CompletionProvider;
        private maybeTriggerCompletion;
        private triggerCompletion;
        private renderCompletion;
        private kindIcon;
        private hideCompletion;
        private acceptCompletion;
        getGoToLine(): GoToLine;
        openGoToLineDialog(): void;
        goToLineNumber(line: number): void;
        private scrollToLine;
        exportFile(): void;
        render(): void;
        setMinimapVisible(visible: boolean): void;
        toggleMinimap(): void;
        isMinimapVisible(): boolean;
        private renderMinimap;
        private renderMinimapContent;
        private updateMinimapViewport;
        private scrollFromMinimap;
        private renderGutter;
        private renderCodeView;
        private renderLine;
        private tokenClass;
        private renderCaret;
        focus(): void;
        getCursor(): Cursor;
        getBuffer(): TextBuffer;
    }
}
declare namespace CodeEditor {
    const sample_vb = "\n' VB.NET sample code\nImports System\nImports System.Collections.Generic\n\nNamespace SampleApp\n    Public Class Program\n\n        Private Shared ReadOnly Version As String = \"1.0.0\"\n\n        Public Shared Function Main(args As String()) As Integer\n            Dim numbers As New List(Of Integer)() From {1, 2, 3, 4, 5}\n            Dim total As Integer = 0\n\n            For Each n As Integer In numbers\n                total += n\n            Next\n\n            Console.WriteLine($\"Total: {total}\")\n            Return 0\n        End Function\n\n        Public Property Name As String\n    End Class\nEnd Namespace\n";
    /**
     * Application entry point. Wires up the editor, toolbar, file load/export,
     * symbol navigator panel, diff view panel, go-to-line dialog, and theme
     * switching.
     */
    class App {
        private editor;
        private fileInput;
        private languageSelect;
        private themeSelect;
        private symbolList;
        private diffPanel;
        private diffContent;
        private goToLineDialog;
        private goToLineInput;
        private statusLine;
        private statusCol;
        private statusLang;
        private statusFile;
        private completionEndpointInput;
        private completionStatus;
        private diffVisible;
        constructor();
        private populateLanguages;
        private languageDisplayName;
        setTheme(theme: "light" | "dark"): void;
        setApiEndpoint(url: string): void;
        toggleTheme(): void;
        toggleMinimap(): void;
        private attachEvents;
        private loadSampleContent;
        private loadFile;
        loadFileText(text: string, filename: string): void;
        private exportFile;
        /**
         * export code text to webview2 host
        */
        getCodeText(): string;
        getCodeLanguage(): string;
        private refreshSymbols;
        /**
         * Recursively renders the symbol tree as nested, collapsible rows.
         * `depth` is the visual nesting level (number of ancestors) used for
         * indentation, independent of the structural level used for building.
         */
        private renderSymbolNodes;
        private renderSymbolNode;
        private symbolIcon;
        private toggleDiffView;
        private renderDiff;
        private openGoToLineDialog;
        private closeGoToLineDialog;
        private executeGoToLine;
        private testCompletion;
        private updateStatus;
    }
    function bootstrap(): App;
}
declare namespace CodeEditor {
    interface IDevKit {
        updateStatus(line: string, col: string, lang: string, file: string): void;
    }
    const devkit: IDevKit;
}
