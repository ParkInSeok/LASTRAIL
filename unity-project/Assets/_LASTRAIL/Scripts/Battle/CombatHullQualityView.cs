using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 void ApplyHullQuality(){
  foreach(string name in new[]{"HullIntegrityHousing","HullNameplate"}){var target=(RectTransform)root.Find(name);target.GetComponent<CombatConceptPanel>().enabled=false;var frame=Rect("MachinedHullFrame",target,0,0,target.rect.width,target.rect.height).gameObject.AddComponent<CombatTitleBarFrame>();frame.raycastTarget=false;}
  var caption=root.Find("HullNameplateStencil").GetComponent<TMP_Text>();caption.rectTransform.sizeDelta=new Vector2(117,23);caption.color=new Color(.46f,.49f,.49f);caption.fontSize=7.5f;
  var wing=Rect("HullNameplateWing",root,682,356,26,18).gameObject.AddComponent<CombatHullWing>();wing.Texture=SecondArt("title_emblem-v01");wing.color=new Color(.78f,.83f,.82f);wing.raycastTarget=false;
  trainHp.HullNeon=true;
  trainText.richText=true;trainText.fontSize=13;trainText.enableAutoSizing=true;trainText.fontSizeMin=10;trainText.fontSizeMax=13;
  root.Find("TrainPictogram").GetComponent<Graphic>().color=new Color(.74f,.82f,.83f);
  root.Find("TrainLabel").GetComponent<TMP_Text>().color=new Color(.78f,.83f,.84f);
 }
 void RefreshHullText(int hp,int maximum){string current=hp<=maximum*.2f?"FFA68A":hp<=maximum*.4f?"FFBA57":"61EDF0";trainText.color=Color.white;trainText.text="<color=#"+current+">"+hp+"</color><color=#858F91> / "+maximum+"</color>";}
}
// Native alpha-preserving geometry: the original wing boundary excludes the adjacent L.
// Coordinates refer to the preserved 2172x724 original, independent of importer downscale.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatHullWing:MaskableGraphic {
 public Texture2D Texture;public override Texture mainTexture=>Texture!=null?Texture:base.mainTexture;
 static readonly Vector2[] Outline={new Vector2(18,148),new Vector2(20,144),new Vector2(26,140),new Vector2(681,140),new Vector2(687,144),new Vector2(688,148),new Vector2(688,152),new Vector2(685,164),new Vector2(673,200),new Vector2(670,208),new Vector2(668,212),new Vector2(613,316),new Vector2(567,388),new Vector2(516,452),new Vector2(439,536),new Vector2(435,540),new Vector2(430,544),new Vector2(361,596),new Vector2(355,600),new Vector2(350,600),new Vector2(281,548),new Vector2(271,540),new Vector2(267,536),new Vector2(190,452),new Vector2(143,392),new Vector2(140,388),new Vector2(97,320),new Vector2(45,228),new Vector2(43,224),new Vector2(37,208),new Vector2(30,188),new Vector2(19,156)};
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();if(Texture==null)return;var r=rectTransform.rect;var center=Vector2.zero;foreach(var p in Outline)center+=p;center/=Outline.Length;
  void Vertex(Vector2 p){mesh.AddVert(new Vector2(r.x+(p.x-18)/670*r.width,r.yMax-(p.y-140)/460*r.height),color,new Vector2(p.x/2172,1-p.y/724));}
  Vertex(center);foreach(var p in Outline)Vertex(p);for(int i=0;i<Outline.Length;i++)mesh.AddTriangle(0,i+1,(i+1)%Outline.Length+1);
 }
}
}
