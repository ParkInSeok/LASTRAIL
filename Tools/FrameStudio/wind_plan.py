"""Authoritative artistic wind settings and redraw guides; never deform pixels."""
import copy, json, math
from train_wind import TrainWindProfile

PARTS=('hair','ribbon','coat')
DEFAULTS={'enabled':False,'strength':50,'forward_angle':-90,'flutter':.12,'region':'hair','parts':{'hair':{'gain':1.,'delay':0.},'ribbon':{'gain':1.2,'delay':.08},'coat':{'gain':.8,'delay':.15}}}

def number(value, low, high):
    if type(value) not in (int,float) or not math.isfinite(value) or not low<=value<=high:raise ValueError('바람 수치의 허용 범위를 확인하세요.')
    return value

def validate_settings(data):
    if not isinstance(data,dict) or type(data.get('enabled'))!=bool:raise ValueError('바람 사용 여부를 선택하세요.')
    cfg={'enabled':data['enabled'],'strength':number(data.get('strength'),0,120),'forward_angle':number(data.get('forward_angle'),-180,180),'flutter':number(data.get('flutter'),0,.25),'region':data.get('region'),'parts':{}}
    if cfg['region'] not in (*PARTS,'mixed'):raise ValueError('재작화 대상 부위를 선택하세요.')
    parts=data.get('parts')
    if not isinstance(parts,dict) or set(parts)!=set(PARTS):raise ValueError('머리·리본·코트 설정을 모두 지정하세요.')
    for part in PARTS:
        value=parts[part]
        if not isinstance(value,dict):raise ValueError('부위별 바람 설정을 확인하세요.')
        cfg['parts'][part]={'gain':number(value.get('gain'),0,2),'delay':number(value.get('delay'),0,.4)}
    return cfg

class WindPlan:
    def __init__(self,settings):
        self.settings=validate_settings(settings)
        angle=math.radians(self.settings['forward_angle'])
        self.profile=TrainWindProfile((math.cos(angle),math.sin(angle)))
    def at_phase(self,phase):
        cfg=self.settings;parts={}
        for name,part in cfg['parts'].items():
            amplitude=4*cfg['strength']/100*part['gain'] if cfg['enabled'] else 0
            offset=self.profile.displacement(phase,amplitude,part['delay'],cfg['flutter'])
            parts[name]={'offset_percent':list(offset),'intensity':min(1.,math.hypot(*offset)/4),'gain':part['gain'],'delay':part['delay']}
        return parts
    def frame(self,p,index):
        if type(index)!=int or not 0<=index<len(p['frames']):raise ValueError('프레임 번호 오류')
        durations=[f['duration_ms'] for f in p['frames']]
        time=sum(durations[:index]);phase=time/max(1,sum(durations[:-1]))
        return {'phase':phase,'time_ms':time,'strength':self.settings['strength'],'region':self.settings['region'],'parts':self.at_phase(phase)}
    def preview(self,p):
        return {'settings':copy.deepcopy(self.settings),'duration_ms':sum(f['duration_ms'] for f in p['frames']),'frames':[self.frame(p,i) for i in range(len(p['frames']))],'samples':[{'phase':i/60,'parts':self.at_phase(i/60)} for i in range(61)],'forward':[ -v for v in self.profile.rear],'wind':list(self.profile.rear),'scope':'Redraw guidance only. Original PNGs unchanged; no rig, translation, rotation or warping.'}
    def note(self,guide):
        return ('\nWind redraw guide: '+json.dumps(guide,ensure_ascii=False,sort_keys=True)+
                '\nThe approved still already includes steady airflow; these values describe an additional gust and recovery. Offset percentages are artistic guides, not a command to displace the original pixels. '
                'Redraw connected bending poses only in the requested mask/part. Preserve hair and cloth length, roots, attachment and occlusion ownership, body and feet. Do not translate, rotate, stretch or warp existing cut pieces to imitate wind. '
                'Use strand-by-strand curved rotation and bending in newly drawn poses, anchored roots, leading middle locks and delayed curl tips; preserve strand identity, volume and front/back overlap. '
                'Before inbetweens, perform a key-pose comparison of rest, maximum gust and delayed recovery at identical canvas coordinates. A saved strength value is not evidence of visible motion: compare the actual silhouette excursion with the accepted motion reference; strength 120 needs a clearly readable larger sweep without longer hair. '
                'Then draw between both surrounding poses, retain the approved exposure timing, and check every adjacent transition plus last-to-first. Avoid an endpoint-biased midpoint, a sudden early leap, repeated maximum poses or frozen parts used to hide flicker. '
                'Reuse a suitable accepted drawing for easing when it fits the actual trajectory; record its source and verify the new neighbors. Keep ink density and width consistent without moving the drawn lines. Pixel preservation and median ink statistics do not prove motion quality. '
                'Review roots, strand overlaps and newly exposed attachment surfaces separately; record static comparison and continuous playback separately, leaving unobserved playback unreviewed. '
                'At zero intensity add no gust. First and last poses return to the approved steady-flow silhouette.')

def configure_wind(p,data):
    settings=validate_settings(data.get('settings'))
    if p.get('wind_settings')==settings:return
    if 'wind_settings' in p:p.setdefault('wind_history',[]).append({'revision':p['revision'],'settings':copy.deepcopy(p['wind_settings'])})
    p['wind_settings']=settings;p['revision']+=1
    for key in ('review_export','export','temporal_review'):p.pop(key,None)

def request_wind(p,index):
    cfg=p.get('wind_settings')
    if not cfg:return None,None
    plan=WindPlan(cfg);guide=plan.frame(p,index)
    return guide,plan.note(guide)
