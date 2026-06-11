<#
  Smoke-drives the Real Estate API end-to-end: register -> login -> chat.
  This is the agent harness for the run-real-estate skill: it proves the
  backend (and the AI chat in particular) actually works, not just that
  the process started.

  Usage (from repo root, with the API already listening on :7144):
    powershell -ExecutionPolicy Bypass -File .claude/skills/run-real-estate/smoke.ps1

  Exit 0 = chat returned property suggestions (AI + cross-language city
  match working). Non-zero = something broke.

  NOTE: no Hebrew literals live in this file on purpose - Windows
  PowerShell 5.1 reads BOM-less .ps1 files in the local codepage and
  would corrupt them. The Hebrew chat message lives in chat-body.json
  (UTF-8), POSTed verbatim with `curl --data-binary`.
#>
param(
    [string]$BaseUrl  = "https://localhost:7144",
    [string]$Email    = "smoke@example.com",
    [string]$Password = "Passw0rd!"
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Curl-Json($method, $path, $bodyFile, $token) {
    $a = @("-k", "-s", "-X", $method, "$BaseUrl$path",
           "-H", "Content-Type: application/json; charset=utf-8")
    if ($token)    { $a += @("-H", "Authorization: Bearer $token") }
    if ($bodyFile) { $a += @("--data-binary", "@$bodyFile") }
    return (& curl.exe @a)
}

Write-Host "1. Register (ignored if the user already exists)..."
$regFile = Join-Path $env:TEMP "re_smoke_reg.json"
'{"fullName":"Smoke Test","email":"' + $Email + '","password":"' + $Password + '","phoneNumber":"0500000000","maxBudget":3000000,"role":"Buyer"}' |
    Set-Content -LiteralPath $regFile -Encoding ascii
Curl-Json "POST" "/api/auth/register" $regFile $null | Out-Null

Write-Host "2. Login -> JWT..."
$loginFile = Join-Path $env:TEMP "re_smoke_login.json"
'{"email":"' + $Email + '","password":"' + $Password + '"}' |
    Set-Content -LiteralPath $loginFile -Encoding ascii
$loginResp = Curl-Json "POST" "/api/auth/login" $loginFile $null
$token = ($loginResp | ConvertFrom-Json).token
if (-not $token) { Write-Error "Login failed: $loginResp"; exit 1 }
Write-Host "   token length: $($token.Length)"

Write-Host "3. Chat: Hebrew 'Tel Aviv' search (AI parse + cross-language city match)..."
$chatResp = Curl-Json "POST" "/api/chat" (Join-Path $here "chat-body.json") $token
$chat = $chatResp | ConvertFrom-Json
$count = @($chat.suggestedProperties).Count
Write-Host "   reply length: $($chat.reply.Length) ; suggestions: $count"

if (-not $chat.reply) { Write-Error "Chat returned no reply: $chatResp"; exit 1 }
if ($count -lt 1) {
    Write-Error "Expected >=1 suggestion for Tel Aviv. Got 0 - AI fallback (402/DI) or city-match regression."
    exit 1
}
Write-Host "`nSMOKE OK: chat is working end-to-end ($count suggestion(s))." -ForegroundColor Green
