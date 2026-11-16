"""Bounded image-to-video request and native ComfyUI graph (no custom nodes)."""
import math
MODEL='ltxv-2b-0.9.8-distilled-fp8.safetensors'
ENCODER='t5xxl_fp8_e4m3fn_scaled.safetensors'
DEFAULT_CANVAS=(288,512)
SUPPORTED_CANVASES=(DEFAULT_CANVAS,(384,512))

def validate_request(data,media):
 if not isinstance(data,dict):raise ValueError('잘못된 생성 요청')
 asset=data.get('asset');prompt=data.get('prompt');seed=data.get('seed',42)
 if not isinstance(asset,str) or asset not in media or media[asset]['kind']!='image':raise ValueError('등록된 참고 이미지를 선택하세요.')
 if not isinstance(prompt,str) or not 1<=len(prompt.strip())<=4000:raise ValueError('동작 설명은 1~4000자로 입력하세요.')
 if type(seed)!=int or not 0<=seed<=2**32-1:raise ValueError('시드는 0~4294967295 정수입니다.')
 profile=data.get('profile','standard')
 if profile not in ('standard','idle_reference','wind_reference'):raise ValueError('지원하지 않는 생성 설정')
 width=data.get('width',DEFAULT_CANVAS[0]);height=data.get('height',DEFAULT_CANVAS[1])
 if type(width)!=int or type(height)!=int or (width,height) not in SUPPORTED_CANVASES:raise ValueError('지원하는 영상 캔버스는 288×512 또는 384×512입니다.')
 result=dict(asset=asset,prompt=prompt.strip(),seed=seed,width=width,height=height,frames=49,fps=16,model=MODEL,encoder=ENCODER,profile=profile)
 if 'middle_guide' in data:
  guide=data['middle_guide']
  if not isinstance(guide,dict):raise ValueError('중간 가이드 설정을 확인하세요.')
  guide_asset=guide.get('asset');index=guide.get('frame_index');strength=guide.get('strength')
  if not isinstance(guide_asset,str) or guide_asset not in media or media[guide_asset]['kind']!='image':raise ValueError('등록된 중간 가이드 이미지를 선택하세요.')
  if type(index)!=int or not 0<index<result['frames']-1:raise ValueError('중간 가이드 위치는 1~47 프레임입니다.')
  if type(strength) not in (int,float) or not math.isfinite(strength) or not 0<strength<=1:raise ValueError('중간 가이드 강도는 0 초과 1 이하입니다.')
  result['middle_guide']=dict(asset=guide_asset,frame_index=index,strength=strength)
 return result

def workflow(r,image,prefix,middle_image=None):
 def node(kind,**inputs):return dict(class_type=kind,inputs=inputs)
 graph={
 '1':node('CheckpointLoaderSimple',ckpt_name=MODEL),
 '2':node('CLIPLoader',clip_name=ENCODER,type='ltxv',device='default'),
 '3':node('CLIPTextEncode',clip=['2',0],text=r['prompt']),
 '4':node('CLIPTextEncode',clip=['2',0],text='deformed hands, extra limbs, flicker, text, watermark, camera cut'),
 '5':node('LoadImage',image=image),
 '6':node('LTXVImgToVideo',positive=['3',0],negative=['4',0],vae=['1',2],image=['5',0],width=r['width'],height=r['height'],length=r['frames'],batch_size=1,strength=1.0),
 '7':node('LTXVConditioning',positive=['6',0],negative=['6',1],frame_rate=r['fps']),
 '8':node('KSamplerSelect',sampler_name='euler'),
 '9':node('ManualSigmas',sigmas='1.0, 0.9937, 0.9875, 0.9812, 0.975, 0.9094, 0.725, 0.4219, 0.0'),
 '10':node('SamplerCustom',model=['1',0],add_noise=True,noise_seed=r['seed'],cfg=1.0,positive=['7',0],negative=['7',1],sampler=['8',0],sigmas=['9',0],latent_image=['6',2]),
 '11':node('VAEDecodeTiled',samples=['10',0],vae=['1',2],tile_size=256,overlap=64,temporal_size=16,temporal_overlap=4),
 '12':node('SaveAnimatedWEBP',images=['11',0],filename_prefix='FrameStudio/'+prefix,fps=r['fps'],lossless=True,quality=100,method='default')}
 if r.get('profile') in ('idle_reference','wind_reference'):
  graph['13']=node('LTXVAddGuide',positive=['7',0],negative=['7',1],vae=['1',2],latent=['6',2],image=['5',0],frame_idx=r['frames']-1,strength=0.65 if r['profile']=='wind_reference' else 1.0)
  graph['10']['inputs'].update(positive=['13',0],negative=['13',1],latent_image=['13',2])
  graph['14']=node('LTXVCropGuides',positive=['13',0],negative=['13',1],latent=['10',0])
  graph['11']['inputs']['samples']=['14',2]
 if r.get('middle_guide'):
  if not middle_image:raise ValueError('중간 가이드 이미지가 업로드되지 않았습니다.')
  guide=r['middle_guide'];previous='13' if '13' in graph else '7';latent=['13',2] if '13' in graph else ['6',2]
  graph['16']=node('LoadImage',image=middle_image)
  graph['15']=node('LTXVAddGuide',positive=[previous,0],negative=[previous,1],vae=['1',2],latent=latent,image=['16',0],frame_idx=guide['frame_index'],strength=guide['strength'])
  graph['10']['inputs'].update(positive=['15',0],negative=['15',1],latent_image=['15',2])
  graph['14']=node('LTXVCropGuides',positive=['15',0],negative=['15',1],latent=['10',0])
  graph['11']['inputs']['samples']=['14',2]
 return graph
