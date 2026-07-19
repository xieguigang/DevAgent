"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        /**
         * Provides intelligent code completion by calling a REST endpoint.
         *
         * The endpoint URL is configurable. If the endpoint is unreachable or
         * returns an error, a small built-in fallback suggestion list is used
         * based on the current language.
         */
        class CompletionProvider {
            constructor() {
                this.endpoint = "";
                this.enabled = true;
            }
            setEndpoint(url) {
                this.endpoint = url;
            }
            getEndpoint() {
                return this.endpoint;
            }
            setEnabled(enabled) {
                this.enabled = enabled;
            }
            /**
             * Request completions asynchronously. Returns a promise that resolves
             * to a list of completion items.
             */
            async requestCompletions(req) {
                if (!this.enabled) {
                    return [];
                }
                if (this.endpoint) {
                    try {
                        const items = await this.callEndpoint(req);
                        return items;
                    }
                    catch (e) {
                        // Fall through to fallback.
                        console.warn("Completion endpoint failed, using fallback:", e);
                    }
                }
                return this.fallbackCompletions(req);
            }
            async callEndpoint(req) {
                const response = await fetch(this.endpoint, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(req)
                });
                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }
                const data = await response.json();
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
            fallbackCompletions(req) {
                switch (req.language) {
                    case "vbnet":
                        return this.vbNetCompletions();
                    case "r":
                        return this.rCompletions();
                    case "json":
                        return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                    case "yaml":
                        return [{ label: "true", kind: "constant" }, { label: "false", kind: "constant" }, { label: "null", kind: "constant" }];
                    default:
                        return [];
                }
            }
            vbNetCompletions() {
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
            rCompletions() {
                const items = [
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
        }
        Features.CompletionProvider = CompletionProvider;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
