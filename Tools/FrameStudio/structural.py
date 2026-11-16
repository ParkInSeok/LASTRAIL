"""One-frame SD1.5 lineart trial, using LocalProvider's existing override boundary."""
import json
from pathlib import Path
from PIL import Image

def workflow():
 cfg=json.loads(Path(__file__).with_name('local-workflow.json').read_text(encoding='utf-8'))
 g=cfg['workflow']
 if g['1']['inputs']['ckpt_name']!='v1-5-pruned-emaonly.safetensors':raise ValueError('SD1.5 checkpoint required')
 g['2']['inputs']['image']='{{ref:appearance}}'
 del g['3']
 g['4']['inputs']['pixels']=['2',0]
 g['7']['inputs'].update(positive=['12',0],negative=['12',1],steps=24,cfg=4,denoise=0.18)
 g['10']={'class_type':'LoadImage','inputs':{'image':'{{ref:lineart}}'}}
 g['11']={'class_type':'ControlNetLoader','inputs':{'control_net_name':'control_v11p_sd15_lineart.fp16.safetensors'}}
 g['12']={'class_type':'ControlNetApplyAdvanced','inputs':{'positive':['5',0],'negative':['6',0],'control_net':['11',0],'image':['10',0],'strength':1.0,'start_percent':0.0,'end_percent':1.0,'vae':['1',2]}}
 g['13']={'class_type':'ImageCrop','inputs':{'image':['8',0],'width':1254,'height':1254,'x':13,'y':13}}
 g['9']['inputs'].update(images=['13',0],filename_prefix='Kyle4-structure-v05')
 cfg['label']='1254 native / 1280 padded SD1.5 lineart trial'
 return cfg

def generate(provider,request,refs,work):
 if set(request.get('reference_roles',{}))!={'appearance','lineart'} or request.get('draft'):raise ValueError('Separate appearance/lineart roles required; no draft substitution')
 by_name={p.name:p for p in refs}
 for name in request['reference_roles'].values():
  with Image.open(by_name[name]) as im:
   if im.size!=(1280,1280) or im.mode not in ('RGB','RGBA'):raise ValueError('1280 RGB input required')
   if im.mode=='RGBA' and im.getchannel('A').getextrema()!=(255,255):raise ValueError('Inputs must be opaque')
 return provider.generate(request,refs,work,workflow_override=workflow())
