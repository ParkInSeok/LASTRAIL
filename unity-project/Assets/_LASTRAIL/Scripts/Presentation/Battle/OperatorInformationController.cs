using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace LastRail.Presentation.Battle {
 // Identity comes from the composition owner; generation identifies an actor incarnation.
 public sealed class OperatorInformationTarget : IEquatable<OperatorInformationTarget> {
  public string Scope {get;} public string ActorId {get;} public string OperatorId {get;} public long Generation {get;}
  public OperatorInformationTarget(string scope,string actorId,string operatorId,long generation) {
   if(string.IsNullOrWhiteSpace(scope)||string.IsNullOrWhiteSpace(actorId)||string.IsNullOrWhiteSpace(operatorId)||generation<0)throw new ArgumentException("Information target identity required.");
   Scope=scope;ActorId=actorId;OperatorId=operatorId;Generation=generation;
  }
  public bool Equals(OperatorInformationTarget other)=>other!=null&&Scope==other.Scope&&ActorId==other.ActorId&&OperatorId==other.OperatorId&&Generation==other.Generation;
  public override bool Equals(object value)=>Equals(value as OperatorInformationTarget);
  public override int GetHashCode()=>Scope.GetHashCode()^ActorId.GetHashCode()^OperatorId.GetHashCode()^Generation.GetHashCode();
 }
 public sealed class OperatorEquipmentInformation {
  public string InstanceId {get;} public long Revision {get;} public long EffectRevision {get;} public string Name {get;} public int Star {get;}
  public string IconKey {get;} public string Description {get;}
  public OperatorEquipmentInformation(string instanceId,long revision,string name,int star,string iconKey,string description,long effectRevision=0) {
   if(string.IsNullOrWhiteSpace(instanceId)||revision<0||effectRevision<0||star<1||star>5)throw new ArgumentException("Confirmed equipment identity/revision/star required.");
   InstanceId=instanceId;Revision=revision;EffectRevision=effectRevision;Name=name;Star=star;IconKey=iconKey;Description=description??"장비 정보를 확인할 수 없습니다.";
  }
 }
 public sealed class OperatorHealthInformation {
  public int CurrentHp {get;} public int MaxHp {get;} public int? Shield {get;}
  public OperatorHealthInformation(int currentHp,int maxHp,int? shield){
   if(maxHp<=0||currentHp<0||currentHp>maxHp||shield<0)throw new ArgumentException("Confirmed HP and independent shield required.");
   CurrentHp=currentHp;MaxHp=maxHp;Shield=shield;
  }
 }
 public sealed class OperatorInformationSnapshot {
  readonly OperatorEquipmentInformation[] slots;
  public OperatorInformationTarget Target {get;} public long Revision {get;} public bool EquipmentKnown=>slots!=null;
  public string RangeNotice {get;} public string SourceNotice {get;}
  public OperatorHealthInformation Health {get;}
  public OperatorEquipmentInformation Slot(int index){if(index<0||index>2)throw new ArgumentOutOfRangeException(nameof(index));return slots?[index];}
  public OperatorInformationSnapshot(OperatorInformationTarget target,long revision,OperatorEquipmentInformation[] confirmedSlots,string rangeNotice,string sourceNotice,OperatorHealthInformation health=null) {
   if(target==null||revision<0||confirmedSlots!=null&&(confirmedSlots.Length!=3||confirmedSlots.Where(x=>x!=null).Select(x=>x.InstanceId).Distinct().Count()!=confirmedSlots.Count(x=>x!=null)))throw new ArgumentException("Exact three confirmed slots or unknown required.");
   Target=target;Revision=revision;slots=confirmedSlots?.ToArray();RangeNotice=rangeNotice??"기본공격 범위를 확인할 수 없습니다.";SourceNotice=sourceNotice??"";Health=health;
  }
 }
 public sealed class OperatorInformationFrame {
  public OperatorInformationTarget Target {get;} public OperatorInformationSnapshot Snapshot {get;} public string Status {get;} public int DetailSlot {get;}
  public Exception Error {get;} public bool Visible=>Target!=null;
  internal OperatorInformationFrame(OperatorInformationTarget target,OperatorInformationSnapshot snapshot,string status,int detailSlot,Exception error){Target=target;Snapshot=snapshot;Status=status;DetailSlot=detailSlot;Error=error;}
 }
 // UI-thread presentation owner. Its only source port is a read, with no combat/hold/economy commands.
 public sealed class OperatorInformationController : IDisposable {
  readonly Func<OperatorInformationTarget,CancellationToken,Task<OperatorInformationSnapshot>> read;
  Action<OperatorInformationFrame> present;CancellationTokenSource pending;long requestGeneration;bool disposed;
  readonly Dictionary<string,(long Instance,long Effect)> revisionFloors=new Dictionary<string,(long,long)>(StringComparer.Ordinal);
  OperatorInformationTarget target;OperatorInformationSnapshot snapshot,accepted;int detail=-1;string status="CLOSED";Exception error;
  public OperatorInformationFrame Frame=>new OperatorInformationFrame(target,snapshot,status,detail,error);
  public OperatorInformationController(Func<OperatorInformationTarget,CancellationToken,Task<OperatorInformationSnapshot>> read){this.read=read??throw new ArgumentNullException(nameof(read));}
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(OperatorInformationController));}
  public void Bind(Action<OperatorInformationFrame> show){Alive();Unbind();present=show??throw new ArgumentNullException(nameof(show));Publish();}
  void Cancel(){requestGeneration++;var old=pending;pending=null;old?.Cancel();old?.Dispose();}
  public void Unbind(){Cancel();target=null;snapshot=null;accepted=null;revisionFloors.Clear();detail=-1;status="CLOSED";error=null;var old=present;present=null;old?.Invoke(Frame);}
  public Task SelectAsync(OperatorInformationTarget selected){Alive();if(selected==null)throw new ArgumentNullException(nameof(selected));if(present==null)return Task.CompletedTask;if(selected.Equals(target))return Task.CompletedTask;Cancel();target=selected;snapshot=null;accepted=null;revisionFloors.Clear();detail=-1;return RefreshAsync();}
  public async Task RefreshAsync(){
   Alive();if(present==null||target==null)return;Cancel();var request=new CancellationTokenSource();pending=request;var token=request.Token;long stamp=requestGeneration;var selected=target;
   // Current composition ports read already committed memory synchronously. Task is
   // retained for cancellation/lifetime compatibility, never a loading UI contract.
   error=null;
   bool Current()=>!disposed&&present!=null&&requestGeneration==stamp&&ReferenceEquals(pending,request)&&selected.Equals(target);
   try {
    var reading=read(selected,token);
    if(!reading.IsCompleted){snapshot=null;detail=-1;status="CLOSED";Publish();}
    var next=await reading;if(!Current())return;
    if(next==null||!selected.Equals(next.Target)||accepted!=null&&next.Revision<accepted.Revision)throw new InvalidOperationException("Information source identity/revision conflict.");
    if(next.EquipmentKnown)for(int i=0;i<3;i++){
     var item=next.Slot(i);if(item!=null&&revisionFloors.TryGetValue(item.InstanceId,out var floor)&&(item.Revision<floor.Instance||item.EffectRevision<floor.Effect))throw new InvalidOperationException("Equipment instance/effect revision regressed.");
    }
    if(detail>=0){var before=snapshot?.Slot(detail);var after=next.Slot(detail);if(!next.EquipmentKnown||before==null||after==null||before.InstanceId!=after.InstanceId)detail=-1;}
    if(next.EquipmentKnown)for(int i=0;i<3;i++){var item=next.Slot(i);if(item!=null)revisionFloors[item.InstanceId]=(item.Revision,item.EffectRevision);}
    snapshot=accepted=next;status="READY";error=null;Publish();
   }catch(Exception failure){if(Current()){snapshot=null;detail=-1;status="FAILED";error=failure;Publish();}}
   finally{if(ReferenceEquals(pending,request)){pending=null;request.Dispose();}}
  }
  public void OpenDetail(int slot){Alive();if(slot<0||slot>2||present==null||status!="READY"||snapshot==null||!snapshot.EquipmentKnown||snapshot.Slot(slot)==null)return;detail=slot;Publish();}
  public void CloseDetail(){Alive();if(detail<0)return;detail=-1;Publish();}
  public void Back(){Alive();if(detail>=0)CloseDetail();else Clear();}
  // Returns true only when the first world click was consumed by the detail layer.
  public bool ConsumeWorldClick(){Alive();if(detail<0)return false;CloseDetail();return true;}
  public Task ClickActorAsync(OperatorInformationTarget selected){Alive();if(ConsumeWorldClick())return Task.CompletedTask;if(selected!=null&&selected.Equals(target)){Clear();return Task.CompletedTask;}return SelectAsync(selected);}
  public void ClickEmptyWorld(){Alive();if(!ConsumeWorldClick())Clear();}
  public void Clear(){Alive();Cancel();target=null;snapshot=null;accepted=null;revisionFloors.Clear();detail=-1;status="CLOSED";error=null;Publish();}
  void Publish()=>present?.Invoke(Frame);
  public void Dispose(){if(disposed)return;Unbind();disposed=true;}
 }
}
