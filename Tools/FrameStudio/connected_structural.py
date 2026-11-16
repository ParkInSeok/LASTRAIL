"""Full-canvas SD1.5 Lineart edit with explicit protected-region restoration.

Independent profile: does not relax the local 512px refinement contract.
RGB/alpha preservation here is before any separately recorded matting stage.
"""
import json
from pathlib import Path
from PIL import Image,ImageChops
from structural import workflow

class ConnectedStructuralEdit:
 def __init__(self,provider):self.provider=provider
 def generate(self,request,original,mask,protected,lineart,work):
  work=Path(work);original=original.convert('RGBA')
  denoise=request.get('denoise',0.18)
  if type(denoise) not in (int,float) or not 0<denoise<=1:raise ValueError('Denoise must be a finite number in (0,1]')
  if original.size!=(1254,1254) or mask.size!=original.size or protected.size!=original.size:raise ValueError('Connected profile requires native 1254 canvas and matching masks')
  for image in [mask,protected]:
   if image.mode!='L' or set(image.tobytes())-{0,255}:raise ValueError('Binary L masks required; no implicit conversion or feathering')
  if not mask.getbbox():raise ValueError('Empty edit mask')
  if ImageChops.multiply(mask,protected).getbbox():raise ValueError('Edit mask overlaps protected pixels')
  with Image.open(lineart) as im:
   if im.size!=(1280,1280) or im.mode not in ['RGB','RGBA']:raise ValueError('Existing padded 1280 lineart required')
   if im.mode=='RGBA' and im.getchannel('A').getextrema()!=(255,255):raise ValueError('Opaque lineart required')
  if request.get('neighbor_indices') or request.get('reference_conditioned'):raise ValueError('No unimplemented neighbor or identity conditioning')
  if self.provider.cancelled.is_set():raise ValueError('Cancelled before generation')
  work.mkdir(parents=True,exist_ok=False)
  app=Image.new('RGBA',(1280,1280),'white');app.alpha_composite(original,(13,13));app.convert('RGB').save(work/'appearance.png')
  padded=Image.new('L',(1280,1280));padded.paste(mask,(13,13));padded.convert('RGB').save(work/'noise-mask.png')
  mask.save(work/'edit-mask.png');protected.save(work/'protected-mask.png')
  cfg=workflow();g=cfg['workflow']
  g['7']['inputs']['denoise']=denoise
  g['14']={'class_type':'LoadImage','inputs':{'image':'{{ref:mask}}'}}
  g['15']={'class_type':'ImageToMask','inputs':{'image':['14',0],'channel':'red'}}
  g['16']={'class_type':'SetLatentNoiseMask','inputs':{'samples':['4',0],'mask':['15',0]}}
  g['7']['inputs']['latent_image']=['16',0]
  g['9']['inputs']['filename_prefix']='Kyle-connected-protected'
  sub=dict(request,reference_roles={'appearance':'appearance.png','lineart':Path(lineart).name,'mask':'noise-mask.png'},reference_assets=['appearance.png',Path(lineart).name,'noise-mask.png'],trial_only=True)
  raw=self.provider.generate(sub,[work/'appearance.png',Path(lineart),work/'noise-mask.png'],work/'raw',workflow_override=cfg)
  if self.provider.cancelled.is_set():raise ValueError('Cancelled before composition')
  with Image.open(raw) as im:
   if im.size!=original.size:raise ValueError('Output size mismatch; no rescaling allowed')
   layer=im.convert('RGBA');layer.putalpha(original.getchannel('A'))
  result=Image.composite(layer,original,mask)
  outside=ImageChops.invert(mask);delta=ImageChops.difference(result,original)
  if any(ImageChops.multiply(c,outside).getbbox() for c in delta.split()):raise ValueError('Outside-mask pixel preservation failed')
  result.save(work/'protected-result.png')
  (work/'preservation.json').write_text(json.dumps({'profile':'connected-structural-fullcanvas','outside_mask_rgba_exact':True,'alpha_preserved_before_matting':True,'mask_bbox':mask.getbbox(),'canvas':original.size,'raw_output':str(raw),'latent_mask_consumed':True,'final_restore':'binary source copy outside mask; no blend, warp or resize','limitations':'VAE can alter raw protected pixels; final restoration enforces exact output. Does not guarantee seams or anatomy. Transparency is a separate stage.'},indent=2))
  return work/'protected-result.png'
