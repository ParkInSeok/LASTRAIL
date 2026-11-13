using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 readonly CombatDangerAudioFeedback dangerFeedback=new CombatDangerAudioFeedback();
 AudioSource dangerAudio;
 void StopDangerAudio(){if(dangerAudio!=null)dangerAudio.Stop();}
 void RefreshDangerAudio(JObject state){
  bool audible=isActiveAndEnabled&&!driver.RecoveryRequired&&cueVolume>0;
  string cue=dangerFeedback.Observe(state,audible);
  if(!audible||!dangerFeedback.Active)StopDangerAudio();
  if(cue==null||effectAudio==null)return;
  if(dangerAudio==null){dangerAudio=gameObject.AddComponent<AudioSource>();dangerAudio.playOnAwake=false;dangerAudio.spatialBlend=0;}
  dangerAudio.Stop();dangerAudio.PlayOneShot(effectAudio.WarningClip,Mathf.Clamp01(effectAudio.WarningGain*cueVolume/.65f));
  PresentationCue?.Invoke("WARNING",cue);
 }
}
}
