"""Independent articulated scene rules. Coordinates are 1080x1920 design pixels."""
import copy, math, re
from cutscene_model import number

DEFAULT_POSE=dict(x=0.,y=0.,rotation=0.,sx=1.,sy=1.,opacity=1.,bend=0.,asset=None,ease="smooth")
RANGES={"x":(-4320,4320),"y":(-7680,7680),"rotation":(-1440,1440),
        "sx":(.05,8),"sy":(.05,8),"opacity":(0,1),"bend":(-160,160)}

def validate_scene(data,media):
    if not isinstance(data,dict):raise ValueError("동작 데이터가 필요합니다.")
    frames=number(data.get("frames",150),1,900,"동작 길이",True)
    background=data.get("background","#101820")
    if not isinstance(background,str) or not re.fullmatch(r"#[0-9a-fA-F]{6}",background):
        raise ValueError("배경 색상 오류")
    ref=data.get("reference")
    if ref is not None and (not isinstance(ref,str) or ref not in media or media[ref]["kind"]!="video"):
        raise ValueError("등록한 비교 영상을 선택하세요.")
    rows=data.get("parts",[])
    if not isinstance(rows,list) or len(rows)>32:raise ValueError("파츠는 최대32개입니다.")
    parts=[];ids=set()
    for row in rows:
        if not isinstance(row,dict):raise ValueError("파츠 형식 오류")
        ident=row.get("id")
        if not isinstance(ident,str) or not re.fullmatch(r"[a-zA-Z0-9_-]{1,48}",ident) or ident in ids:
            raise ValueError("파츠 ID가 없거나 중복됩니다.")
        ids.add(ident)
        name=row.get("name",ident)
        if not isinstance(name,str) or not 1<=len(name)<=80:raise ValueError("파츠 이름 오류")
        parent=row.get("parent")
        if parent is not None and not isinstance(parent,str):raise ValueError("부모 파츠 오류")
        kind=row.get("kind","sprite")
        if kind not in ("group","sprite","rect","ellipse","ring"):raise ValueError("파츠 종류 오류")
        color=row.get("color","#d8b58a")
        if not isinstance(color,str) or not re.fullmatch(r"#[0-9a-fA-F]{6}",color):raise ValueError("파츠 색상 오류")
        p=dict(id=ident,name=name,parent=parent,kind=kind,color=color,visible=row.get("visible",True))
        if type(p["visible"]) is not bool:raise ValueError("표시 상태 오류")
        for k,lo,hi,default in [("width",0 if kind=="group" else 1,2160,100),
                                ("height",0 if kind=="group" else 1,3840,100),
                                ("pivot_x",0,1,.5),("pivot_y",0,1,.5),
                                ("bend_start",0,.95,.5)]:
            p[k]=number(row.get(k,default),lo,hi,k)
        keys=row.get("keys",[])
        if not isinstance(keys,list) or not 1<=len(keys)<=120:raise ValueError("파츠마다1~120개의 자세 키가 필요합니다.")
        prior=dict(DEFAULT_POSE,asset=row.get("asset"));last=-1;p["keys"]=[]
        for key in keys:
            if not isinstance(key,dict):raise ValueError("자세 키 형식 오류")
            frame=number(key.get("frame"),0,frames-1,"키 시각",True)
            if frame<=last:raise ValueError("자세 키는 중복 없이 시간순이어야 합니다.")
            pose={**prior,**key}
            for k,(lo,hi) in RANGES.items():pose[k]=number(pose[k],lo,hi,k)
            if pose["ease"] not in ("linear","smooth","hold"):raise ValueError("보간 방식 오류")
            asset=pose["asset"]
            if asset is not None and (not isinstance(asset,str) or asset not in media or media[asset]["kind"]!="image"):
                raise ValueError("등록한 파츠 이미지를 선택하세요.")
            if kind=="sprite" and asset is None:raise ValueError("이미지 파츠에 작화가 없습니다.")
            pose={k:pose[k] for k in DEFAULT_POSE};pose["frame"]=frame
            p["keys"].append(pose);prior=pose;last=frame
        if p["keys"][0]["frame"]!=0:raise ValueError("첫 자세 키는0프레임이어야 합니다.")
        parts.append(p)
    lookup={p["id"]:p for p in parts}
    for p in parts:
        trail=set();cur=p
        while cur["parent"] is not None:
            if cur["parent"] not in lookup:raise ValueError("부모 파츠가 없습니다.")
            if cur["id"] in trail or len(trail)>=16:raise ValueError("관절 연결이 순환하거나 너무 깊습니다.")
            trail.add(cur["id"]);cur=lookup[cur["parent"]]
    width=data.get('width',1080);height=data.get('height',1920)
    if (width,height) not in ((1080,1920),(1920,1080)):raise ValueError('동작 캔버스는 1080×1920 또는 1920×1080입니다.')
    return dict(version=1,width=width,height=height,frames=frames,background=background,reference=ref,parts=parts)

def sample_pose(part,frame):
    keys=part["keys"];a=keys[0];b=a
    for key in keys[1:]:
        if frame<key["frame"]:b=key;break
        a=key;b=key
    out={**DEFAULT_POSE,**a}
    if b["frame"]==a["frame"] or out["ease"]=="hold":return out
    t=max(0,min(1,(frame-a["frame"])/(b["frame"]-a["frame"])))
    if out["ease"]=="smooth":t=t*t*(3-2*t)
    for k in RANGES:out[k]=out[k]+(b.get(k,DEFAULT_POSE[k])-out[k])*t
    return out

def multiply(a,b):
    return [a[0]*b[0]+a[2]*b[1],a[1]*b[0]+a[3]*b[1],
            a[0]*b[2]+a[2]*b[3],a[1]*b[2]+a[3]*b[3],
            a[0]*b[4]+a[2]*b[5]+a[4],a[1]*b[4]+a[3]*b[5]+a[5]]

def world_matrices(scene,frame):
    parts={p["id"]:p for p in scene["parts"]};result={}
    def visit(ident):
        if ident in result:return result[ident]
        p=parts[ident];pose=sample_pose(p,frame);r=math.radians(pose["rotation"])
        c,s=math.cos(r),math.sin(r)
        matrix=[c*pose["sx"],s*pose["sx"],-s*pose["sy"],c*pose["sy"],pose["x"],pose["y"]]
        opacity=pose["opacity"] if p["visible"] else 0
        if p["parent"]:
            parent=visit(p["parent"]);matrix=multiply(parent["matrix"],matrix);opacity*=parent["opacity"]
        result[ident]=dict(matrix=matrix,opacity=opacity,asset=pose["asset"],bend=pose["bend"])
        return result[ident]
    for ident in parts:visit(ident)
    return result

def demo_scene():
    """Original geometric rig exercise, deliberately not labelled Nyx finished art."""
    parts=[]
    def add(ident,name,kind,w,h,x,y,color,parent="camera",rot=0,px=.5,py=.5,keys=None):
        first=dict(frame=0,x=x,y=y,rotation=rot,sx=1,sy=1,opacity=1,ease="smooth")
        parts.append(dict(id=ident,name=name,kind=kind,width=w,height=h,parent=parent,
            color=color,pivot_x=px,pivot_y=py,visible=True,keys=[first]+(keys or [])))
    add("camera","전체 구도","group",0,0,540,1060,"#000000",None,
        keys=[dict(frame=96,x=575,y=1050,sx=1.12,sy=1.12),dict(frame=149,x=575,y=1050,sx=1.1,sy=1.1)])
    add("ring1","분절 고리 뒤","ring",810,1000,0,-220,"#eeb99a",rot=-25,
        keys=[dict(frame=96,rotation=35,sx=1.1,sy=1.1),dict(frame=149,rotation=38)])
    add("coat","코트 연습 파츠","rect",290,480,0,130,"#244b55",rot=8,
        keys=[dict(frame=80,rotation=18),dict(frame=149,rotation=9)])
    add("body","몸통","rect",250,340,0,-70,"#315b65")
    add("head","머리","ellipse",170,210,0,-360,"#dcb8a0",
        keys=[dict(frame=96,rotation=-6),dict(frame=149,rotation=-3)])
    add("leftarm","촉매 팔","rect",70,240,125,-170,"#315b65",rot=-25,py=0)
    add("catalyst","열린 촉매","ring",110,170,0,240,"#c8a34c","leftarm")
    add("upperarm","오른쪽 어깨→팔꿈치","rect",80,200,-115,-175,"#315b65",rot=-35,py=0,
        keys=[dict(frame=20,rotation=-55),dict(frame=96,rotation=65),dict(frame=149,rotation=62)])
    add("forearm","팔꿈치→손목","rect",66,180,0,190,"#dcb8a0","upperarm",rot=-60,py=0,
        keys=[dict(frame=20,rotation=-85),dict(frame=96,rotation=-135,sy=.7),dict(frame=149,rotation=-130,sy=.75)])
    add("palm","손바닥 원근","ellipse",100,125,0,180,"#e4c1a9","forearm",
        keys=[dict(frame=30,sx=1,sy=1),dict(frame=96,sx=2.1,sy=2.1,rotation=-10),
              dict(frame=149,sx=2.05,sy=2.05,rotation=-8)])
    for i,(x,y,h,r) in enumerate([(-46,-14,55,-55),(-31,-52,72,-16),(-7,-62,87,-3),(17,-59,80,8),(39,-42,62,23)]):
        add("finger"+str(i),"손가락 "+str(i+1),"rect",18,h,x,y,"#e4c1a9","palm",rot=r,py=1)
    add("ring2","전경 고리","ring",900,590,0,-60,"#d89c7f",rot=30,
        keys=[dict(frame=96,rotation=-25,opacity=.8),dict(frame=149,rotation=-23,opacity=.65)])
    return dict(version=1,frames=150,background="#101820",reference=None,parts=parts)
