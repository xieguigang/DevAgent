namespace CodeEditor.Features {
    /**
     * A navigable symbol in the document.
     */
    export interface Symbol {
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
    export interface SymbolNode {
        symbol: Symbol;
        /** Structural nesting level (1 = outermost). Used for tree building. */
        level: number;
        children: SymbolNode[];
    }

    export enum SymbolKind {
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
    export class SymbolNavigator {
        extractSymbols(lines: string[], language: string): Symbol[] {
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

        /**
         * Builds a hierarchical tree from a flat symbol list. The nesting level
         * is derived per-language:
         *  - vbnet / r: by declaration kind (Namespace > Type > Member).
         *  - markdown: by heading level (H1 > H2 > ...).
         *  - yaml / json / xml: by leading indentation (column position).
         */
        buildSymbolTree(symbols: Symbol[], language: string): SymbolNode[] {
            const root: SymbolNode[] = [];
            const stack: { node: SymbolNode; level: number }[] = [];

            for (const sym of symbols) {
                const level = this.levelOf(sym, language);
                while (stack.length > 0 && stack[stack.length - 1].level >= level) {
                    stack.pop();
                }
                const parentNode = stack.length > 0 ? stack[stack.length - 1].node : null;
                const node: SymbolNode = { symbol: sym, level, children: [] };
                if (parentNode) {
                    parentNode.children.push(node);
                } else {
                    root.push(node);
                }
                stack.push({ node, level });
            }
            return root;
        }

        private levelOf(sym: Symbol, language: string): number {
            switch (language) {
                case "markdown": {
                    const m = /^H(\d+)$/.exec(sym.detail || "");
                    return m ? parseInt(m[1], 10) : 1;
                }
                case "yaml":
                case "json":
                case "xml":
                    // Indentation-based nesting: deeper keys sit further right.
                    return sym.column;
                case "vbnet":
                case "r":
                default:
                    switch (sym.kind) {
                        case SymbolKind.Namespace:
                            return 1;
                        case SymbolKind.Module:
                        case SymbolKind.Class:
                        case SymbolKind.Structure:
                        case SymbolKind.Interface:
                        case SymbolKind.Enum:
                            return 2;
                        default:
                            return 3;
                    }
            }
        }

        private extractVbNet(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];

            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                const trimmed = line.trim();
                if (trimmed.startsWith("'") || trimmed.toLowerCase().startsWith("rem ")) continue;

                // Match declarations.
                const m = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Delegate)\s+([A-Za-z_][A-Za-z0-9_]*)/i.exec(trimmed);
                if (m) {
                    const kindStr = m[1].charAt(0).toUpperCase() + m[1].slice(1).toLowerCase();
                    let kind: SymbolKind;
                    switch (m[1].toLowerCase()) {
                        case "class": kind = SymbolKind.Class; break;
                        case "module": kind = SymbolKind.Module; break;
                        case "structure": kind = SymbolKind.Structure; break;
                        case "interface": kind = SymbolKind.Interface; break;
                        case "enum": kind = SymbolKind.Enum; break;
                        case "namespace": kind = SymbolKind.Namespace; break;
                        case "sub": kind = SymbolKind.Sub; break;
                        case "function": kind = SymbolKind.Function; break;
                        case "property": kind = SymbolKind.Property; break;
                        case "operator": kind = SymbolKind.Function; break;
                        case "event": kind = SymbolKind.Function; break;
                        case "delegate": kind = SymbolKind.Function; break;
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

        private extractR(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];
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

        private extractJson(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];
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

        private extractXml(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];
            const seen = new Set<string>();
            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                const m = /<([A-Za-z_][\w\-.:]*)\b[^>]*>/g;
                let match: RegExpExecArray | null;
                while ((match = m.exec(line)) !== null) {
                    const tag = match[1];
                    if (tag.startsWith("/")) continue;
                    if (seen.has(tag)) continue;
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

        private extractMarkdown(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];
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

        private extractYaml(lines: string[]): Symbol[] {
            const symbols: Symbol[] = [];
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
}
