namespace CodeEditor.Utils {
    /**
     * Common token types used across all highlighters.
     */
    export enum TokenType {
        Plain,
        Keyword,
        ControlKeyword,
        Identifier,
        Type,
        String,
        Number,
        Comment,
        Operator,
        Punctuation,
        Preprocessor,
        Attribute,
        Tag,
        AttrName,
        AttrValue,
        XmlDelimiter,
        XmlText,
        Heading,
        Bold,
        Italic,
        Code,
        Link,
        ListMarker,
        Quote,
        Property,
        Function,
        Constant,
        Annotation,
        DocComment,
        Error
    }

    /**
     * A single token produced by a highlighter.
     */
    export interface Token {
        type: TokenType;
        value: string;
        start: number;
        end: number;
    }

    /**
     * Result of tokenizing a line: list of tokens plus optional state to carry
     * over to the next line (for multi-line constructs like block comments).
     */
    export interface TokenizeResult {
        tokens: Token[];
        state: any;
    }

    /**
     * Interface that every language highlighter must implement.
     */
    export interface ILanguageHighlighter {
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
    export class TokenBuilder {
        private tokens: Token[] = [];
        private pos: number = 0;

        push(type: TokenType, value: string): void {
            const start = this.pos;
            this.pos += value.length;
            this.tokens.push({ type, value, start, end: this.pos });
        }

        advance(n: number): void {
            this.pos += n;
        }

        get position(): number {
            return this.pos;
        }

        set position(value: number) {
            this.pos = value;
        }

        get result(): Token[] {
            return this.tokens;
        }
    }

    /**
     * Match a regex at the current position; returns the match or null.
     */
    export function matchAt(regex: RegExp, text: string, pos: number): RegExpExecArray | null {
        // Use sticky flag emulation by anchoring with substring.
        const slice = text.substr(pos);
        const m = regex.exec(slice);
        if (m && m.index === 0) {
            return m;
        }
        return null;
    }

    /**
     * Escape HTML special characters for safe insertion into innerHTML.
     */
    export function escapeHtml(text: string): string {
        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }
}
