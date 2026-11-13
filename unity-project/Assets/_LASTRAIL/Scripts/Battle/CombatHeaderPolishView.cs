using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 CombatWaveTrack waveTrack;
 void DressHeader(){
  var header=root.Find("Header");header.GetComponent<Image>().color=new Color(.01f,.022f,.028f,.9f);root.Find("Title").gameObject.SetActive(false);
  Box("HeaderUnderlight",root,0,64,720,1,new Color(.2f,.52f,.6f,.5f));
  wave.fontSize=22;wave.rectTransform.anchoredPosition=new Vector2(22,-5);wave.rectTransform.sizeDelta=new Vector2(408,30);
  waveTrack=Rect("WaveProgress",root,42,43,369,9).gameObject.AddComponent<CombatWaveTrack>();waveTrack.raycastTarget=false;
  foreach(var button in new[]{speed,pause}){var r=button.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(button==speed?474:609,-10);r.sizeDelta=new Vector2(button==speed?118:92,44);button.GetComponent<Image>().color=new Color(0,0,0,0);var bevel=Rect("ControlHousing",button.transform,0,0,r.rect.width,44).gameObject.AddComponent<CombatConceptPanel>();bevel.Metal=true;bevel.raycastTarget=false;bevel.transform.SetAsFirstSibling();var text=button.GetComponentInChildren<TMP_Text>();text.fontSize=22;text.rectTransform.anchoredPosition=new Vector2(button==speed?35:5,-3);text.rectTransform.sizeDelta=new Vector2(button==speed?77:82,38);}
  var arrows=Text("SpeedChevrons",speed.transform,"»",9,3,30,38,33);arrows.color=new Color(.32f,.9f,1);arrows.fontStyle=FontStyles.Bold;
  var panel=Rect("HullIntegrityHousing",root,0,341,535,49).gameObject.AddComponent<CombatConceptPanel>();panel.Metal=true;panel.Hull=true;panel.raycastTarget=false;panel.transform.SetSiblingIndex(trainHp.transform.GetSiblingIndex());
  var plate=Rect("HullNameplate",root,538,341,182,49).gameObject.AddComponent<CombatConceptPanel>();plate.Metal=true;plate.Hull=true;plate.raycastTarget=false;plate.transform.SetSiblingIndex(panel.transform.GetSiblingIndex()+1);
  var maker=Text("HullNameplateStencil",root,"A SAFE TOMORROW\nTHROUGH A LONGER RAIL",554,354,150,23,8);maker.color=new Color(.36f,.39f,.39f);maker.raycastTarget=false;maker.characterSpacing=1;
  root.Find("TrainLabel").SetAsLastSibling();var trainLabel=root.Find("TrainLabel").GetComponent<TMP_Text>();trainLabel.fontSize=12;trainLabel.rectTransform.anchoredPosition=new Vector2(69,-350);trainLabel.rectTransform.sizeDelta=new Vector2(78,32);
  var trainGlyph=Rect("TrainPictogram",root,21,350,35,30).gameObject.AddComponent<CombatConceptGlyph>();trainGlyph.Symbol=5;trainGlyph.Halo=false;trainGlyph.color=new Color(.64f,.8f,.86f);trainGlyph.raycastTarget=false;
  trainHp.rectTransform.anchoredPosition=new Vector2(149,-360);trainHp.rectTransform.sizeDelta=new Vector2(266,11);trainHp.RoundedEnds=true;
  trainText.rectTransform.anchoredPosition=new Vector2(422,-350);trainText.rectTransform.sizeDelta=new Vector2(97,32);trainText.fontSize=14;
  // Small rail-company stencil, attached to the original cabin so skin composition remains intact.
  var stencil=Text("RailCompanyStencil",cabinLayer.transform,"L.R.  /  01",337,103,73,16,10);stencil.color=new Color(.65f,.59f,.43f,.55f);stencil.characterSpacing=2;
 }
 void RefreshHeaderPolish(int current,int total,bool victory,float hp){
  waveTrack?.SetProgress(current,total,victory);
  trainHp.color=hp<=.2f?new Color(1,.38f,.28f):hp<=.4f?new Color(1,.73f,.34f):new Color(.38f,.91f,.97f);
  trainText.color=hp<=.2f?new Color(1,.65f,.54f):new Color(.9f,.95f,.96f);
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatWaveTrack:MaskableGraphic {
 int current=1,count=1;bool complete;
 public void SetProgress(int wave,int total,bool finished){total=Mathf.Max(1,total);wave=Mathf.Clamp(wave,1,total);if(wave==current&&total==count&&complete==finished)return;current=wave;count=total;complete=finished;SetVerticesDirty();}
 void Quad(VertexHelper m,Rect r,Color tint){int n=m.currentVertCount;m.AddVert(new Vector2(r.x,r.y),tint,Vector2.zero);m.AddVert(new Vector2(r.xMax,r.y),tint,Vector2.zero);m.AddVert(new Vector2(r.xMax,r.yMax),tint,Vector2.zero);m.AddVert(new Vector2(r.x,r.yMax),tint,Vector2.zero);m.AddTriangle(n,n+1,n+2);m.AddTriangle(n,n+2,n+3);}
 void Dot(VertexHelper m,Vector2 p,float radius,Color c){int n=m.currentVertCount;m.AddVert(p,c,Vector2.zero);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;m.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);}for(int i=0;i<12;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%12);}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();var r=GetPixelAdjustedRect();float step=(r.width-8)/Mathf.Max(1,count-1);Quad(m,new Rect(r.x+4,r.center.y-.5f,r.width-8,1f),new Color(.15f,.20f,.21f));for(int i=0;i<count;i++){var p=new Vector2(r.x+4+i*step,r.center.y);var tint=i<current-1||complete?new Color(.43f,.47f,.48f):i==current-1?new Color(.35f,.93f,1):new Color(.35f,.41f,.44f);if(i==current-1&&!complete)Dot(m,p,4,new Color(.015f,.21f,.25f));Dot(m,p,2.8f,new Color(.055f,.08f,.09f));Dot(m,p,2.0f,tint);if(i==current-1&&!complete)Dot(m,p,1.2f,new Color(.64f,.97f,1));}}
}
}
