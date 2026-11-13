using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
 public sealed partial class ApprovedCombatView {
  OperatorInformationController information;OperatorInformationView informationView;long informationIncarnation;
  public OperatorInformationFrame InformationFrame=>information?.Frame;
  readonly Dictionary<string,long> actorIncarnations=new Dictionary<string,long>(StringComparer.Ordinal);
  void EnsureInformation(){
   if(information!=null)return;
   informationView=gameObject.AddComponent<OperatorInformationView>();informationView.Configure(root,font);
   BindEmptyInformationSurface(exteriorViewport.gameObject);
   BindEmptyInformationSurface(cabinLayer.gameObject);
   // This legacy scene supplies committed actors but no current-run equipment or spatial range contract.
   information=new OperatorInformationController((target,token)=>{
    if(!driver.TryGetCommittedRevision(out var revision))throw new InvalidOperationException("Combat committed source unavailable.");
    var actor=((JArray)driver.ReadDisplayState()["actors"]).FirstOrDefault(a=>(string)a["id"]==target.ActorId);
    if(actor==null||(bool)actor["enemy"]||(string)actor["visual"]!=target.OperatorId)throw new InvalidOperationException("Selected actor no longer current.");
    return Task.FromResult(new OperatorInformationSnapshot(target,revision,null,null,"장착 정보를 확인할 수 없습니다.",new OperatorHealthInformation((int)actor["hp"],(int)actor["maxHp"],(int?)actor["shield"])));
   });
   BindInformation();
  }
  void BindEmptyInformationSurface(GameObject surface){
   var image=surface.GetComponent<Graphic>();if(image==null){var transparent=surface.AddComponent<Image>();transparent.color=Color.clear;image=transparent;}image.raycastTarget=true;
   var button=surface.AddComponent<CombatInputButton>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
   button.onClick.AddListener(()=>{if(isActiveAndEnabled&&!FullCutinVisible&&!driver.RecoveryRequired)information?.ClickEmptyWorld();});
  }
  bool ConsumeInformationWorldClick()=>information!=null&&information.ConsumeWorldClick();
  void BindInformation(){if(information==null)return;information.Bind(frame=>{if(frame.Error!=null)Debug.LogError("Combat information memory binding: "+frame.Error.GetType().Name);informationView.Present(frame);if(displayBound&&reinaDisplayState!=null)RefreshOperatorCards(reinaDisplayState);});informationView.Bind(information.OpenDetail,information.CloseDetail,information.Back);}
  void BindActorInformation(string id,Unit unit){
   if(unit.Enemy)return;EnsureInformation();actorIncarnations[id]=++informationIncarnation;
   // Cards and world actors share selection; the separate skill button retains its command.
   var hit=unit.Root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
   var input=unit.Root.gameObject.AddComponent<CombatInputButton>();input.targetGraphic=hit;input.transition=Selectable.Transition.None;
   input.onClick.AddListener(()=>SelectOperatorActor(id));
  }
  void SelectOperatorActor(string id){
   if(!isActiveAndEnabled||driver.RecoveryRequired||FullCutinVisible||driver.Outcome!=BasicCombatOutcome.Running||!units.TryGetValue(id,out var unit)||unit.Enemy||!unit.Root.gameObject.activeInHierarchy||!actorIncarnations.TryGetValue(id,out var incarnation))return;
   _=information.ClickActorAsync(new OperatorInformationTarget("view:"+GetInstanceID(),id,unit.Visual,incarnation));
  }
  void ValidateInformation(JArray actors){
   informationView?.SetSuppressed(FullCutinVisible);
   var target=information?.Frame.Target;if(target==null)return;
   if(!driver.TryGetCommittedRevision(out var revision)||driver.Outcome!=BasicCombatOutcome.Running||!actors.Any(a=>(string)a["id"]==target.ActorId)||!units.TryGetValue(target.ActorId,out var unit)||(string)actors.First(a=>(string)a["id"]==target.ActorId)["visual"]!=target.OperatorId){information.Clear();return;}
   if(information.Frame.Snapshot is OperatorInformationSnapshot snapshot&&snapshot.Revision!=revision)_=information.RefreshAsync();
  }
  bool wasInputPaused;
  void InputDeviceChanged(UnityEngine.InputSystem.InputDevice device,UnityEngine.InputSystem.InputDeviceChange change){if(change==UnityEngine.InputSystem.InputDeviceChange.Disconnected||change==UnityEngine.InputSystem.InputDeviceChange.Removed||change==UnityEngine.InputSystem.InputDeviceChange.Disabled)CancelPendingInput();}
  void OnDisable(){UnbindDisplayChanges();UnityEngine.InputSystem.InputSystem.onDeviceChange-=InputDeviceChanged;CancelPendingInput();SetThreatDetails(false);information?.Unbind();informationView?.Unbind();}
  void OnEnable(){UnityEngine.InputSystem.InputSystem.onDeviceChange+=InputDeviceChanged;if(information!=null)BindInformation();if(displayBound){if(driver!=null&&!driver.IsRetired)dangerFeedback.Seed(driver.HudState);BindDisplayChanges();OwnerDisplayChanged();}}
  public void ReleaseOwnedPresentationResources(){UnbindDisplayChanges();displayBound=false;try{information?.Dispose();information=null;}finally{try{ReleaseOrdinaryRoof();}finally{ReleasePresentationSprites();if(presentationAudio!=null){presentationAudio.Stop();presentationAudio.clip=null;}abilityAudio=null;effectAudio=null;assets=null;font=null;}}}
  void OnDestroy()=>ReleaseOwnedPresentationResources();
 }
}
