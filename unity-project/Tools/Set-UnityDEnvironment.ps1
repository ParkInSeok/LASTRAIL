# Dot-source before Unity resource, capture, build, or QA tools.
# Process-local settings only; no global profile or installed-tool changes.
$unityDProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($unityDProject.TrimEnd('\', '/') -ne 'D:\Project\LASTRAIL\unity-project') {
    throw 'Unity tools must run from the active D Unity project.'
}
$unityDTemp = 'D:\Project\LASTRAIL\LocalTemp\Unity'
$unityDCache = Join-Path $unityDTemp 'Cache'
$unityDPaths = @{
    TEMP = $unityDTemp
    TMP = $unityDTemp
    TMPDIR = $unityDTemp
    XDG_CACHE_HOME = $unityDCache
    PIP_CACHE_DIR = (Join-Path $unityDCache 'pip')
    UV_CACHE_DIR = (Join-Path $unityDCache 'uv')
    IMAGEIO_USERDIR = (Join-Path $unityDCache 'imageio')
    MPLCONFIGDIR = (Join-Path $unityDCache 'matplotlib')
    GRADLE_USER_HOME = (Join-Path $unityDCache 'gradle')
    DOTNET_CLI_HOME = (Join-Path $unityDCache 'dotnet')
    NUGET_PACKAGES = (Join-Path $unityDCache 'nuget')
    npm_config_cache = (Join-Path $unityDCache 'npm')
    UPM_CACHE_ROOT = (Join-Path $unityDCache 'upm')
}
foreach ($unityDEntry in $unityDPaths.GetEnumerator()) {
    [void][IO.Directory]::CreateDirectory($unityDEntry.Value)
    [Environment]::SetEnvironmentVariable($unityDEntry.Key, $unityDEntry.Value, 'Process')
}
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
# Hub/Codex processes may still carry PATH from before the user's tool setup.
$unityPythonTools = 'D:\CodexData\Tools\bin'
if (Test-Path (Join-Path $unityPythonTools 'uv.exe')) {
    $env:PATH = $unityPythonTools + ';' + $env:PATH
    $env:UV_PYTHON_INSTALL_DIR = 'D:\CodexData\Tools\python'
    $env:UV_PYTHON_BIN_DIR = $unityPythonTools
}
