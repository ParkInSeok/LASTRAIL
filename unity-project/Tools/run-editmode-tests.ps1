param(
    [string]$UnityExe = "D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$TestResults = (Join-Path $ProjectPath "TestResults-EditMode.xml"),
    [string]$LogFile = (Join-Path $ProjectPath "Unity-EditMode.log"),
    [string]$TestFilter = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity executable not found: $UnityExe"
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Unity project path not found: $ProjectPath"
}

$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $TestResults,
    "-logFile", $LogFile
)

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $arguments += @("-testFilter", $TestFilter)
}

# Unity Test Framework owns process exit after -runTests. Adding -quit can exit
# before CommandLineTest starts and leave no XML result file in Unity 6000.3.22f1.
& $UnityExe @arguments
$unityExitCode = $LASTEXITCODE

$deadline = (Get-Date).AddSeconds(30)
while (-not (Test-Path -LiteralPath $TestResults)) {
    if ((Get-Date) -gt $deadline) {
        throw "Unity Test Runner did not create XML results at $TestResults. Check that the command did not include -quit."
    }

    Start-Sleep -Milliseconds 500
}

$xml = $null
$deadline = (Get-Date).AddSeconds(10)
while ($null -eq $xml) {
    try {
        [xml]$xml = Get-Content -Raw -LiteralPath $TestResults
    } catch {
        if ((Get-Date) -gt $deadline) {
            throw
        }

        Start-Sleep -Milliseconds 250
    }
}

$testRun = $xml.SelectSingleNode("/test-run")
if ($null -eq $testRun) {
    throw "Unity Test Runner XML is missing the /test-run node: $TestResults"
}

$failed = [int]$testRun.failed
$total = [int]$testRun.total
$passed = [int]$testRun.passed

Write-Host "Unity EditMode tests: total=$total passed=$passed failed=$failed xml=$TestResults"

if ($unityExitCode -ne 0) {
    exit $unityExitCode
}

if ($failed -ne 0) {
    exit 1
}

exit 0
