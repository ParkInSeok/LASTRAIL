using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 RawImage ActiveIcon(Transform parent,string ability,float x,float y,float size){
  if(!assets.CardPolishEnabled){int original=ability=="op_reina.active"||ability=="op_eve.active"?1:ability=="op_mira.active"?2:0;return Icon(parent,original,x,y,size);}
  int index=assets.ActiveIconIds==null?-1:System.Array.IndexOf(assets.ActiveIconIds,ability);
  if(assets.ActiveIconAtlas==null||index<0||assets.ActiveIconRegions==null||index>=assets.ActiveIconRegions.Length)throw new System.InvalidOperationException("Reviewed active icon missing: "+ability);
  var image=Raw("SkillIcon",parent,assets.ActiveIconAtlas,x,y,size,size);image.uvRect=assets.ActiveIconRegions[index];image.enabled=false;
  var glyph=Rect("GoldAbilityGlyph",image.transform,0,0,size,size).gameObject.AddComponent<CombatConceptGlyph>();glyph.Symbol=index;glyph.raycastTarget=false;glyph.color=new Color(1,.84f,.43f);return image;
 }
 CombatPanelChrome AddChrome(string name,Transform parent,float x,float y,float w,float h,bool leader=false){
  var chrome=Rect(name,parent,x,y,w,h).gameObject.AddComponent<CombatPanelChrome>();chrome.Leader=leader;chrome.raycastTarget=false;return chrome;
 }
 void DressCard(Control control,bool leader,float w,float h){
  if(!assets.CardPolishEnabled)return;
  control.Chrome=AddChrome("CommonCardFrame",control.Button.transform,-3,-4,w+6,h+8,leader);control.Chrome.transform.SetAsFirstSibling();
  // Thin separator frames the portrait without covering the face or changing its source crop.
  Box("PortraitSill",control.Button.transform,3,leader?119:84,w-6,1,new Color(.42f,.39f,.3f,.65f));
  if(!leader){var socket=Rect("GoldSkillSocket",control.Button.transform,7,115,w-14,46).gameObject.AddComponent<CombatConceptPanel>();socket.Gold=true;socket.raycastTarget=false;}
 }
 void RefreshCardChrome(Control c,string status,bool enabled,int charge,float seconds){
  
  float glow=!enabled?0:c.Group=="ultimate"?charge==100?.78f+.12f*Mathf.Sin(seconds*2.6f):0:.35f;
  c.Chrome?.SetState(status,enabled,glow);
  if(c.Portrait!=null)c.Portrait.color=status=="DOWN"?new Color(.43f,.48f,.5f):Color.white;
  if(c.AbilityIcon!=null)c.AbilityIcon.color=status=="DOWN"?new Color(.33f,.38f,.4f):enabled||status=="CASTING"?Color.white:new Color(.66f,.72f,.76f);
  if(c.AbilityIcon!=null){var glyph=c.AbilityIcon.GetComponentInChildren<CombatConceptGlyph>();if(glyph!=null)glyph.color=status=="DOWN"?new Color(.34f,.33f,.29f):enabled||status=="CASTING"?new Color(1,.84f,.43f):new Color(.64f,.52f,.29f);}
 }
}
}
