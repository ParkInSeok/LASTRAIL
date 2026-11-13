param(
    [string]$UnityExe = "D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$TestResults = "",
    [string]$LogFile = "",
    [string]$TestFilter = "",
    [switch]$WithGraphics
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot 'Set-UnityDEnvironment.ps1')
$ProjectPath = [IO.Path]::GetFullPath($ProjectPath)
if ($ProjectPath.TrimEnd('\', '/') -ne $unityDProject.TrimEnd('\', '/')) {
    throw 'Use the active D Unity project.'
}
$runDirectory = Join-Path $ProjectPath ('QA/Unity1Artifacts/Runs/EditMode-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
if ([string]::IsNullOrWhiteSpace($TestResults)) { $TestResults = Join-Path $runDirectory 'editmode.xml' }
if ([string]::IsNullOrWhiteSpace($LogFile)) { $LogFile = Join-Path $runDirectory 'editor.log' }
foreach ($outputPath in @($TestResults, $LogFile)) {
    $resolvedOutput = [IO.Path]::GetFullPath($outputPath)
    if (-not $resolvedOutput.StartsWith('D:\Project\LASTRAIL\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Test outputs must stay under the D LASTRAIL project.'
    }
    if (Test-Path -LiteralPath $resolvedOutput) { throw 'Fresh test output paths are required.' }
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutput))
}
$TestResults = [IO.Path]::GetFullPath($TestResults)
$LogFile = [IO.Path]::GetFullPath($LogFile)

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity executable not found: $UnityExe"
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Unity project path not found: $ProjectPath"
}

$unityDGiCache = Join-Path $unityDProject 'Caches/GI'
[void][IO.Directory]::CreateDirectory($unityDGiCache)

$arguments = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $TestResults,
    "-logFile", $LogFile,
    "-giCustomCacheLocation", $unityDGiCache
)
if (-not $WithGraphics) { $arguments += "-nographics" }

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $arguments += @("-testFilter", $TestFilter)
}

# Unity Test Framework owns process exit after -runTests. Adding -quit can exit
# before CommandLineTest starts and leave no XML result file in Unity 6000.3.22f1.
$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -WorkingDirectory $ProjectPath -WindowStyle Hidden -PassThru
$process.WaitForExit()
$unityExitCode = $process.ExitCode

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
    [Environment]::Exit($unityExitCode)
}

if ($failed -ne 0) {
    Write-Error "Unity EditMode tests failed: total=$total passed=$passed failed=$failed xml=$TestResults"
    [Environment]::Exit(1)
}

[Environment]::Exit(0)
