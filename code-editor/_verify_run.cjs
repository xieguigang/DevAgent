"use strict";
var __create = Object.create;
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __getProtoOf = Object.getPrototypeOf;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
  // If the importer is in node compatibility mode or this is not an ESM
  // file that has been converted to a CommonJS file using a Babel-
  // compatible transform (i.e. "__esModule" has not been set), then set
  // "default" to the CommonJS "module.exports" for node compatibility.
  isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
  mod
));

// _verify_run.ts
var fs = __toESM(require("fs"));
var path = __toESM(require("path"));
var bundle = fs.readFileSync(path.join(__dirname, "dist", "verify.bundle.js"), "utf8");
var sandbox = {};
var fn = new Function("window", "self", "globalThis", bundle);
fn(sandbox, sandbox, sandbox);
var CodeEditor = sandbox.CodeEditor;
if (!CodeEditor) throw new Error("CodeEditor global not found on sandbox");
var HL = CodeEditor.Highlighters;
var R = HL.RHighlighter;
var VB = HL.VbNetHighlighter;
function run(name, source, h) {
  let pass = true;
  let state = h.initialState();
  const lines = source.split("\n");
  console.log(`
=== ${name} ===`);
  lines.forEach((line, idx) => {
    const r = h.tokenizeLine(line, state);
    const types = r.tokens.map((t) => t.type);
    console.log(`L${idx + 1}: ${JSON.stringify(line)} -> ${JSON.stringify(types)}`);
    if (r.state.inString) {
      const nonString = types.filter((t) => t !== "String");
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
var all = true;
all = run("R double-quote multiline", [
  'x <- "line one',
  "line two",
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
  "world",
  ')"',
  "z <- 1"
].join("\n"), R) && all;
all = run("R escaped quote should NOT close", [
  'q <- "she said \\"hi',
  'there\\" end"',
  "w <- 9"
].join("\n"), R) && all;
all = run("VB multiline string", [
  'Dim s As String = "line one',
  "line two",
  'line three"',
  "Dim y As Integer = 1 + 2"
].join("\n"), VB) && all;
all = run("VB interpolated multiline", [
  'Dim s = $"value {x}',
  "more {y}",
  '"',
  "Dim z = 1"
].join("\n"), VB) && all;
all = run("VB char literal single-line", [
  'Dim c As Char = "a"c',
  "Dim d As Integer = 5"
].join("\n"), VB) && all;
all = run("VB doubled quote escape should NOT close", [
  'Dim s = "he said ""hi',
  'there"" end"',
  "Dim w = 9"
].join("\n"), VB) && all;
console.log(`
==== OVERALL: ${all ? "ALL PASS" : "SOME FAIL"} ====`);
if (!all) process.exit(1);
