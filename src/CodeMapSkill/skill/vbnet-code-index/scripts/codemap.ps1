<#
.SYNOPSIS
    Call the REST endpoints of the CodeMapSkill vb.net code index service.

.DESCRIPTION
    A thin wrapper around the /api endpoints so that a Windows shell does not have
    to hand-craft urls and json decoding. The service is read only: nothing here
    ever modifies the indexed repository.

.PARAMETER Action
    status | workspaces | open | workspace | query | symbol | members | files | source

.PARAMETER Params
    The query string parameters of the endpoint, e.g. @{ q = 'QGram'; top = 10; ws = 'ws1' }

.PARAMETER Base
    The base address of the service, default http://localhost:8080

.EXAMPLE
    .\codemap.ps1 status

.EXAMPLE
    .\codemap.ps1 open -Params @{ path = 'G:\DevAgent\src\DevAgent.slnx' }

.EXAMPLE
    .\codemap.ps1 query -Params @{ q = 'CodeMapIndex'; top = 5; ws = 'ws1' }

.EXAMPLE
    .\codemap.ps1 members -Params @{ fullname = 'CodeIndex.CodeMapIndex'; ws = 'ws1' }

.EXAMPLE
    .\codemap.ps1 source -Params @{ file = 'CodeIndex\CodeMapIndex.vb'; start = 1; end = 60; ws = 'ws1' }
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("status", "workspaces", "open", "workspace", "query", "symbol", "members", "files", "source")]
    [string]$Action,

    [hashtable]$Params = @{},

    [string]$Base = "http://localhost:8080",

    [switch]$Raw
)

$query = ($Params.GetEnumerator() |
    ForEach-Object { "{0}={1}" -f $_.Key, [System.Uri]::EscapeDataString([string]$_.Value) }) -join "&"

$url = "$($Base.TrimEnd('/'))/api/$Action"
if ($query) { $url = "$url?$query" }

Write-Verbose "GET $url"

try {
    $response = Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json"
}
catch {
    $status = $null
    if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode.value__ }
    $body = ""

    if ($_.ErrorDetails.Message) { $body = $_.ErrorDetails.Message }

    if ($status -eq 503) {
        Write-Error "the index is not ready yet ($body). poll '.\codemap.ps1 status' until ready is true."
    }
    elseif ($status -eq 404) {
        Write-Error "not found ($body). check the workspace id and the symbol full name."
    }
    else {
        Write-Error "the call failed (HTTP $status): $body"
    }

    exit 1
}

if ($Raw) {
    $response
}
else {
    $response | ConvertTo-Json -Depth 12
}
