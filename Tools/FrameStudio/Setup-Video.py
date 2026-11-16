"""Download official LTX-Video weights; no package or custom-node installation."""
import os,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parent
os.environ['HF_HOME']=str(ROOT/'runtime/cache/huggingface')
os.environ['HF_HUB_DISABLE_XET']='1'
from huggingface_hub import hf_hub_download
items=[('Lightricks/LTX-Video','ltxv-2b-0.9.8-distilled-fp8.safetensors','checkpoints'),('Comfy-Org/mochi_preview_repackaged','split_files/text_encoders/t5xxl_fp8_e4m3fn_scaled.safetensors','text_encoders')]
for repo,name,kind in items:
 target=ROOT/'runtime/ComfyUI/models'/kind/Path(name).name
 if target.exists():print('Present',target,flush=True);continue
 print('Downloading',repo,name,flush=True)
 source=Path(hf_hub_download(repo_id=repo,filename=name,local_dir=ROOT/'runtime/video-downloads'/repo.split('/')[0],token=False))
 target.parent.mkdir(parents=True,exist_ok=True)
 staged=target.with_suffix('.installing');shutil.copy2(source,staged);staged.replace(target)
 print('Installed',target,target.stat().st_size,flush=True)
