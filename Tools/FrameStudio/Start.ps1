$ErrorActionPreference = 'Stop'
$env:TEMP = 'D:/Project/LASTRAIL/LocalTemp'
$env:TMP = $env:TEMP
$env:TMPDIR = $env:TEMP
$env:PYTHONIOENCODING = 'utf-8'
$studioPython = 'C:/Users/dlstj/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
Set-Location 'D:/Project/LASTRAIL'
& $studioPython "$PSScriptRoot/server.py"
