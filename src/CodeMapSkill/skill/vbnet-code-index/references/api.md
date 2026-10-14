# CodeMapSkill API reference

The complete argument and response reference of the `vbnet-code-index` skill.
Read this file with `read_skill_file` when the short forms in `SKILL.md` are not
enough. `{{BASE}}` is `http://localhost:<port>`, by default `http://localhost:8080`.

## Service startup

```
CodeMapSkill --workspace <repo> [--port 8080] [--q 3] [--top 20] [--threshold 0.15]
             [--max-lines 200] [--max-chars 1200] [--max-workspaces 4] [--silent]
```

- `--workspace` / `-w` / `--open` may be repeated (or comma separated) to open
  several repositories at startup; the first one becomes the default workspace.
- `--max-workspaces` bounds the in-memory index pool (LRU eviction, `0` = no limit).
- `--max-chars` is the number of source characters returned per symbol hit.

## Tools (REST and MCP)

The eight tools are identical over both protocols. Over REST the tool is a route,
over MCP it is a `tools/call` name; `workspaceId` is spelled `ws` in a query string.

| tool | REST route | purpose |
|---|---|---|
| `index_status` | `GET /api/status` | index build progress |
| `list_workspaces` | `GET /api/workspaces` | the open workspaces |
| `open_workspace` | `GET|POST /api/open` | open a repository |
| `search_symbols` | `GET|POST /api/query` | fuzzy keyword search |
| `find_symbol` | `GET /api/symbol` | resolve one symbol |
| `list_members` | `GET /api/members` | members of a type |
| `list_files` | `GET /api/files` | source file list |
| `get_source` | `GET|POST /api/source` | raw source text |

`GET /api/workspace` has no tool of its own: it returns the workspace summary
(`kind`, `name`, `rootPath`, `projectCount`, `fileCount`, `projects[]`, `errors[]`).

### index_status / `GET /api/status?ws=`

```json
{
  "workspaceId": "ws1", "path": "G:\\DevAgent\\src\\DevAgent.slnx",
  "ready": true, "phase": "ready", "progress": 1,
  "processedFiles": 128, "totalFiles": 128,
  "symbols": 4213, "documents": 3901, "errors": 0, "elapsedMs": 1840,
  "message": "4213 symbols indexed from 128 files",
  "workspace": { "kind": "Solution", "name": "DevAgent", "rootPath": "...",
                 "projectCount": 6, "fileCount": 128, "projects": ["..."], "errors": [] }
}
```

`phase` is `idle | building | ready | failed`.

### open_workspace / `GET /api/open?path=`

`path` accepts a `.sln`, a `.slnx`, a `.vbproj` or a source folder. The answer is
returned immediately; the index is built on a background thread.

```json
{ "workspaceId": "ws1", "path": "...", "name": "DevAgent", "isNew": true,
  "status": { "...": "the index_status payload" } }
```

Opening the same path twice returns the existing handle with `isNew: false`.

### search_symbols / `GET /api/query?q=&top=&threshold=&ws=`

| argument | default | meaning |
|---|---|---|
| `q` / `query` / `keyword` | required | the keyword |
| `top` | 20 | max number of hits |
| `threshold` | 0.15 | minimum q-gram similarity of a matched word |

The engine tokenizes the query, drops words shorter than three characters and keeps
the eight most selective ones. The raw similarity is scaled by how well the symbol
name itself matches (exact name 16x, exact full name 12x, name contains 8x,
full name contains 4x), so a query that is an identifier ranks its own declaration
first. `hits[]` is ordered by `score`.

### find_symbol / `GET /api/symbol?fullname=&ws=`

Matches the namespace qualified name exactly, then as a name suffix, then as a
simple name, and finally falls back to the parsed project models. Returns up to
50 matches.

### list_members / `GET /api/members?fullname=&ws=`

Returns `members[]` sorted by declaration line, each with `signature` (for example
`Public Function Query(keyword As String, top As Integer) As QueryHit()`),
`parameters[]`, `returnType`, `declarationLine`, `startLine`, `endLine` and `xmlDoc`,
plus `nestedTypes[]` for the nested type declarations.

### list_files / `GET /api/files?project=&pattern=&ws=`

`project` filters by project name (exact, then substring). `pattern` is a VB `Like`
wildcard on the workspace relative path (`*Index*.vb`), and is also matched as a
plain substring, so a bare fragment works too.

### get_source / `GET /api/source?file=&start=&end=&maxLines=&ws=`

`file` is a workspace relative path or an absolute path **inside the workspace root**
(a path that escapes the root is rejected). `symbol=<full name>` may be used instead
of `file` to read the declaration block of a symbol. `start` / `startLine` and
`end` / `endLine` are 1-based; `maxLines` defaults to 200 and is capped at 5000.
The answer reports `truncated: true` when the range was clipped.

## MCP endpoint

`POST {{BASE}}/mcp`, `Content-Type: application/json`, one JSON-RPC 2.0 message per
request. Stateless: no `Mcp-Session-Id` is issued or required.

```json
{"jsonrpc":"2.0","id":1,"method":"tools/call",
 "params":{"name":"search_symbols","arguments":{"query":"QGram","top":5,"workspaceId":"ws1"}}}
```

- `initialize` → `protocolVersion` `2025-06-18`, `capabilities.tools`, `serverInfo`
  and an `instructions` string.
- `tools/list` → `{"tools":[{ "name","description","inputSchema" }]}`.
- `tools/call` → `{"content":[{"type":"text","text":"<json>"}],"isError":false,
  "structuredContent":{...}}`.
- `ping` → `{}`.
- `notifications/*` → empty body, HTTP 200.
- `GET /mcp` (browser friendly) → a short description of the endpoint.

### Error codes

| code | meaning |
|---|---|
| `-32700` | the body is not valid JSON |
| `-32600` | `method` is missing |
| `-32601` | unknown method |
| `-32602` | `tools/call` without a tool name |
| `-32603` | the tool failed |
| `-32002` | the index of the workspace is still building (`data.status` carries the `index_status` payload) |

## Skill endpoints

| path | content |
|---|---|
| `GET /skill/SKILL.md` | the skill definition (with the real base address substituted) |
| `GET /skill/manifest.yaml` | the skill manifest |
| `GET /skill/scripts/codemap.ps1` | the REST helper |
| `GET /skill/scripts/mcp_call.ps1` | the MCP helper |
| `GET /skill/references/api.md` | this reference |
| `GET /tools.json` | the machine readable tool catalogue |
| `GET /` | a landing page with the endpoint table |

## Troubleshooting

| symptom | cause and fix |
|---|---|
| connection refused | the service is not running; start `CodeMapSkill --workspace <repo>` |
| `503` / `-32002` | the index is still building; poll `index_status` |
| `404` on a symbol | the full name is wrong; use `search_symbols` to find the exact name |
| `0` files in the workspace | the path is not a vb.net repository, or every project failed to parse (see `workspace.errors`) |
| a hit points at stale code | re-open the workspace; the index is a snapshot taken when it was opened |
