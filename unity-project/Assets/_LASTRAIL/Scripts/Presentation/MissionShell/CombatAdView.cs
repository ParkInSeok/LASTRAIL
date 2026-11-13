using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
namespace LastRail.Presentation.MissionShell {
 // Combat-specific display adapter; shared View owns no settlement or provider logic.
 public sealed class CombatAdView:MonoBehaviour {
  SharedRoulettePopup popup;
  public static CombatAdView Create(Transform parent,TMP_FontAsset font){var r=AuthFormVisuals.Rect("CombatRoulette",parent);AuthFormVisuals.Place(r,0,0,1600,900);var v=r.gameObject.AddComponent<CombatAdView>();v.popup=SharedRoulettePopup.Create(r,font,"CombatAd");return v;}
  public void Bind(Action main,Action refresh,Action basic,Action close)=>popup.Bind(main,refresh,basic,close);
  public void Unbind(){if(popup!=null)popup.Unbind();}
  void OnDisable(){Unbind();}void OnDestroy(){Unbind();}
  public void Render(CombatAdIntent intent,string message,bool busy,bool provider,bool pending,bool canBasic,bool canPresent=false,string mainLabel=null,bool? canMain=null){
   AuthFormVisuals.Place((RectTransform)transform,0,0,941,Mathf.Max(1672,((RectTransform)transform.parent).rect.height));
   bool paid=intent?.Status=="CLAIMED"||intent?.Status=="REWARD_CONSUMED";
   var result=intent?.Result;
   string label=intent?.Terminal==true?"확인":intent?.CanSpin==true?"룰렛 돌리기":canPresent?"광고 시청":pending?"다시 확인":"광고 시청";
   string notice=message;
   if(result!=null||intent?.CanSpin==true&&!busy)notice="";else if(intent?.Status=="REWARD_CONSUMED")notice="기본 보상 수령 내역은 작전 결과에서 확인하세요.";
   else if(string.IsNullOrEmpty(notice))notice=busy?"결과 확인 중…":intent?.Status=="AD_PENDING"?"광고 확인 중…":!provider&&!pending?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"";
   if(mainLabel!=null)label=mainLabel;
   bool separateRefresh=pending&&intent?.Terminal!=true&&label!="다시 확인";
   popup.Render(result,intent?.BaseLines,notice,label,canMain??(paid||intent?.Terminal==true||pending||provider),busy,paid,separateRefresh,canBasic);
  }
 }
}
