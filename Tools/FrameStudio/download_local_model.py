from pathlib import Path
from huggingface_hub import hf_hub_download
root=Path(__file__).resolve().parent/'runtime/ComfyUI/models/checkpoints'
root.mkdir(parents=True,exist_ok=True)
print(hf_hub_download(repo_id='stable-diffusion-v1-5/stable-diffusion-v1-5',filename='v1-5-pruned-emaonly.safetensors',local_dir=root))
