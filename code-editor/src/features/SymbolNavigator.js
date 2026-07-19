"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Features;
    (function (Features) {
        let SymbolKind;
        (function (SymbolKind) {
            SymbolKind["Function"] = "Function";
            SymbolKind["Sub"] = "Sub";
            SymbolKind["Property"] = "Property";
            SymbolKind["Class"] = "Class";
            SymbolKind["Module"] = "Module";
            SymbolKind["Structure"] = "Structure";
            SymbolKind["Interface"] = "Interface";
            SymbolKind["Enum"] = "Enum";
            SymbolKind["Namespace"] = "Namespace";
            SymbolKind["Variable"] = "Variable";
            SymbolKind["Heading"] = "Heading";
            SymbolKind["Tag"] = "Tag";
            SymbolKind["Key"] = "Key";
            SymbolKind["Field"] = "Field";
        })(SymbolKind = Features.SymbolKind || (Features.SymbolKind = {}));
        /**
         * Extracts navigable symbols from a document based on language.
         */
        class SymbolNavigator {
            extractSymbols(lines, language) {
                switch (language) {
                    case "vbnet":
                        return this.extractVbNet(lines);
                    case "r":
                        return this.extractR(lines);
                    case "json":
                        return this.extractJson(lines);
                    case "xml":
                        return this.extractXml(lines);
                    case "markdown":
                        return this.extractMarkdown(lines);
                    case "yaml":
                        return this.extractYaml(lines);
                    default:
                        return [];
                }
            }
            extractVbNet(lines) {
                const symbols = [];
                const declRegex = /\b(Public|Private|Protected|Friend|Shared|Overridable|Overrides|Overloads|MustOverride|NotOverridable|Shadows|Iterator|Async|Partial|Static|ReadOnly|WriteOnly|Default|Const|Dim)\s+)*(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Delegate|Field|Const|Dim)\s+([A-Za-z_][A-Za-z0-9_]*)/i;
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const trimmed = line.trim();
                    if (trimmed.startsWith("'") || trimmed.toLowerCase().startsWith("rem "))
                        continue;
                    // Match declarations.
                    const m = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Delegate)\s+([A-Za-z_][A-Za-z0-9_]*)/i.exec(trimmed);
                    if (m) {
                        const kindStr = m[1].charAt(0).toUpperCase() + m[1].slice(1).toLowerCase();
                        let kind;
                        switch (m[1].toLowerCase()) {
                            case "class":
                                kind = SymbolKind.Class;
                                break;
                            case "module":
                                kind = SymbolKind.Module;
                                break;
                            case "structure":
                                kind = SymbolKind.Structure;
                                break;
                            case "interface":
                                kind = SymbolKind.Interface;
                                break;
                            case "enum":
                                kind = SymbolKind.Enum;
                                break;
                            case "namespace":
                                kind = SymbolKind.Namespace;
                                break;
                            case "sub":
                                kind = SymbolKind.Sub;
                                break;
                            case "function":
                                kind = SymbolKind.Function;
                                break;
                            case "property":
                                kind = SymbolKind.Property;
                                break;
                            case "operator":
                                kind = SymbolKind.Function;
                                break;
                            case "event":
                                kind = SymbolKind.Function;
                                break;
                            case "delegate":
                                kind = SymbolKind.Function;
                                break;
                            default: kind = SymbolKind.Function;
                        }
                        const col = line.indexOf(m[2]);
                        symbols.push({
                            name: m[2],
                            kind,
                            line: i,
                            column: col >= 0 ? col : 0,
                            detail: m[1]
                        });
                    }
                }
                return symbols;
            }
            extractR(lines) {
                const symbols = [];
                // Function definitions: name <- function(...) or function name(...) {...}
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Pattern: name <- function
                    const m1 = /^([A-Za-z_.][A-Za-z0-9_.]*)\s*(<-|=)\s*function\b/.exec(line);
                    if (m1) {
                        symbols.push({
                            name: m1[1],
                            kind: SymbolKind.Function,
                            line: i,
                            column: 0,
                            detail: "function"
                        });
                        continue;
                    }
                    // Pattern: function(name)
                    const m2 = /\bfunction\s*\(\s*([A-Za-z_.][A-Za-z0-9_.]*)\s*\)/.exec(line);
                    if (m2) {
                        symbols.push({
                            name: m2[1],
                            kind: SymbolKind.Function,
                            line: i,
                            column: m2.index,
                            detail: "function"
                        });
                        continue;
                    }
                    // Variable assignment: name <- value (top-level only, simple heuristic).
                    const m3 = /^([A-Za-z_.][A-Za-z0-9_.]*)\s*<-\s*(?!function\b)/.exec(line);
                    if (m3) {
                        symbols.push({
                            name: m3[1],
                            kind: SymbolKind.Variable,
                            line: i,
                            column: 0,
                            detail: "variable"
                        });
                    }
                }
                return symbols;
            }
            extractJson(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    // Match "key":
                    const m = /^\s*"([^"]+)"\s*:/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[1],
                            kind: SymbolKind.Key,
                            line: i,
                            column: line.indexOf('"'),
                            detail: "key"
                        });
                    }
                }
                return symbols;
            }
            extractXml(lines) {
                const symbols = [];
                const seen = new Set();
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /<([A-Za-z_][\w\-.:]*)\b[^>]*>/g;
                    let match;
                    while ((match = m.exec(line)) !== null) {
                        const tag = match[1];
                        if (tag.startsWith("/"))
                            continue;
                        if (seen.has(tag))
                            continue;
                        seen.add(tag);
                        symbols.push({
                            name: tag,
                            kind: SymbolKind.Tag,
                            line: i,
                            column: match.index,
                            detail: "tag"
                        });
                    }
                }
                return symbols;
            }
            extractMarkdown(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /^(#{1,6})\s+(.+?)\s*$/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[2],
                            kind: SymbolKind.Heading,
                            line: i,
                            column: 0,
                            detail: "H" + m[1].length
                        });
                    }
                }
                return symbols;
            }
            extractYaml(lines) {
                const symbols = [];
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i];
                    const m = /^(\s*)([A-Za-z_][A-Za-z0-9_\-\.]*)\s*:/.exec(line);
                    if (m) {
                        symbols.push({
                            name: m[2],
                            kind: SymbolKind.Key,
                            line: i,
                            column: m[1].length,
                            detail: "key"
                        });
                    }
                }
                return symbols;
            }
        }
        Features.SymbolNavigator = SymbolNavigator;
    })(Features = CodeEditor.Features || (CodeEditor.Features = {}));
})(CodeEditor || (CodeEditor = {}));
