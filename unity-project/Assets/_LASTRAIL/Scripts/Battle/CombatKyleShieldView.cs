using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle {
 // Borrows authored FX textures; the combat snapshot owns amount and lifetime.
 public sealed class CombatKyleShieldView:MonoBehaviour {
  RawImage first,second;
  Texture2D[] frames;
  public bool Visible {get;private set;}
  public void Render(bool active,float combatSeconds,bool facesRight=false){
   if(first==null){
    first=GetComponent<RawImage>();
    frames=new[]{LastRail.Resources.PreparedContent.Load<Texture2D>("CombatPresentation/KyleGuard/guard-fx"),LastRail.Resources.PreparedContent.Load<Texture2D>("CombatPresentation/KyleGuard/guard-flow-01"),LastRail.Resources.PreparedContent.Load<Texture2D>("CombatPresentation/KyleGuard/guard-flow-02")};
    if(System.Array.Exists(frames,t=>t==null))throw new System.InvalidOperationException("Kyle guard FX source is missing.");
    var child=new GameObject("GuardFlow",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));child.transform.SetParent(transform,false);
    second=child.GetComponent<RawImage>();second.rectTransform.anchorMin=Vector2.zero;second.rectTransform.anchorMax=Vector2.one;second.rectTransform.offsetMin=second.rectTransform.offsetMax=Vector2.zero;
    first.raycastTarget=second.raycastTarget=false;
   }
   first.uvRect=second.uvRect=facesRight?new Rect(1,0,-1,1):new Rect(0,0,1,1);
   Visible=active;first.enabled=second.enabled=active;
   if(!active)return;
   float phase=Mathf.Repeat(combatSeconds/.72f,1)*3;int index=Mathf.FloorToInt(phase);float mix=phase-index;
   first.texture=frames[index];second.texture=frames[(index+1)%3];
   first.color=new Color(1,1,1,.74f*(1-mix));second.color=new Color(1,1,1,.74f*mix);
  }
 }
}
