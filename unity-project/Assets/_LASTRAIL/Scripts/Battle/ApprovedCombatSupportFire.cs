using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 CombatSupportFireView supportFire;
 void BindSupportFire(JObject state){if(assets.SupportFire==null)return;supportFire=gameObject.AddComponent<CombatSupportFireView>();supportFire.Bind(trainLayer.rectTransform,exteriorActors,assets.SupportFire,state);RefreshSupportFire(state);}
 void RefreshSupportFire(JObject state){if(supportFire==null)return;supportFire.Refresh(state,id=>units.TryGetValue(id,out var u)?(Vector3?)u.Art.rectTransform.TransformPoint(u.Art.rectTransform.rect.center):null,presentation.Mode==CombatPresentationMode.Simplified);}
}
}
