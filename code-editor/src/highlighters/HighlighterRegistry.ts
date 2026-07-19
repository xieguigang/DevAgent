namespace CodeEditor.Highlighters {
    import ILanguageHighlighter = Utils.ILanguageHighlighter;

    /**
     * Registry that maps language identifiers and file extensions to
     * highlighter instances.
     */
    export class HighlighterRegistry {
        private static byLanguage = new Map<string, ILanguageHighlighter>();
        private static byExtension = new Map<string, string>();

        static register(highlighter: ILanguageHighlighter, extensions: string[]): void {
            this.byLanguage.set(highlighter.language, highlighter);
            for (const ext of extensions) {
                this.byExtension.set(ext.toLowerCase(), highlighter.language);
            }
        }

        static get(language: string): ILanguageHighlighter | null {
            return this.byLanguage.get(language) || null;
        }

        static getByExtension(ext: string): ILanguageHighlighter | null {
            const lang = this.byExtension.get(ext.toLowerCase());
            if (!lang) return null;
            return this.get(lang);
        }

        static getLanguageForExtension(ext: string): string | null {
            return this.byExtension.get(ext.toLowerCase()) || null;
        }

        static detectFromFilename(filename: string): ILanguageHighlighter | null {
            const dotIdx = filename.lastIndexOf(".");
            if (dotIdx < 0) return null;
            const ext = filename.substring(dotIdx + 1);
            return this.getByExtension(ext);
        }

        static listLanguages(): string[] {
            return Array.from(this.byLanguage.keys());
        }

        /** Register all built-in highlighters. */
        static registerDefaults(): void {
            this.register(new VbNetHighlighter(), ["vb", "vbnet"]);
            this.register(new RHighlighter(), ["r", "rmd"]);
            this.register(new JsonHighlighter(), ["json", "jsonc"]);
            this.register(new XmlHighlighter(), ["xml", "xsd", "xsl", "xslt", "csproj", "vbproj", "props", "targets", "config"]);
            this.register(new MarkdownHighlighter(), ["md", "markdown"]);
            this.register(new YamlHighlighter(), ["yaml", "yml"]);
        }
    }
}
