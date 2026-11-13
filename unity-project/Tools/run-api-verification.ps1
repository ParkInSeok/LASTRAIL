param(
    [Parameter(Mandatory=$true)][string]$ConnectionFile,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ServerRevision,
    [long]$AppVersionCode = 123,
    [ValidateSet('ACCOUNT_READ_LOCAL','SUPPORT_LOCAL_DEV')][string]$Profile = 'ACCOUNT_READ_LOCAL',
    [ValidateSet('LOCAL_SQLJS','DEV_ISOLATED')][string]$EnvironmentName = 'LOCAL_SQLJS'
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Set-UnityDEnvironment.ps1')
$source=[IO.Path]::GetFullPath($ConnectionFile)
if(-not $source.StartsWith('D:\Project\LASTRAIL\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Use a private connection file under the active D project.' }
$directory=Join-Path $unityDProject ('QA/ApiVerification/Runs/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($directory)
$private=Join-Path $directory 'configuration.private.json'
$oldConfig=$env:LASTRAIL_API_VERIFICATION_CONFIG
$oldReport=$env:LASTRAIL_API_VERIFICATION_REPORT
try {
    try { $config=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json } catch { throw 'Invalid private connection document.' }
    foreach($entry in @{profile=$Profile;environment=$EnvironmentName;serverRevision=$ServerRevision;appVersionCode=$AppVersionCode}.GetEnumerator()) {
        $config | Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value -Force
    }
    [IO.File]::WriteAllText($private,'',[Text.UTF8Encoding]::new($false))
    if($env:OS -eq 'Windows_NT') {
        $principal=$env:USERDOMAIN+'\'+$env:USERNAME
        & icacls $private /inheritance:r /grant:r ($principal+':F') | Out-Null
        if($LASTEXITCODE -ne 0) { throw 'Could not restrict private verification configuration.' }
    }
    $config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $private -Encoding utf8
    $env:LASTRAIL_API_VERIFICATION_CONFIG=$private
    $env:LASTRAIL_API_VERIFICATION_REPORT=Join-Path $directory 'actual-http.json'
    # Child process: the existing test runner exits its host. Keep this wrapper alive for private-file cleanup.
    & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $PSScriptRoot 'run-editmode-tests.ps1') -TestFilter 'ApiVerificationConfigurationTests;ActualApiVerificationTests;GameApiAuthenticatedTransportTests;InstalledAdmissionTransportTests;AccountStateReadTests;AuthenticatedSubmissionTests;SupportReceiptReconciliationTests;SupportReceiptControllerTests' -TestResults (Join-Path $directory 'editmode.xml') -LogFile (Join-Path $directory 'editor.log')
    $code=$LASTEXITCODE
    if($code -ne 0) { throw 'API verification tests failed; inspect sanitized status and Unity test results.' }
    if(-not (Test-Path -LiteralPath $env:LASTRAIL_API_VERIFICATION_REPORT)) { throw 'Actual HTTP report was not produced.' }
    Write-Host ('API verification evidence: '+$directory)
} finally {
    $env:LASTRAIL_API_VERIFICATION_CONFIG=$oldConfig
    $env:LASTRAIL_API_VERIFICATION_REPORT=$oldReport
    if(Test-Path -LiteralPath $private) { Remove-Item -LiteralPath $private }
}
