"""Immutable line support, separately editable internal ink appearance."""
import io
from PIL import Image, ImageChops, ImageFilter, ImageColor
from clip_workflow import decode_png


def record(flow,index):
    item=flow.get('line_protection',{}).get(str(index))
    lines=flow.get('frames',{}).get(str(index),{}).get('line',[])
    return item if item and lines and item['line']==lines[-1]['asset'] else None


def read(repo,asset):
    with Image.open(repo.asset_path(asset)) as im:return im.convert('RGBA')


def appearance_mask(repo,p,index,mask):
    item=record(p['production_flow'],index)
    if not p['production_flow']['config'].get('separate_line_protection') or not item:raise ValueError('승인 선 보호 지도가 필요합니다.')
    def support(asset):
        im=read(repo,asset)
        return ImageChops.multiply(im.convert('L'),im.getchannel('A')).point(lambda x:255 if x else 0)
    # Tiny internal alpha dips are not silhouette boundaries. Repair only the
    # eligibility map; never repair or change the character's actual alpha.
    silhouette=read(repo,item['line']).getchannel('A').point(lambda x:255 if x>128 else 0)
    inside=silhouette.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MinFilter(7))
    allowed=ImageChops.subtract(ImageChops.multiply(support(item['ink']),inside),support(item['protected']))
    if not mask.getbbox() or ImageChops.subtract(mask,allowed).getbbox():raise ValueError('명암 내부선 교정은 원래 선 안에서만 허용하며 외곽·손·발 보호 영역은 제외해야 합니다.')
    return mask


def register(repo,p,data):
    flow=p['production_flow'];index=data['index']
    if not flow['config'].get('separate_line_protection'):raise ValueError('형태·내부선 분리 보호를 먼저 켜세요.')
    line=flow.get('frames',{}).get(str(index),{}).get('line',[])
    if not line:raise ValueError('현재 미채색 선화가 필요합니다.')
    alpha=read(repo,line[-1]['asset']).getchannel('A')
    masks={};raws={}
    for name in ('ink','editable','protected'):
        raw,_,_=decode_png(data.get(name),alpha.size)
        with Image.open(io.BytesIO(raw)) as im:
            value=ImageChops.multiply(im.convert('L'),im.convert('RGBA').getchannel('A'))
        masks[name]=value.point(lambda x:255 if x else 0)
        raws[name]=raw
    if not masks['ink'].getbbox() or not masks['protected'].getbbox():raise ValueError('선 영역과 손·발·겹침 보호 영역이 필요합니다.')
    if not masks['editable'].getbbox():raise ValueError('색·농도 조절을 허용할 내부선을 지정하세요.')
    inside=alpha.point(lambda x:255 if x>128 else 0).filter(ImageFilter.MinFilter(7))
    allowed=ImageChops.subtract(ImageChops.multiply(masks['ink'],inside),masks['protected'])
    if ImageChops.subtract(masks['editable'],allowed).getbbox():raise ValueError('내부선 변경 범위가 선 밖·외곽·보호 부위와 겹칩니다.')
    if ImageChops.subtract(masks['ink'],alpha.point(lambda x:255 if x else 0)).getbbox():raise ValueError('선 영역이 캐릭터 밖으로 나갑니다.')
    flow.setdefault('line_protection',{})[str(index)]={'line':line[-1]['asset'],**{k:repo.put_asset(v) for k,v in raws.items()}}


def apply(repo,p,index,slot,image):
    if not p['production_flow']['config'].get('separate_line_protection') or slot not in ('flat','shadow','gloss','texture','composite'):return image
    item=record(p['production_flow'],index)
    if not item:raise ValueError('현재 선화의 형태·내부선 보호 지도가 필요합니다.')
    line=read(repo,item['line']);masks={}
    for role in ('ink','editable'):
        im=read(repo,item[role]);masks[role]=ImageChops.multiply(im.convert('L'),im.getchannel('A')).point(lambda x:255 if x else 0)
    if slot!='flat':
        from production_flow import mask_basis
        for roles in p['production_flow'].get('masks',{}).get(str(index),{}).values():
            correction=roles.get('ink_appearance',[])
            if not correction:continue
            if correction[-1]['basis']!=mask_basis(p,index):raise ValueError('내부선 교정의 선화 기준이 변경됐습니다.')
            im=read(repo,correction[-1]['asset'])
            mask=ImageChops.multiply(im.convert('L'),im.getchannel('A')).point(lambda x:255 if x else 0)
            masks['editable']=ImageChops.lighter(masks['editable'],appearance_mask(repo,p,index,mask))
    fixed=masks['ink'] if slot=='flat' else ImageChops.subtract(masks['ink'],masks['editable'])
    result=image.copy()
    if slot in ('flat','composite'):
        if slot=='flat':
            # Uncolored antialias RGB is paper/ink mixture, not the approved material color.
            flow=p['production_flow'];covered=Image.new('L',line.size)
            for region in flow['config']['regions']:
                entries=flow.get('masks',{}).get(str(index),{}).get(region['id'],{}).get('base',[])
                if not entries:continue
                from production_flow import mask_basis
                if entries[-1]['basis']!=mask_basis(p,index):raise ValueError('밑색 부위 지도의 선화 기준이 변경됐습니다.')
                im=read(repo,entries[-1]['asset'])
                area=ImageChops.multiply(im.convert('L'),im.getchannel('A')).point(lambda x:255 if x else 0)
                support=ImageChops.multiply(fixed,area);covered=ImageChops.lighter(covered,support)
                result.paste(ImageColor.getrgb(region['base'])+(255,),(0,0,*line.size),support)
            if ImageChops.subtract(fixed,covered).getbbox():raise ValueError('선 영역의 밑색·장식색 지도가 누락됐습니다.')
        else:
            flat=p['production_flow']['frames'][str(index)]['flat'][-1]['asset']
            result.paste(read(repo,flat),(0,0),fixed)
        result.putalpha(line.getchannel('A'))
    else:
        inside=line.getchannel('A').point(lambda x:255 if x else 0)
        result.putalpha(ImageChops.multiply(result.getchannel('A'),ImageChops.subtract(inside,fixed)))
    return result
