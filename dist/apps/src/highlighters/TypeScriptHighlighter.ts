namespace CodeEditor.Highlighters {
    import TokenType = Utils.TokenType;
    import TokenBuilder = Utils.TokenBuilder;

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
    export class TypeScriptHighlighter extends JavaScriptHighlighter {
        readonly language = "typescript";

        private static readonly TS_EXTRA_KEYWORDS = new Set<string>([
            "interface", "type", "enum", "implements", "declare", "namespace",
            "readonly", "abstract", "public", "private", "protected",
            "satisfies", "asserts", "keyof", "infer", "is", "module",
            "override", "global", "unique"
        ]);

        private static readonly TS_TYPES = new Set<string>([
            "string", "number", "boolean", "any", "unknown", "never", "void",
            "object", "symbol", "bigint", "String", "Number", "Boolean",
            "Symbol", "BigInt", "Object", "Array", "ReadonlyArray", "Map",
            "Set", "ReadonlyMap", "ReadonlySet", "Promise", "Date", "RegExp",
            "Error", "Record", "Partial", "Required", "Readonly", "Pick",
            "Omit", "Exclude", "Extract", "NonNullable", "Parameters",
            "ConstructorParameters", "ReturnType", "InstanceType",
            "ThisType", "Awaited", "Lowercase", "Uppercase", "Capitalize",
            "Uncapitalize", "Function", "Error"
        ]);

        protected isKeyword(word: string): boolean {
            return super.isKeyword(word) || TypeScriptHighlighter.TS_EXTRA_KEYWORDS.has(word);
        }

        protected isType(word: string): boolean {
            return TypeScriptHighlighter.TS_TYPES.has(word);
        }

        protected parseDecorators(): boolean {
            return true;
        }
    }
}
