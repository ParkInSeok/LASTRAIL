[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [string[]]$Arguments = @()
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Set-UnityDEnvironment.ps1')
Push-Location -LiteralPath $unityDProject
try {
    if ([IO.Path]::GetFileName($Executable) -ieq 'Unity.exe' -and $Arguments -notcontains '-giCustomCacheLocation') { $Arguments += @('-giCustomCacheLocation', (Join-Path $unityDCache 'gi')) }
    & $Executable @Arguments
    if ($null -ne $LASTEXITCODE) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
