$ErrorActionPreference='Stop'
Set-Location 'D:/Project/LASTRAIL'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp'; $env:TMP=$env:TEMP; $env:TMPDIR=$env:TEMP
$env:HF_HOME='D:/Project/LASTRAIL/Tools/FrameStudio/runtime/cache/huggingface'
$guard = Start-Process -FilePath "$PSScriptRoot/runtime/venv/Scripts/python.exe" -ArgumentList @('-B',"$PSScriptRoot/idle_gpu.py",$PID) -WindowStyle Hidden -PassThru
try {
 & "$PSScriptRoot/runtime/venv/Scripts/python.exe" "$PSScriptRoot/runtime/ComfyUI/main.py" --listen 127.0.0.1 --port 8188 --lowvram --disable-auto-launch
} finally {
 if (-not $guard.HasExited) { $guard.Kill(); $guard.WaitForExit() }
 $guard.Dispose()
}
