---
name: vbnet-code-index
description: Index and navigate VB.NET source code through a local read-only code index service. This skill should be used when a task requires locating where a VB.NET type, method, property or field is declared, listing the members of a class, module, structure, interface or enum, reading a range of raw source text, enumerating the source files of a solution, or searching a .sln / .slnx / .vbproj repository by keyword instead of grepping it blindly.
version: 1.0.0
tags: [vbnet, code-search, code-navigation, symbol-index, codemap]
author: DevAgent
---

# VB.NET Code Index

A read-only index over a VB.NET repository, exposed by the `CodeMapSkill` HTTP service.
The index holds every parsed type and type member of the repository: its namespace
qualified full name, its kind, its modifiers, its XML documentation, the file that
declares it and the exact line range of its declaration block.

The service speaks two protocols over the same capability set:

- **REST** at `{{BASE}}/api/...`
- **MCP** as a single `POST {{BASE}}/mcp` JSON-RPC 2.0 endpoint
  (`initialize`, `tools/list`, `tools/call`, `ping`)

The full argument and response reference lives in `references/api.md`; read it with
`read_skill_file` before calling an endpoint whose arguments are unclear. The
PowerShell helpers `scripts/codemap.ps1` (REST) and `scripts/mcp_call.ps1` (MCP) wrap
the same calls for a Windows shell.

## Execution Flow

1. **Make sure the service is reachable.** Fetch `{{BASE}}/tools.json`. It returns the
   tool catalogue and the endpoint table. A connection refusal means the service is
   not running: start it with
   `CodeMapSkill --workspace <repo> --port 8080`
   (the workspace may be a `.sln`, a `.slnx`, a `.vbproj` or a plain source folder).
2. **Open the repository** when it is not open yet: `GET {{BASE}}/api/open?path=<repo>`,
   or the `open_workspace` tool. The call returns a `workspaceId` (for example `ws1`)
   immediately; the index is built in the background.
   Skip this step when the target repository was already given to the service at startup.
3. **Wait until the index is ready**: `GET {{BASE}}/api/status?ws=<id>`, or the
   `index_status` tool. Repeat while `ready` is `false` (a large solution takes a few
   seconds; `progress` reports the fraction of files already indexed). Every other
   capability fails while the index is still building.
4. **Look the symbol up.** Choose the cheapest capability that answers the question:
   - a partly remembered name, a concept or a piece of documentation text →
     `search_symbols` (`GET /api/query?q=<keyword>&top=20&ws=<id>`);
   - an exact or suffix namespace qualified name →
     `find_symbol` (`GET /api/symbol?fullname=CodeIndex.CodeMapIndex&ws=<id>`);
   - everything a type exposes →
     `list_members` (`GET /api/members?fullname=<type>&ws=<id>`);
   - where a file lives in the repository →
     `list_files` (`GET /api/files?pattern=*Index*.vb&ws=<id>`);
   - the actual source text → `get_source`
     (`GET /api/source?file=<relative path>&start=1&end=120&ws=<id>`, or
     `?symbol=<full name>` to read the declaration block of a symbol).
   All of them accept `ws`; omitting it targets the default workspace.
5. **Read the answer.** A symbol hit carries `fullName`, `kind`, `modifiers`,
   `relativeFile`, `startLine`, `endLine`, `declarationLine`, `xmlDoc` and a clipped
   `code` block. Use `get_source` with the reported file and line range when the clipped
   block is not enough.

Over MCP the flow is identical: `initialize`, then `tools/list` to see the eight tools
and their JSON schemas, then `tools/call` with `{"name": "...", "arguments": {...}}`.

## Input Specification

- `path` — a `.sln`, `.slnx`, `.vbproj` file or a source folder. Absolute paths are
  safest; a folder that contains a solution file is indexed through that solution.
- `workspaceId` — the handle returned by `open_workspace` (`ws1`, `ws2`, ...). Every
  capability accepts it; an empty value targets the default workspace. The REST
  endpoints spell it `ws`.
- `query` — free text for the fuzzy search: an identifier, several words, or a phrase
  from a comment. Words shorter than three characters are ignored by the index.
  `top` (default 20) caps the hits, `threshold` (default 0.15) is the minimum
  q-gram similarity of a matched word; lower it to widen the recall.
- `fullName` — a namespace qualified name. `find_symbol` matches it exactly, then as a
  name suffix, then as a simple name. `list_members` needs the type full name.
- `file` — a workspace relative path (`CodeIndex\CodeMapIndex.vb`) or an absolute path
  inside the workspace root. `startLine` / `endLine` are 1-based; `maxLines` defaults to
  200 and is capped at 5000.
- `symbol` — an alternative to `file` for `get_source`: the full name of a symbol whose
  declaration block should be returned.

## Output Specification

- `index_status` → `ready`, `phase` (`idle|building|ready|failed`), `progress`,
  `processedFiles` / `totalFiles`, `symbols`, `errors`, and the workspace summary.
- `search_symbols` → `hits[]` ordered by score: `score`, `fullName`, `kind`,
  `modifiers`, `relativeFile`, `startLine`, `endLine`, `declarationLine`, `xmlDoc`,
  clipped `code`, `truncated`.
- `find_symbol` → `symbols[]` with the same shape, without a score. An empty array
  means the symbol is not part of the indexed repository.
- `list_members` → `members[]` (`name`, `kind`, `modifiers`, `signature`,
  `parameters[]`, `returnType`, `declarationLine`, `startLine`, `endLine`, `xmlDoc`)
  plus `nestedTypes[]`; the type level fields repeat the declaring file and range.
- `list_files` → `files[]` with `project`, `relativePath`, `absolutePath`.
- `get_source` → `code`, `startLine`, `endLine`, `totalLines`, `truncated`, `project`,
  `relativeFile`.
- `open_workspace` → `workspaceId`, `path`, `isNew`, `status`.
- `list_workspaces` → `workspaces[]` with the handle, the path and the index state,
  plus `defaultId`.

Over REST a failure is an HTTP error code whose body repeats the message
(`400` bad arguments, `404` unknown workspace or symbol, `503` index still building).
Over MCP a failure is a JSON-RPC `error` object; the code `-32002` means the index is
still building, so retry after polling `index_status`.

## Constraints

- **VB.NET only.** The index parses `*.vb` sources; C#, F# and other languages in a
  mixed solution are skipped silently. Do not use this skill for non VB.NET code.
- **Read-only.** Nothing is written back: the service never edits, moves or deletes a
  file, and the index is discarded when the process exits.
- **The index must be ready.** Every capability except `open_workspace`,
  `list_workspaces`, `index_status` and `list_files` fails while the index is building.
- **Poll, do not assume.** `open_workspace` returning a handle does not mean the index
  is usable; `index_status` is the only authoritative answer.
- **Verify before editing.** The index is a snapshot of the parse result: a hit reports
  where a symbol *was declared* when the workspace was opened. Re-open the workspace
  after large refactorings, and always confirm a line range with `get_source` before
  proposing an edit.
- **Bounded payloads.** Symbol hits carry at most ~1200 characters of source and
  `get_source` at most `maxLines` lines; raise `maxLines`, or page through the file with
  `startLine` / `endLine`, when more context is needed.
- **Keep the workspace handle.** Pass `ws` on every call when several repositories are
  open; the pool keeps only the most recently used ones and silently evicts the rest.
- **Local service.** It listens on `localhost`; if the port does not answer, start the
  service rather than falling back to a blind grep of the repository.
