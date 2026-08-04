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

// _verify_multiline.ts
var fs = __toESM(require("fs"));
var path = __toESM(require("path"));
var bundle = fs.readFileSync(path.join(__dirname, "dist", "editor.bundle.js"), "utf8");
var fn = new Function("window", "self", "globalThis", bundle + "\nreturn (typeof CodeEditor!=='undefined')?CodeEditor:undefined;");
var CodeEditor = fn(global, global, global);
function run(name, source, lang) {
  const HL = CodeEditor.Highlighters;
  const h = lang === "R" ? HL.RHighlighter : HL.VbNetHighlighter;
  const state = h.initialState();
  const lines = source.split("\n");
  console.log(`
=== ${name} (${lang}) ===`);
  let ok = true;
  lines.forEach((line, idx) => {
    const r = h.tokenizeLine(line, state);
    const types = r.tokens.map((t) => t.type);
    const nonString = types.filter((t) => t !== "String");
    console.log(`L${idx + 1}: ${JSON.stringify(line)} -> ${JSON.stringify(types)}`);
    if (r.state.inString) {
      if (nonString.length > 0) {
        ok = false;
        console.log(`  !! BUG: continuation line has non-String tokens: ${JSON.stringify(nonString)}`);
      }
    }
    Object.assign(state, r.state);
  });
  console.log(ok ? "RESULT: PASS" : "RESULT: FAIL");
  return ok;
}
var pass = true;
pass = run("R double-quote multiline", [
  'x <- "line one',
  "line two",
  'line three"',
  "y <- 1 + 2  # code after string"
].join("\n"), "R") && pass;
pass = run("R single-quote multiline", [
  "a <- 'foo",
  "bar",
  "baz'"
].join("\n"), "R") && pass;
pass = run("R raw string multiline", [
  's <- r"(hello',
  "world",
  ')"',
  "z <- 1"
].join("\n"), "R") && pass;
pass = run("R escaped quote", [
  'q <- "she said \\"hi',
  'there\\" end"',
  "w <- 9"
].join("\n"), "R") && pass;
pass = run("VB multiline string", [
  'Dim s As String = "line one',
  "line two",
  'line three"',
  "Dim y As Integer = 1 + 2"
].join("\n"), "VB") && pass;
pass = run("VB interpolated multiline", [
  'Dim s = $"value {x}',
  "more {y}",
  '"',
  "Dim z = 1"
].join("\n"), "VB") && pass;
pass = run("VB char literal", [
  'Dim c As Char = "a"c',
  "Dim d As Integer = 5"
].join("\n"), "VB") && pass;
pass = run("VB doubled quote escape", [
  'Dim s = "he said ""hi',
  'there"" end"',
  "Dim w = 9"
].join("\n"), "VB") && pass;
console.log(`
==== OVERALL: ${pass ? "ALL PASS" : "SOME FAIL"} ====`);
