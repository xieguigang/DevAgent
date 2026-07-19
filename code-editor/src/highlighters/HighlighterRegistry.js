"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Highlighters;
    (function (Highlighters) {
        /**
         * Registry that maps language identifiers and file extensions to
         * highlighter instances.
         */
        class HighlighterRegistry {
            static register(highlighter, extensions) {
                this.byLanguage.set(highlighter.language, highlighter);
                for (const ext of extensions) {
                    this.byExtension.set(ext.toLowerCase(), highlighter.language);
                }
            }
            static get(language) {
                return this.byLanguage.get(language) || null;
            }
            static getByExtension(ext) {
                const lang = this.byExtension.get(ext.toLowerCase());
                if (!lang)
                    return null;
                return this.get(lang);
            }
            static getLanguageForExtension(ext) {
                return this.byExtension.get(ext.toLowerCase()) || null;
            }
            static detectFromFilename(filename) {
                const dotIdx = filename.lastIndexOf(".");
                if (dotIdx < 0)
                    return null;
                const ext = filename.substring(dotIdx + 1);
                return this.getByExtension(ext);
            }
            static listLanguages() {
                return Array.from(this.byLanguage.keys());
            }
            /** Register all built-in highlighters. */
            static registerDefaults() {
                this.register(new Highlighters.VbNetHighlighter(), ["vb", "vbnet"]);
                this.register(new Highlighters.RHighlighter(), ["r", "rmd"]);
                this.register(new Highlighters.JsonHighlighter(), ["json", "jsonc"]);
                this.register(new Highlighters.XmlHighlighter(), ["xml", "xsd", "xsl", "xslt", "csproj", "vbproj", "props", "targets", "config"]);
                this.register(new Highlighters.MarkdownHighlighter(), ["md", "markdown"]);
                this.register(new Highlighters.YamlHighlighter(), ["yaml", "yml"]);
            }
        }
        HighlighterRegistry.byLanguage = new Map();
        HighlighterRegistry.byExtension = new Map();
        Highlighters.HighlighterRegistry = HighlighterRegistry;
    })(Highlighters = CodeEditor.Highlighters || (CodeEditor.Highlighters = {}));
})(CodeEditor || (CodeEditor = {}));
