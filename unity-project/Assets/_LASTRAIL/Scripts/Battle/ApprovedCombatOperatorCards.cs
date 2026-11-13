using System;
using LastRail.Presentation.MissionShell;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 sealed class OperatorCard {public string Actor,Ability;public Button Button;public CombatMeter Hp,Charge;public TMP_Text Percent,Selection;public RawImage Portrait,Illustration,DCharge;public RawImage DWash,DFlash,DSelected;public Color DTint;public float DWidth;public double DReadyAt;public bool DReadySeen,DReady;public CombatConceptGlyph Icon;public CombatReadyFrame Rim;public Image ReadyWash;public bool WasReady;public float ReadyAt;}
 static readonly Color ReinaAttackColor=new Color32(255,77,79,255);
 readonly List<OperatorCard> operatorCards=new List<OperatorCard>();readonly Dictionary<string,CombatMeter> worldCooldown=new Dictionary<string,CombatMeter>();Button ultimateAutoButton;Graphic autoThumb;TMP_Text autoText;
 void BuildOperatorCards(JObject state){
  var frameTexture=SecondArt("operator-card-rim-d-v01");
  bool legacyGeneral=!(bool)state["generalAuto"];foreach(var row in (JArray)state["ultimates"]){int index=operatorCards.Count;string actor=(string)row["actor"],ability=(string)row["ability"];var card=Rect("OperatorCard_"+actor,root,24+136*index,844,128,230);Box("CardPlate",card,2,2,124,226,new Color(.047f,.071f,.082f));
   string visual=(string)((JArray)state["actors"]).Single(a=>(string)a["id"]==actor&&!(bool)a["train"]&&!(bool)a["enemy"])["visual"];var portrait=Portrait(card,visual,4,3,120,91);Box("OperatorNameplate",card,4,94,120,21,new Color(.043f,.067f,.078f));Text("OperatorName",card,OperatorCardName(visual),2,94,124,21,15);
   var portraitArt=visual!="op_kyle"&&assets.OperatorFrames?.Find(visual)?.Portrait==null?SecondArt(visual.Substring(3)+"-v02"):null;if(portraitArt!=null){portrait.texture=portraitArt;portrait.uvRect=OperatorVisualCatalog.Crop(portraitArt,120,91);}
   var select=Rect("SelectOperator_"+actor,card,0,0,128,128).gameObject.AddComponent<CombatInputButton>();var hit=select.gameObject.AddComponent<Image>();hit.color=Color.clear;select.targetGraphic=hit;select.transition=Selectable.Transition.None;select.onClick.AddListener(()=>SelectOperatorActor(actor));var selected=Text("SelectedOperator",card,"",2,0,124,20,13);selected.raycastTarget=false;var hp=Meter("OperatorHp",card,5,117,118,8,new Color(.3f,.95f,1));bool isReina=visual=="op_reina";var charge=Meter("UltimateCharge",card,5,133,118,13,isReina?ReinaAttackColor:new Color(1,.71f,.17f));charge.Gold=!isReina;Box("ChargeNumberBackplate",card,82,132,41,15,new Color(.008f,.015f,.019f,.88f));var percent=Text("ChargePercent",card,"0%",66,129,55,20,14);percent.alignment=TextAlignmentOptions.Right;percent.raycastTarget=false;
   var b=Button("Ultimate_"+actor,"",0,0,120,74,()=>UseOperatorUltimate(actor,ability));b.transform.SetParent(card,false);var br=b.GetComponent<RectTransform>();br.anchoredPosition=new Vector2(4,-152);b.GetComponent<Image>().color=Color.clear;b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
   var socket=Rect("UltimateSocket",b.transform,0,0,120,74).gameObject.AddComponent<CombatConceptPanel>();socket.Gold=true;socket.raycastTarget=false;var icon=Rect("UltimateIcon",b.transform,28,5,64,64).gameObject.AddComponent<CombatConceptGlyph>();icon.Symbol=10+Array.IndexOf(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"},visual);icon.raycastTarget=false;
   if(legacyGeneral){var gb=Button("General_"+actor,CombatSkillNames.Get((string)((JArray)state["skills"]).Single(r=>(string)r["actor"]==actor)["ability"]),24+136*index+4,996,120,29,()=>UseLegacyGeneral(actor));legacyGeneralButtons.Add(actor,gb);br.anchoredPosition=new Vector2(4,-185);br.sizeDelta=new Vector2(120,41);socket.rectTransform.sizeDelta=br.sizeDelta;icon.rectTransform.anchoredPosition=new Vector2(42,-2);icon.rectTransform.sizeDelta=new Vector2(36,36);}
   RawImage illustration=null;var art=SecondArt("ultimate_"+visual.Substring(3)+(visual=="op_reina"?"-v02":"-v01"));if(art!=null){icon.gameObject.SetActive(false);illustration=Raw("UltimateIllustration",b.transform,art,6,6,108,62);FitArt(illustration.rectTransform,art,60,legacyGeneral?20.5f:37,108,legacyGeneral?29:62);}
   var rim=Rect("UltimateReadyRim",b.transform,-1,-1,122,legacyGeneral?43:76).gameObject.AddComponent<CombatReadyFrame>();rim.raycastTarget=false;rim.WidthScale=.45f;
   Raw("CardFrame",card,frameTexture,-3,-4,134,238);
   var wash=isReina?Box("ReadyWash",card,1,1,126,228,Color.clear):null;
   operatorCards.Add(new OperatorCard{ReadyWash=wash,Actor=actor,Ability=ability,Selection=selected,Button=b,Portrait=portrait,Hp=hp,Charge=charge,Percent=percent,Icon=icon,Illustration=illustration,Rim=rim});
  }
 }
 void BuildUltimateAuto(){if(operatorCards.Count==0)return;wave.rectTransform.sizeDelta=new Vector2(265,30);waveTrack.rectTransform.sizeDelta=new Vector2(260,9);waveTrack.rectTransform.anchoredPosition=new Vector2(26,-43);ultimateAutoButton=Button("UltimateAuto","AUTO",338,11,119,43,()=>{driver.SetUltimateAuto(!(bool)driver.ReadDisplayState()["ultimateAuto"]);});ultimateAutoButton.GetComponent<Image>().color=new Color(.025f,.043f,.047f);var housing=Rect("AutoHousing",ultimateAutoButton.transform,0,0,119,43).gameObject.AddComponent<CombatConceptPanel>();housing.Metal=true;housing.raycastTarget=false;housing.transform.SetAsFirstSibling();autoText=ultimateAutoButton.GetComponentInChildren<TMP_Text>();autoText.fontSize=18;autoText.raycastTarget=false;autoThumb=Rect("AutoThumb",ultimateAutoButton.transform,84,12.5f,18,18).gameObject.AddComponent<CombatAutoThumb>();autoThumb.raycastTarget=false;}
 // Korean card labels requested for the current five; actor identity and shared catalog stay canonical.
 static string OperatorCardName(string actor){switch(actor){case "op_kyle":return "카일";case "op_reina":return "레이나";case "op_eve":return "이브";case "op_sera":return "세라";case "op_mira":return "미라";case "op_nyx":return "닉스";default:return OperatorVisualCatalog.Label(actor);}}
 readonly Dictionary<string,Button> legacyGeneralButtons=new Dictionary<string,Button>();
 void UseLegacyGeneral(string actor){var row=((JArray)driver.ReadDisplayState()["skills"]).Single(r=>(string)r["actor"]==actor);if(!driver.RecoveryRequired&&(bool)row["enabled"])driver.RequestSkill(Guid.NewGuid().ToString("N"),actor,(string)row["ability"]);}
 void UseOperatorUltimate(string actor,string ability){var state=driver.ReadDisplayState();var row=((JArray)state["ultimates"]).Single(r=>(string)r["actor"]==actor&&(string)r["ability"]==ability);if(driver.RecoveryRequired||!(bool)row["enabled"])return;driver.RequestSkill(Guid.NewGuid().ToString("N"),actor,ability);}
 void RefreshOperatorCards(JObject state){if(operatorCards.Count==0)return;bool on=(bool)state["ultimateAuto"];ultimateAutoButton.interactable=!driver.RecoveryRequired&&!(bool)state["paused"]&&(string)state["outcome"]=="Running";float autoHeight=((RectTransform)ultimateAutoButton.transform).rect.height;autoThumb.rectTransform.anchoredPosition=new Vector2(((RectTransform)ultimateAutoButton.transform).rect.width-24,-(autoHeight-autoThumb.rectTransform.sizeDelta.y)*.5f);autoThumb.color=on?new Color(.25f,.88f,1):new Color(.16f,.21f,.23f);autoText.rectTransform.anchoredPosition=new Vector2(8,-(autoHeight-37)*.5f);autoText.rectTransform.sizeDelta=new Vector2(((RectTransform)ultimateAutoButton.transform).rect.width-36,37);
  foreach(var card in operatorCards){var row=((JArray)state["ultimates"]).Single(r=>(string)r["actor"]==card.Actor&&(string)r["ability"]==card.Ability);var actor=((JArray)state["actors"]).Single(r=>(string)r["id"]==card.Actor);bool enabled=!driver.RecoveryRequired&&(bool)row["enabled"],down=(int)actor["hp"]==0;card.Selection.text=information?.Frame.Target?.ActorId==card.Actor?"선택":"";card.Button.interactable=enabled;card.Hp.SetHealth((int)actor["hp"],(int)actor["maxHp"],(int)actor["shield"]);int? charge=(int?)row["charge"];card.Charge.gameObject.SetActive(charge.HasValue);if(charge.HasValue)card.Charge.SetValue(charge.Value/100f);card.Percent.text=charge.HasValue?charge.Value+"%":"미확인";card.Portrait.color=down?new Color(.4f,.43f,.45f):Color.white;if(card.Illustration!=null)card.Illustration.color=down?new Color(.2f,.2f,.2f):enabled?new Color(1,.94f,.82f):new Color(.38f,.38f,.38f);card.Icon.Halo=enabled;card.Icon.color=down?new Color(.3f,.28f,.22f):enabled?new Color(1,.9f,.58f):new Color(.74f,.6f,.33f);bool ready=enabled&&(card.ReadyWash==null||charge==100);if(ready&&!card.WasReady)card.ReadyAt=time;card.WasReady=ready;
   if(card.DCharge!=null){
    float amount=charge.HasValue?Mathf.Clamp01(charge.Value/100f):0;
    card.DCharge.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,card.DWidth*amount);
    card.DCharge.uvRect=new Rect(0,0,amount,1);
    RefreshDCardLight(card,charge,down,information?.Frame.Target?.ActorId==card.Actor);
   }
   else if(card.ReadyWash!=null){
    float age=time-card.ReadyAt;
    var tint=card.DCharge!=null?card.DTint:ReinaAttackColor;
    tint.a=ready?.25f+.10f*Mathf.Cos(age*13f)+.23f*Mathf.Clamp01(1-age/.16f):0;
    card.ReadyWash.color=tint;
   }
   if(card.DCharge==null){card.Rim.SetLight(ready?Mathf.Clamp01((time-card.ReadyAt)/.35f):0,1);card.Rim.color=card.ReadyWash!=null?ReinaAttackColor:new Color(1,.68f,.13f,.8f+.12f*Mathf.Sin(time*2.4f));}
  }
  foreach(var row in (JArray)state["skills"]){string actor=(string)row["actor"];if(legacyGeneralButtons.TryGetValue(actor,out var legacyButton))legacyButton.interactable=!driver.RecoveryRequired&&(bool)row["enabled"];if(!units.TryGetValue(actor,out var u)||u.Enemy||u.Vehicle)continue;if(!worldCooldown.TryGetValue(actor,out var meter)){meter=Meter("GeneralCooldown_"+actor,u.Hp.transform.parent,0,0,105,6,new Color(.22f,.56f,.95f));worldCooldown.Add(actor,meter);}var hpRect=u.Hp.rectTransform;meter.rectTransform.anchoredPosition=hpRect.anchoredPosition+new Vector2(0,-hpRect.sizeDelta.y-2);meter.rectTransform.sizeDelta=new Vector2(hpRect.sizeDelta.x,6);meter.transform.SetAsLastSibling();long? remaining=(long?)row["cooldownMs"];meter.gameObject.SetActive(remaining.HasValue);if(remaining.HasValue)meter.SetValue(1-remaining.Value/(float)Math.Max(1,(long)row["cooldownTotalMs"]));meter.color=(int)row["hp"]==0?new Color(.15f,.22f,.29f):new Color(.22f,.56f,.95f);}
 }
 void UpdateCastPortrait(string actor){string visual=(string)((JArray)driver.ReadDisplayState()["actors"]).Single(a=>(string)a["id"]==actor)["visual"];var replacement=Portrait(cutin.transform,visual,8,6,112,120);replacement.name="ActualCasterPortrait";if(cutPortrait!=null)DisposeVisual(cutPortrait.gameObject);cutPortrait=replacement;}
 bool UltimateAbility(JObject state,string ability)=>state["ultimates"] is JArray rows?rows.Any(r=>(string)r["ability"]==ability):(string)state["ultimate"]?["ability"]==ability;
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatAutoThumb:MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();var r=rectTransform.rect;var center=r.center;float radius=Mathf.Min(r.width,r.height)*.5f;Disc(mesh,center,radius,new Color(.76f,.84f,.87f),new Color(.33f,.42f,.46f));Disc(mesh,center,radius-1.5f,Color.Lerp(color,Color.white,.18f),color);}
 static void Disc(VertexHelper mesh,Vector2 p,float radius,Color top,Color bottom){int n=mesh.currentVertCount;mesh.AddVert(p,Color.Lerp(top,bottom,.5f),Vector2.zero);for(int i=0;i<40;i++){float a=i*Mathf.PI/20;mesh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,Color.Lerp(bottom,top,(Mathf.Sin(a)+1)*.5f),Vector2.zero);}for(int i=0;i<40;i++)mesh.AddTriangle(n,n+1+i,n+1+(i+1)%40);}
}

}
