namespace CodeEditor.Features {
    /**
     * A single completion item returned by the REST endpoint.
     */
    export interface CompletionItem {
        label: string;
        detail?: string;
        documentation?: string;
        kind: string; // "function", "variable", "keyword", "snippet", etc.
        insertText?: string;
    }

    /**
     * Request payload sent to the REST endpoint.
     */
    export interface CompletionRequest {
        language: string;
        text: string;
        line: number;
        column: number;
        trigger?: string;
    }

    /**
     * Response payload from the REST endpoint.
     */
    export interface CompletionResponse {
        items: CompletionItem[];
        suggestions?: string[]; // alternative simple string list
    }

    /**
     * Provides intelligent code completion by calling a REST endpoint.
     *
     * The endpoint URL is configurable. If the endpoint is unreachable or
     * returns an error, a small built-in fallback suggestion list is used
     * based on the current language.
     */
    export class CompletionProvider {
        private endpoint: string = "";
        private enabled: boolean = true;

        setEndpoint(url: string): void {
            this.endpoint = url;
        }

        getEndpoint(): string {
            return this.endpoint;
        }

        setEnabled(enabled: boolean): void {
            this.enabled = enabled;
        }

        /**
         * Request completions asynchronously. Returns a promise that resolves
         * to a list of completion items.
         */
        async requestCompletions(req: CompletionRequest): Promise<CompletionItem[]> {
            if (!this.enabled) {
                return [];
            }

            if (this.endpoint) {
                try {
                    const items = await this.callEndpoint(req);
                    return items;
                } catch (e) {
                    // Fall through to fallback.
                    console.warn("Completion endpoint failed, using fallback:", e);
                }
            }

            return this.fallbackCompletions(req);
        }

        private async callEndpoint(req: CompletionRequest): Promise<CompletionItem[]> {
            const response = await fetch(this.endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(req)
            });
            if (!response.ok) {
                throw new Error("HTTP " + response.status);
            }
            const data: CompletionResponse = await response.json();
            if (data.items && Array.isArray(data.items)) {
                return data.items;
            }
            if (data.suggestions && Array.isArray(data.suggestions)) {
                return data.suggestions.map(s => ({ label: s, kind: "text" }));
            }
            return [];
        }

        /**
         * Built-in fallback suggestions per language. These are static
         * keyword lists that are useful when no backend is configured.
         */
        private fallbackCompletions(req: CompletionRequest): CompletionItem[] {
            switch (req.language) {
                case "vbnet":
                    return this.vbNetCompletions();
                case "r":
                    return this.rCompletions();
                case "json":
                    return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                case "yaml":
                    return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                case "javascript":
                    return this.jsCompletions();
                case "typescript":
                    return this.tsCompletions();
                case "css":
                    return this.cssCompletions();
                case "html":
                    return this.htmlCompletions();
                default:
                    return [];
            }
        }

        private vbNetCompletions(): CompletionItem[] {
            const keywords = [
                "Public", "Private", "Protected", "Friend", "Shared", "ReadOnly", "WriteOnly",
                "Class", "Module", "Structure", "Interface", "Enum", "Namespace",
                "Sub", "Function", "Property", "Operator", "Event", "Delegate",
                "Dim", "Const", "Static", "If", "Then", "Else", "ElseIf", "End If",
                "For", "Each", "In", "Next", "While", "Do", "Loop", "Until",
                "Try", "Catch", "Finally", "Throw", "Using", "SyncLock",
                "Return", "Yield", "Await", "Async", "Iterator", "Partial",
                "Inherits", "Implements", "Of", "As", "New", "Me", "MyBase",
                "Nothing", "True", "False", "And", "Or", "Not", "AndAlso", "OrElse",
                "ByVal", "ByRef", "Optional", "ParamArray", "Handles", "AddressOf",
                "GetType", "TypeOf", "DirectCast", "TryCast", "CType"
            ];
            return keywords.map(k => ({ label: k, kind: "keyword", insertText: k }));
        }

        private rCompletions(): CompletionItem[] {
            const items: CompletionItem[] = [
                { label: "if", kind: "keyword" },
                { label: "else", kind: "keyword" },
                { label: "for", kind: "keyword" },
                { label: "while", kind: "keyword" },
                { label: "function", kind: "keyword", insertText: "function() {\n  \n}" },
                { label: "return", kind: "keyword" },
                { label: "library", kind: "function", insertText: "library()" },
                { label: "require", kind: "function", insertText: "require()" },
                { label: "source", kind: "function", insertText: "source('')" },
                { label: "print", kind: "function", insertText: "print()" },
                { label: "cat", kind: "function", insertText: "cat()" },
                { label: "paste", kind: "function", insertText: "paste()" },
                { label: "paste0", kind: "function", insertText: "paste0()" },
                { label: "sprintf", kind: "function", insertText: "sprintf()" },
                { label: "c", kind: "function", insertText: "c()" },
                { label: "list", kind: "function", insertText: "list()" },
                { label: "vector", kind: "function", insertText: "vector()" },
                { label: "matrix", kind: "function", insertText: "matrix()" },
                { label: "data.frame", kind: "function", insertText: "data.frame()" },
                { label: "factor", kind: "function", insertText: "factor()" },
                { label: "length", kind: "function", insertText: "length()" },
                { label: "nrow", kind: "function", insertText: "nrow()" },
                { label: "ncol", kind: "function", insertText: "ncol()" },
                { label: "names", kind: "function", insertText: "names()" },
                { label: "rownames", kind: "function", insertText: "rownames()" },
                { label: "colnames", kind: "function", insertText: "colnames()" },
                { label: "head", kind: "function", insertText: "head()" },
                { label: "tail", kind: "function", insertText: "tail()" },
                { label: "summary", kind: "function", insertText: "summary()" },
                { label: "str", kind: "function", insertText: "str()" },
                { label: "mean", kind: "function", insertText: "mean()" },
                { label: "median", kind: "function", insertText: "median()" },
                { label: "sd", kind: "function", insertText: "sd()" },
                { label: "var", kind: "function", insertText: "var()" },
                { label: "sum", kind: "function", insertText: "sum()" },
                { label: "min", kind: "function", insertText: "min()" },
                { label: "max", kind: "function", insertText: "max()" },
                { label: "range", kind: "function", insertText: "range()" },
                { label: "TRUE", kind: "constant" },
                { label: "FALSE", kind: "constant" },
                { label: "NULL", kind: "constant" },
                { label: "NA", kind: "constant" },
                { label: "Inf", kind: "constant" },
                { label: "NaN", kind: "constant" },
                { label: "pi", kind: "constant" }
            ];
            return items;
        }

        private jsCompletions(): CompletionItem[] {
            const items: CompletionItem[] = [
                // Control flow
                { label: "if", kind: "keyword" },
                { label: "else", kind: "keyword" },
                { label: "for", kind: "keyword" },
                { label: "while", kind: "keyword" },
                { label: "do", kind: "keyword" },
                { label: "switch", kind: "keyword" },
                { label: "case", kind: "keyword" },
                { label: "break", kind: "keyword" },
                { label: "continue", kind: "keyword" },
                { label: "return", kind: "keyword" },
                { label: "throw", kind: "keyword" },
                { label: "try", kind: "keyword" },
                { label: "catch", kind: "keyword" },
                { label: "finally", kind: "keyword" },
                // Declarations
                { label: "var", kind: "keyword" },
                { label: "let", kind: "keyword" },
                { label: "const", kind: "keyword" },
                { label: "function", kind: "keyword", insertText: "function () {\n  \n}" },
                { label: "class", kind: "keyword", insertText: "class  {\n  \n}" },
                { label: "extends", kind: "keyword" },
                { label: "new", kind: "keyword" },
                { label: "this", kind: "keyword" },
                { label: "super", kind: "keyword" },
                { label: "import", kind: "keyword" },
                { label: "export", kind: "keyword" },
                { label: "default", kind: "keyword" },
                { label: "from", kind: "keyword" },
                { label: "as", kind: "keyword" },
                { label: "async", kind: "keyword" },
                { label: "await", kind: "keyword" },
                { label: "typeof", kind: "keyword" },
                { label: "instanceof", kind: "keyword" },
                { label: "in", kind: "keyword" },
                { label: "of", kind: "keyword" },
                { label: "delete", kind: "keyword" },
                { label: "void", kind: "keyword" },
                // Constants
                { label: "true", kind: "constant" },
                { label: "false", kind: "constant" },
                { label: "null", kind: "constant" },
                { label: "undefined", kind: "constant" },
                { label: "NaN", kind: "constant" },
                { label: "Infinity", kind: "constant" },
                // Built-ins
                { label: "console", kind: "variable" },
                { label: "console.log", kind: "function", insertText: "console.log()" },
                { label: "console.error", kind: "function", insertText: "console.error()" },
                { label: "console.warn", kind: "function", insertText: "console.warn()" },
                { label: "Math", kind: "class" },
                { label: "JSON", kind: "class" },
                { label: "JSON.parse", kind: "function", insertText: "JSON.parse()" },
                { label: "JSON.stringify", kind: "function", insertText: "JSON.stringify()" },
                { label: "Promise", kind: "class" },
                { label: "Array", kind: "class" },
                { label: "Object", kind: "class" },
                { label: "String", kind: "class" },
                { label: "Number", kind: "class" },
                { label: "Boolean", kind: "class" },
                { label: "Date", kind: "class" },
                { label: "RegExp", kind: "class" },
                { label: "Map", kind: "class" },
                { label: "Set", kind: "class" },
                { label: "setTimeout", kind: "function", insertText: "setTimeout()" },
                { label: "setInterval", kind: "function", insertText: "setInterval()" },
                { label: "parseInt", kind: "function", insertText: "parseInt()" },
                { label: "parseFloat", kind: "function", insertText: "parseFloat()" }
            ];
            return items;
        }

        private tsCompletions(): CompletionItem[] {
            const items = this.jsCompletions();
            // TypeScript-specific keywords.
            items.push(
                { label: "interface", kind: "keyword", insertText: "interface  {\n  \n}" },
                { label: "type", kind: "keyword", insertText: "type  = " },
                { label: "enum", kind: "keyword", insertText: "enum  {\n  \n}" },
                { label: "implements", kind: "keyword" },
                { label: "declare", kind: "keyword" },
                { label: "namespace", kind: "keyword" },
                { label: "readonly", kind: "keyword" },
                { label: "abstract", kind: "keyword" },
                { label: "public", kind: "keyword" },
                { label: "private", kind: "keyword" },
                { label: "protected", kind: "keyword" },
                { label: "static", kind: "keyword" },
                { label: "override", kind: "keyword" },
                { label: "keyof", kind: "keyword" },
                { label: "infer", kind: "keyword" },
                { label: "is", kind: "keyword" },
                { label: "as", kind: "keyword" },
                { label: "satisfies", kind: "keyword" },
                // Built-in types
                { label: "string", kind: "type" },
                { label: "number", kind: "type" },
                { label: "boolean", kind: "type" },
                { label: "any", kind: "type" },
                { label: "unknown", kind: "type" },
                { label: "never", kind: "type" },
                { label: "void", kind: "type" },
                { label: "object", kind: "type" },
                { label: "symbol", kind: "type" },
                { label: "bigint", kind: "type" },
                { label: "Record", kind: "type", insertText: "Record<,>" },
                { label: "Partial", kind: "type", insertText: "Partial<>" },
                { label: "Readonly", kind: "type", insertText: "Readonly<>" },
                { label: "Pick", kind: "type", insertText: "Pick<,>" },
                { label: "Omit", kind: "type", insertText: "Omit<,>" },
                { label: "ReturnType", kind: "type", insertText: "ReturnType<>" },
                { label: "Parameters", kind: "type", insertText: "Parameters<>" }
            );
            return items;
        }

        private cssCompletions(): CompletionItem[] {
            const items: CompletionItem[] = [
                // At-rules
                { label: "@media", kind: "keyword", insertText: "@media () {\n  \n}" },
                { label: "@keyframes", kind: "keyword", insertText: "@keyframes  {\n  \n}" },
                { label: "@import", kind: "keyword", insertText: "@import '';" },
                { label: "@supports", kind: "keyword", insertText: "@supports () {\n  \n}" },
                { label: "@font-face", kind: "keyword", insertText: "@font-face {\n  \n}" },
                { label: "@charset", kind: "keyword", insertText: "@charset '';" },
                { label: "@page", kind: "keyword" },
                { label: "@layer", kind: "keyword" },
                { label: "@container", kind: "keyword" },
                // Common properties
                { label: "display", kind: "property" },
                { label: "position", kind: "property" },
                { label: "top", kind: "property" },
                { label: "right", kind: "property" },
                { label: "bottom", kind: "property" },
                { label: "left", kind: "property" },
                { label: "width", kind: "property" },
                { label: "height", kind: "property" },
                { label: "min-width", kind: "property" },
                { label: "max-width", kind: "property" },
                { label: "margin", kind: "property" },
                { label: "padding", kind: "property" },
                { label: "border", kind: "property" },
                { label: "border-radius", kind: "property" },
                { label: "color", kind: "property" },
                { label: "background", kind: "property" },
                { label: "background-color", kind: "property" },
                { label: "font-family", kind: "property" },
                { label: "font-size", kind: "property" },
                { label: "font-weight", kind: "property" },
                { label: "line-height", kind: "property" },
                { label: "text-align", kind: "property" },
                { label: "text-decoration", kind: "property" },
                { label: "flex", kind: "property" },
                { label: "flex-direction", kind: "property" },
                { label: "flex-wrap", kind: "property" },
                { label: "justify-content", kind: "property" },
                { label: "align-items", kind: "property" },
                { label: "grid", kind: "property" },
                { label: "grid-template-columns", kind: "property" },
                { label: "grid-template-rows", kind: "property" },
                { label: "gap", kind: "property" },
                { label: "z-index", kind: "property" },
                { label: "opacity", kind: "property" },
                { label: "overflow", kind: "property" },
                { label: "cursor", kind: "property" },
                { label: "transition", kind: "property" },
                { label: "transform", kind: "property" },
                { label: "animation", kind: "property" },
                { label: "box-shadow", kind: "property" },
                // Common values
                { label: "none", kind: "value" },
                { label: "auto", kind: "value" },
                { label: "inherit", kind: "value" },
                { label: "initial", kind: "value" },
                { label: "unset", kind: "value" },
                { label: "block", kind: "value" },
                { label: "inline", kind: "value" },
                { label: "flex", kind: "value" },
                { label: "grid", kind: "value" },
                { label: "absolute", kind: "value" },
                { label: "relative", kind: "value" },
                { label: "fixed", kind: "value" },
                { label: "sticky", kind: "value" },
                { label: "center", kind: "value" },
                { label: "space-between", kind: "value" },
                { label: "space-around", kind: "value" },
                { label: "wrap", kind: "value" },
                { label: "nowrap", kind: "value" },
                { label: "solid", kind: "value" },
                { label: "dashed", kind: "value" },
                { label: "dotted", kind: "value" },
                { label: "bold", kind: "value" },
                { label: "italic", kind: "value" },
                { label: "pointer", kind: "value" },
                { label: "default", kind: "value" },
                // Functions
                { label: "var()", kind: "function", insertText: "var(--)" },
                { label: "calc()", kind: "function", insertText: "calc()" },
                { label: "rgb()", kind: "function", insertText: "rgb(, , )" },
                { label: "rgba()", kind: "function", insertText: "rgba(, , , )" },
                { label: "hsl()", kind: "function", insertText: "hsl(, , )" },
                { label: "url()", kind: "function", insertText: "url('')" },
                { label: "linear-gradient()", kind: "function", insertText: "linear-gradient()" },
                // !important
                { label: "!important", kind: "keyword" }
            ];
            return items;
        }

        private htmlCompletions(): CompletionItem[] {
            const items: CompletionItem[] = [
                // Structural tags
                { label: "html", kind: "keyword", insertText: "<html>\n  \n</html>" },
                { label: "head", kind: "keyword", insertText: "<head>\n  \n</head>" },
                { label: "body", kind: "keyword", insertText: "<body>\n  \n</body>" },
                { label: "div", kind: "keyword", insertText: "<div>\n  \n</div>" },
                { label: "span", kind: "keyword", insertText: "<span></span>" },
                { label: "section", kind: "keyword", insertText: "<section>\n  \n</section>" },
                { label: "header", kind: "keyword", insertText: "<header>\n  \n</header>" },
                { label: "footer", kind: "keyword", insertText: "<footer>\n  \n</footer>" },
                { label: "nav", kind: "keyword", insertText: "<nav>\n  \n</nav>" },
                { label: "main", kind: "keyword", insertText: "<main>\n  \n</main>" },
                { label: "article", kind: "keyword", insertText: "<article>\n  \n</article>" },
                { label: "aside", kind: "keyword", insertText: "<aside>\n  \n</aside>" },
                // Text tags
                { label: "h1", kind: "keyword", insertText: "<h1></h1>" },
                { label: "h2", kind: "keyword", insertText: "<h2></h2>" },
                { label: "h3", kind: "keyword", insertText: "<h3></h3>" },
                { label: "p", kind: "keyword", insertText: "<p></p>" },
                { label: "a", kind: "keyword", insertText: "<a href=\"\"></a>" },
                { label: "strong", kind: "keyword", insertText: "<strong></strong>" },
                { label: "em", kind: "keyword", insertText: "<em></em>" },
                { label: "br", kind: "keyword", insertText: "<br>" },
                { label: "hr", kind: "keyword", insertText: "<hr>" },
                // Lists
                { label: "ul", kind: "keyword", insertText: "<ul>\n  \n</ul>" },
                { label: "ol", kind: "keyword", insertText: "<ol>\n  \n</ol>" },
                { label: "li", kind: "keyword", insertText: "<li></li>" },
                // Media
                { label: "img", kind: "keyword", insertText: "<img src=\"\" alt=\"\">" },
                { label: "video", kind: "keyword", insertText: "<video src=\"\"></video>" },
                { label: "audio", kind: "keyword", insertText: "<audio src=\"\"></audio>" },
                { label: "canvas", kind: "keyword", insertText: "<canvas></canvas>" },
                { label: "svg", kind: "keyword", insertText: "<svg></svg>" },
                // Forms
                { label: "form", kind: "keyword", insertText: "<form action=\"\"></form>" },
                { label: "input", kind: "keyword", insertText: "<input type=\"\">" },
                { label: "button", kind: "keyword", insertText: "<button></button>" },
                { label: "label", kind: "keyword", insertText: "<label></label>" },
                { label: "select", kind: "keyword", insertText: "<select>\n  \n</select>" },
                { label: "option", kind: "keyword", insertText: "<option value=\"\"></option>" },
                { label: "textarea", kind: "keyword", insertText: "<textarea></textarea>" },
                // Tables
                { label: "table", kind: "keyword", insertText: "<table>\n  \n</table>" },
                { label: "tr", kind: "keyword", insertText: "<tr>\n  \n</tr>" },
                { label: "td", kind: "keyword", insertText: "<td></td>" },
                { label: "th", kind: "keyword", insertText: "<th></th>" },
                { label: "thead", kind: "keyword", insertText: "<thead>\n  \n</thead>" },
                { label: "tbody", kind: "keyword", insertText: "<tbody>\n  \n</tbody>" },
                // Script / Style
                { label: "script", kind: "keyword", insertText: "<script>\n  \n</script>" },
                { label: "style", kind: "keyword", insertText: "<style>\n  \n</style>" },
                { label: "link", kind: "keyword", insertText: "<link rel=\"\" href=\"\">" },
                { label: "meta", kind: "keyword", insertText: "<meta >" },
                // Common attributes
                { label: "class", kind: "property" },
                { label: "id", kind: "property" },
                { label: "style", kind: "property" },
                { label: "href", kind: "property" },
                { label: "src", kind: "property" },
                { label: "alt", kind: "property" },
                { label: "title", kind: "property" },
                { label: "type", kind: "property" },
                { label: "name", kind: "property" },
                { label: "value", kind: "property" },
                { label: "placeholder", kind: "property" },
                { label: "onclick", kind: "property" },
                { label: "onload", kind: "property" },
                { label: "disabled", kind: "property" },
                { label: "readonly", kind: "property" },
                { label: "checked", kind: "property" },
                { label: "required", kind: "property" },
                { label: "data-", kind: "property" },
                { label: "aria-", kind: "property" }
            ];
            return items;
        }
    }
}
