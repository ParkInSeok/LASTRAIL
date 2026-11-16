"""Project workflow selection; independent from the frame image provider."""
METHODS={
 'frame_authoring':{'label':'프레임 작화','description':'주요 자세·중간 프레임을 개별 제작하고 시간표로 연결합니다. 이미지 AI도 사용할 수 있습니다.','route':'/'},
 'ltx':{'label':'로컬 AI 모션(LTX)','description':'원화·동작 가이드로 영상을 생성한 뒤 프레임을 추출합니다.','route':'/local-motion'},
}
def selected(p):return p.get('production_method','frame_authoring')
def select(p,value):
 if not isinstance(value,str) or value not in METHODS:raise ValueError('제작방식을 선택하세요: 프레임 작화 / 로컬 AI 모션(LTX)')
 p['production_method']=value
def require(p,method):
 # Old callers without an explicit selection retain their existing API behavior.
 if 'production_method' in p and selected(p)!=method:
  raise ValueError('제작방식을 '+METHODS[method]['label']+'로 먼저 선택하세요. 자동 전환하지 않습니다.')
