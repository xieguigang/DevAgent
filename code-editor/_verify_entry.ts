// Pure import entry: pulls Tokenizer + R + VB highlighters into the global CodeEditor
// namespace. Compiled to IIFE so cross-file `Utils`/`CodeEditor` ambient references resolve.
import "./src/utils/Tokenizer";
import "./src/highlighters/RHighlighter";
import "./src/highlighters/VbNetHighlighter";
