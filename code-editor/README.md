# Code Editor

A lightweight, dependency-free code editor built with HTML and TypeScript.
Compiled to a single JavaScript bundle — no npm packages, no bundlers, no
external runtime dependencies.

## Features

### 1. Syntax Highlighting

Full syntax highlighting for six languages:

| Language   | Extensions                          |
|------------|-------------------------------------|
| VB.NET     | `.vb`, `.vbnet`                     |
| R          | `.r`, `.rmd`                        |
| JSON       | `.json`, `.jsonc`                   |
| XML        | `.xml`, `.xsd`, `.xsl`, `.csproj`, `.vbproj`, `.props`, `.targets`, `.config` |
| Markdown   | `.md`, `.markdown`                  |
| YAML       | `.yaml`, `.yml`                     |

VB.NET and R are the primary supported languages with the richest tokenization
(keywords, control flow, types, functions, constants, comments, strings,
numbers, preprocessor directives, etc.).

### 2. Code Folding

Click the fold markers in the gutter (or use the triangle icons) to collapse
and expand code blocks. Folding is computed per-language:

- **VB.NET**: `Class`/`Module`/`Structure`/`Interface`/`Enum`/`Namespace`/
  `Sub`/`Function`/`Property` blocks, `#Region`/`#End Region`, and `Begin`/`End`
  pairs.
- **R / JSON / YAML**: brace-based `{ }`, `( )`, `[ ]` folding.
- **XML**: element-tag folding.
- **Markdown**: heading-section folding.

### 3. Symbol Navigation

Open the **Symbols** panel from the toolbar to see a navigable outline of the
current document. Click any symbol to jump to its definition. Supported symbol
kinds:

- VB.NET: `Class`, `Module`, `Structure`, `Interface`, `Enum`, `Namespace`,
  `Sub`, `Function`, `Property`, `Field`
- R: `Function`, `Variable`
- JSON: `Key`
- XML: `Tag`
- Markdown: `Heading`
- YAML: `Key`

### 4. Git-style Diff View

Click **Diff** in the toolbar to open a side-by-side diff panel comparing the
original loaded file against the current edited text. The diff uses a
Longest-Common-Subsequence algorithm and renders added lines (green `+`),
removed lines (red `-`), and unchanged lines with line numbers. A summary
(`+N -M`) is shown in the panel header.

### 5. REST-based Code Completion

The editor calls a configurable REST endpoint to fetch intelligent code
completions. Set the endpoint URL in the toolbar's **Endpoint** field and
click **Test** to verify connectivity.

**Request payload** (POST, JSON body):

```json
{
  "language": "r",
  "text": "<full document text>",
  "line": 5,
  "column": 12,
  "trigger": "."
}
```

**Expected response** (JSON):

```json
{
  "items": [
    {
      "label": "mean",
      "kind": "function",
      "detail": "Arithmetic Mean",
      "insertText": "mean()"
    }
  ]
}
```

If the endpoint is unreachable or returns an error, a built-in fallback
suggestion list is used (VB.NET keywords or R built-in functions).

Completions are triggered by typing `.` or `:` or by pressing
`Ctrl+Space`.

### 6. Go to Line

Press `Ctrl+G` or click **Go to Line** in the toolbar to open a dialog where
you can type a line number and jump directly to it.

### 7. File Load / Export

- **Open**: Click **Open** to load a file from disk. The language is
  auto-detected from the file extension.
- **Export**: Click **Export** to download the current editor content as a
  file. The original filename is preserved (or `untitled.<ext>` if none).

### 8. Themes

Two themes matching Visual Studio:

- **Light** — matches the Visual Studio light theme syntax colors.
- **Dark** — matches the Visual Studio dark theme syntax colors.

Themes are managed in separate CSS files (`css/theme-light.css` and
`css/theme-dark.css`) and switched via `body[data-theme="..."]`.

## Keyboard Shortcuts

| Shortcut         | Action              |
|------------------|---------------------|
| `Ctrl+O`         | Open file           |
| `Ctrl+S`         | Export file         |
| `Ctrl+G`         | Go to line          |
| `Ctrl+Space`     | Trigger completion  |
| `Ctrl+D`         | Toggle diff view    |
| `Ctrl+B`         | Toggle symbols panel|

## Project Structure

```
code-editor/
├── index.html                  # Main HTML page
├── tsconfig.json               # TypeScript compiler config
├── css/
│   ├── layout.css              # Base layout & component styles
│   ├── theme-light.css         # Light theme (Visual Studio light)
│   └── theme-dark.css          # Dark theme (Visual Studio dark)
├── src/
│   ├── main.ts                 # Application entry point
│   ├── core/
│   │   ├── Editor.ts           # Main editor controller
│   │   ├── TextBuffer.ts       # Line-based text storage
│   │   ├── Cursor.ts           # Selection & caret management
│   │   └── Highlighter.ts      # Per-line tokenization cache
│   ├── highlighters/
│   │   ├── VbNetHighlighter.ts
│   │   ├── RHighlighter.ts
│   │   ├── JsonHighlighter.ts
│   │   ├── XmlHighlighter.ts
│   │   ├── MarkdownHighlighter.ts
│   │   ├── YamlHighlighter.ts
│   │   └── HighlighterRegistry.ts
│   ├── features/
│   │   ├── CodeFolder.ts       # Code folding ranges
│   │   ├── SymbolNavigator.ts  # Symbol extraction
│   │   ├── DiffViewer.ts       # Git-style diff
│   │   ├── CompletionProvider.ts # REST completion client
│   │   └── GoToLine.ts         # Go-to-line validation
│   └── utils/
│       ├── Tokenizer.ts        # Token types & helpers
│       ├── EventEmitter.ts     # Tiny event emitter
│       └── Diff.ts             # LCS diff algorithm
└── dist/
    └── editor.bundle.js        # Compiled single-file bundle
```

## Building

Requires TypeScript 5.x (the `outFile` option is used to produce a single
bundle, which was removed in TypeScript 7+).

```bash
# Install TypeScript 5.x globally (one-time)
npm install -g typescript@5.6.3

# Compile
cd code-editor
tsc
```

The output is a single file: `dist/editor.bundle.js`.

## Running

Open `index.html` directly in a browser, or serve the folder with any static
HTTP server:

```bash
python3 -m http.server 8000
# then open http://localhost:8000/
```

No build step is required to run — the precompiled `dist/editor.bundle.js`
is included.

## REST Endpoint Contract

See section 5 above. A minimal mock endpoint (Node.js, no dependencies):

```js
const http = require('http');
http.createServer((req, res) => {
  let body = '';
  req.on('data', c => body += c);
  req.on('end', () => {
    const { language } = JSON.parse(body || '{}');
    const items = language === 'r'
      ? [{ label: 'mean', kind: 'function', insertText: 'mean()' },
         { label: 'sd',   kind: 'function', insertText: 'sd()' }]
      : [{ label: 'If',     kind: 'keyword' },
         { label: 'While',  kind: 'keyword' }];
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ items }));
  });
}).listen(9000);
```

Point the editor's **Endpoint** field at `http://localhost:9000/completion`.
