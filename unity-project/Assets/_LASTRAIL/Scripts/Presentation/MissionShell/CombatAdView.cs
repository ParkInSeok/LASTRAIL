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
  public void Render(CombatAdIntent intent,string message,bool busy,bool provider,bool pending,bool canBasic,bool canPresent=false,string mainLabel=null,bool? canMain=null)
   =>Render(new CombatRouletteDisplayState(intent,message,busy,provider,pending,canBasic,canPresent,mainLabel,canMain));
  public void Render(CombatRouletteDisplayState state){
   AuthFormVisuals.Place((RectTransform)transform,0,0,941,Mathf.Max(1672,((RectTransform)transform.parent).rect.height));
   popup.Render(state.Result,state.Lines,state.Notice,state.Label,state.Enabled,state.Busy,state.Paid,state.SeparateRefresh,state.CanBasic);
  }
 }
}
