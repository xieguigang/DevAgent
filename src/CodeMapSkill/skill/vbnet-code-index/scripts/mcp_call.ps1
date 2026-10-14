<#
.SYNOPSIS
    Call the MCP json-rpc endpoint of the CodeMapSkill vb.net code index service.

.DESCRIPTION
    Posts one json-rpc 2.0 message to POST /mcp. The transport is stateless: no
    session header is required, initialize is optional but recommended.

.PARAMETER Method
    initialize | tools/list | tools/call | ping

.PARAMETER Tool
    The tool name for tools/call, e.g. search_symbols

.PARAMETER Arguments
    The tool arguments, e.g. @{ query = 'CodeMapIndex'; top = 5; workspaceId = 'ws1' }

.PARAMETER Base
    The base address of the service, default http://localhost:8080

.EXAMPLE
    .\mcp_call.ps1 initialize

.EXAMPLE
    .\mcp_call.ps1 tools/list

.EXAMPLE
    .\mcp_call.ps1 tools/call -Tool open_workspace -Arguments @{ path = 'G:\DevAgent\src\DevAgent.slnx' }

.EXAMPLE
    .\mcp_call.ps1 tools/call -Tool search_symbols -Arguments @{ query = 'QGram'; top = 5 }
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("initialize", "tools/list", "tools/call", "ping")]
    [string]$Method,

    [string]$Tool,

    [hashtable]$Arguments = @{},

    [string]$Base = "http://localhost:8080",

    [int]$Id = 1
)

$params = @{}

if ($Method -eq "tools/call") {
    if (-not $Tool) { Write-Error "tools/call needs -Tool"; exit 1 }

    $params["name"] = $Tool
    $params["arguments"] = $Arguments
}

$body = @{
    jsonrpc = "2.0"
    id      = $Id
    method  = $Method
    params  = $params
} | ConvertTo-Json -Depth 12

$url = "$($Base.TrimEnd('/'))/mcp"

Write-Verbose "POST $url`n$body"

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $body
}
catch {
    Write-Error "the mcp call failed: $($_.Exception.Message)"
    exit 1
}

if ($response.error) {
    $code = $response.error.code
    $message = $response.error.message

    if ($code -eq -32002) {
        Write-Error "the index is not ready yet: $message"
    }
    else {
        Write-Error "json-rpc error $code : $message"
    }

    exit 1
}

$response | ConvertTo-Json -Depth 20
