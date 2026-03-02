# Install dependencies for admin-ui (Angular)
# Chay tu thu muc DemoCICD: .\install-admin-ui.ps1
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location (Join-Path $scriptDir "admin-ui")
try {
    npm i
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
