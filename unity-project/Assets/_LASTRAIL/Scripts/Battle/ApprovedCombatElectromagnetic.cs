using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 CombatElectromagneticField electromagnetic;
 void BindElectromagnetic(JObject state){electromagnetic=Rect("ElectromagneticField",exteriorActors,0,0,720,1280).gameObject.AddComponent<CombatElectromagneticField>();electromagnetic.Bind(trainLayer.rectTransform,state);RefreshElectromagnetic(state);}
 void RefreshElectromagnetic(JObject state){if(electromagnetic==null)return;electromagnetic.Simplified=presentation.Mode==CombatPresentationMode.Simplified;electromagnetic.Refresh(state,id=>units.TryGetValue(id,out var u)?(Vector3?)u.Art.rectTransform.TransformPoint(u.Art.rectTransform.rect.center):null);electromagnetic.transform.SetAsLastSibling();}
}
}
