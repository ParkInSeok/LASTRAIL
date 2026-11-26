from pathlib import Path
import subprocess,sys
base=Path('D:/Project/LASTRAIL/QA/DirectionImpact-20260921')
out=base/'ultimate-three-pose-v08-gif';out.mkdir(parents=True,exist_ok=True)
code=Path('D:/CodexData/Home/worktrees/5ca3/LASTRAIL/export-five-pose-gif.py').read_text(encoding='utf-8')
changes={
"ultimate-five-pose-v07-gif":"ultimate-three-pose-v08-gif",
"new=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))":"new=[]",
"sources=[old/'raised.png',Path(new[0]['source']),old/'mid.png',Path(new[1]['source']),old/'aim.png']":"sources=[old/'raised.png',old/'middle-selected-e076.png',old/'aim.png']",
"[(280,520,0),(650,830,1),(920,1130,2),(1230,1370,3)]":"[(500,600,0),(850,1000,1)]",
"return poses[4]":"return poses[2]",
"palette_sheet=Image.new('RGB',(W*5,H))":"palette_sheet=Image.new('RGB',(W*4,H))",
"[220,570,870,1180,1480]":"[220,750,1200,1480]",
"reina-ultimate-5poses-2.5s.gif":"reina-ultimate-3poses-2.5s.gif",
"sheet=Image.new('RGB',(W*5,H),(8,12,17))":"sheet=Image.new('RGB',(W*3,H),(8,12,17))",
"[220,570,870,1180,1400]":"[220,750,1200]",
"(1080,384)":"(648,384)",
"five-poses.png":"three-poses.png",
"check.n_frames>5":"check.n_frames>3",
"'poseCount':5":"'poseCount':3",
"direction.ultimate-five-pose-v07.20261002":"direction.ultimate-three-pose-v08.20261002",
"# 新規 중간 원화2장":"# 기존 3원화 사용 · 신규 생성없음",
"# 5자세 · 2.5초 GIF":"# 3자세 · 2.5초 GIF",
"사용자 GIF/3→5동작원화 요청. 기존raised·첨부로선택한mid·aim 보존, 두구간 중간원화2장 추가.":"사용자 5자세 대신 기존3자세 GIF 요청. 기존raised·첨부선택mid(e076)·aim만 사용. v07추가원화2장 신규조합 제외·역사보존.",
"PNGalpha5장":"PNGalpha3장",
"독립PNG5개":"독립PNG3개",
"5자세":"3자세",
"동작 원화 5장":"동작 원화 3장",
"든 자세 → 조준 진입 → 선택한 중간 자세 → 정면 조준 진입 → 정면 조준·격발":"든 자세 → 선택한 중간 조준 → 정면 조준·격발"
}
for a,b in changes.items():
    if a not in code:raise RuntimeError('Missing replacement: '+a)
    code=code.replace(a,b)
script=out/'export-gif.py';script.write_text(code,encoding='utf-8')
subprocess.run([sys.executable,str(script)],check=True)
