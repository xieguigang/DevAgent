// Runner: loads the IIFE bundle (which populates a global CodeEditor from a sandbox
// acting as window) and simulates Highlighter.ts's cross-line state passing.
import * as fs from "fs";
import * as path from "path";

const bundle = fs.readFileSync(path.join(__dirname, "dist", "verify.bundle.js"), "utf8");
const sandbox: any = {};
const fn = new Function("window", "self", "globalThis", bundle);
fn(sandbox, sandbox, sandbox);
const CodeEditor = sandbox.CodeEditor;
if (!CodeEditor) throw new Error("CodeEditor global not found on sandbox");

const HL = CodeEditor.Highlighters;
const R = HL.RHighlighter;
const VB = HL.VbNetHighlighter;

function run(name: string, source: string, h: any): boolean {
    let pass = true;
    let state = h.initialState();
    const lines = source.split("\n");
    console.log(`\n=== ${name} ===`);
    lines.forEach((line: string, idx: number) => {
        const r = h.tokenizeLine(line, state);
        const types = r.tokens.map((t: any) => t.type);
        console.log(`L${idx + 1}: ${JSON.stringify(line)} -> ${JSON.stringify(types)}`);
        if (r.state.inString) {
            const nonString = types.filter((t: string) => t !== "String");
            if (nonString.length > 0) {
                pass = false;
                console.log(`  !! BUG continuation has non-String: ${JSON.stringify(nonString)}`);
            }
        }
        state = r.state;
    });
    console.log(pass ? "RESULT: PASS" : "RESULT: FAIL");
    return pass;
}

let all = true;

all = run("R double-quote multiline", [
    'x <- "line one',
    'line two',
    'line three"',
    "y <- 1 + 2  # code after string"
].join("\n"), R) && all;

all = run("R single-quote multiline", [
    "a <- 'foo",
    "bar",
    "baz'"
].join("\n"), R) && all;

all = run("R raw string multiline", [
    's <- r"(hello',
    'world',
    ')"',
    'z <- 1'
].join("\n"), R) && all;

all = run("R escaped quote should NOT close", [
    'q <- "she said \\"hi',
    'there\\" end"',
    'w <- 9'
].join("\n"), R) && all;

all = run("VB multiline string", [
    'Dim s As String = "line one',
    'line two',
    'line three"',
    "Dim y As Integer = 1 + 2"
].join("\n"), VB) && all;

all = run("VB interpolated multiline", [
    'Dim s = $"value {x}',
    'more {y}',
    '"',
    'Dim z = 1'
].join("\n"), VB) && all;

all = run("VB char literal single-line", [
    'Dim c As Char = "a"c',
    'Dim d As Integer = 5'
].join("\n"), VB) && all;

all = run("VB doubled quote escape should NOT close", [
    'Dim s = "he said ""hi',
    'there"" end"',
    'Dim w = 9'
].join("\n"), VB) && all;

console.log(`\n==== OVERALL: ${all ? "ALL PASS" : "SOME FAIL"} ====`);
if (!all) process.exit(1);
