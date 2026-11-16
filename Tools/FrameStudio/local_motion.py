"""Reference preparation and twelve-frame sampling; not approval or animation inference."""
from pathlib import Path
from PIL import Image

def validate_request(data):
    if not isinstance(data,dict): raise ValueError('제작 요청을 확인하세요.')
    if data.get('approved_reference') is not True: raise ValueError('사용할 원화의 승인을 확인하세요.')
    prompt=data.get('prompt');seed=data.get('seed',42)
    if not isinstance(prompt,str) or not 1<=len(prompt.strip())<=4000:
        raise ValueError('동작 프롬프트를 1~4000자로 입력하세요.')
    if type(seed) is not int or not 0<=seed<2**32: raise ValueError('시드는 0~4294967295 정수입니다.')
    ref=data.get('reference','first')
    if not isinstance(ref,str) or len(ref)>80: raise ValueError('원화 선택을 확인하세요.')
    profile=data.get('profile','idle_reference')
    if profile not in ('standard','idle_reference','wind_reference'):raise ValueError('지원하지 않는 생성 설정')
    return dict(prompt=prompt.strip(),seed=seed,reference=ref,approved_reference=True,profile=profile)

def select_indices(count):
    if type(count) is not int or count<12: raise ValueError('12장 이상의 실제 생성 프레임이 필요합니다.')
    return [round(i*(count-1)/11) for i in range(12)]

def prepare_reference(source,target,actor_fit=False):
    with Image.open(source) as image:
        image.load();image=image.convert('RGBA');original=list(image.size)
        crop=(0,0,image.width,image.height)
        if actor_fit:
            bounds=image.getchannel('A').point(lambda v:255 if v>=8 else 0).getbbox()
            if not bounds:raise ValueError('원화에 보이는 인물이 없습니다.')
            pad=max(8,round(max(bounds[2]-bounds[0],bounds[3]-bounds[1])*.035))
            crop=(max(0,bounds[0]-pad),max(0,bounds[1]-pad),min(image.width,bounds[2]+pad),min(image.height,bounds[3]+pad))
            image=image.crop(crop)
        image.thumbnail((272,480) if actor_fit else (288,512),Image.Resampling.LANCZOS)
        xy=((288-image.width)//2,(512-image.height)//2)
        canvas=Image.new('RGBA',(288,512),(52,64,73,255));canvas.alpha_composite(image,xy)
        canvas.convert('RGB').save(target)
        return dict(original=original,resized=list(image.size),offset=list(xy),canvas=[288,512],crop=list(crop),
                    method=('alpha>=8 foreground bounds plus 3.5% padding; aspect-fit; source unchanged' if actor_fit else 'aspect-fit with neutral background; no character cropping or stretch'))

def extract_twelve(source,folder):
    folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    paths=[]
    with Image.open(source) as image:
        count=image.n_frames
        if count>300 or image.width*image.height>2048*2048: raise ValueError('생성 영상 규격 초과')
        indices=select_indices(count)
        for out_index,source_index in enumerate(indices):
            image.seek(source_index);frame=image.convert('RGBA')
            path=folder/f'{out_index+1:02d}.png';frame.save(path);paths.append(path)
    return dict(paths=paths,source_indices=indices,source_frames=count,alpha_verified=False,
                method='uniform sampling of generated video; no interpolation; no automatic art approval')
