$ErrorActionPreference='Stop'
Set-Location 'D:/Project/LASTRAIL'
$env:TEMP='D:/Project/LASTRAIL/LocalTemp'; $env:TMP=$env:TEMP; $env:TMPDIR=$env:TEMP
$env:PIP_CACHE_DIR='D:/Project/LASTRAIL/Tools/FrameStudio/runtime/cache/pip'
$env:HF_HOME='D:/Project/LASTRAIL/Tools/FrameStudio/runtime/cache/huggingface'
$env:PYTHONIOENCODING='utf-8'
$studioRoot='D:/Project/LASTRAIL/Tools/FrameStudio/runtime'
New-Item -ItemType Directory -Force -Path $studioRoot | Out-Null
if(!(Test-Path "$studioRoot/ComfyUI")){
 git clone --depth 1 https://github.com/Comfy-Org/ComfyUI.git "$studioRoot/ComfyUI"
 if($LASTEXITCODE -ne 0){throw 'ComfyUI download failed'}
}
if(!(Test-Path "$studioRoot/venv/Scripts/python.exe")){
 & 'C:/Users/dlstj/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -m venv "$studioRoot/venv"
 if($LASTEXITCODE -ne 0){throw 'venv creation failed'}
}
$studioPython="$studioRoot/venv/Scripts/python.exe"
& $studioPython -m pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128
if($LASTEXITCODE -ne 0){throw 'PyTorch install failed'}
& $studioPython -m pip install -r "$studioRoot/ComfyUI/requirements.txt"
if($LASTEXITCODE -ne 0){throw 'ComfyUI dependency install failed'}
& $studioPython -m pip install huggingface_hub
if($LASTEXITCODE -ne 0){throw 'Model downloader install failed'}
& $studioPython "$PSScriptRoot/download_local_model.py"
if($LASTEXITCODE -ne 0){throw 'Model download failed'}
git -C "$studioRoot/ComfyUI" rev-parse HEAD
& $studioPython -c "import torch; print(torch.__version__, torch.cuda.is_available())"
