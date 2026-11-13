using System;
using System.Linq;
namespace LastRail.Presentation.MissionShell {
 // Owned by the account/run consumer; opening or selecting a view never fetches data.
 public sealed class EquipmentInventoryController {
  readonly string accountId,runId;
  public EquipmentInventoryModel Snapshot {get;private set;}
  public string SelectedId {get;private set;}
  public EquipmentInventoryModel.Item Selected=>Snapshot?.Items.FirstOrDefault(i=>i.Id==SelectedId);
  public event Action Changed;
  public EquipmentInventoryController(string accountId,string runId){if(string.IsNullOrWhiteSpace(accountId)||string.IsNullOrWhiteSpace(runId))throw new ArgumentException("Equipment owner required");this.accountId=accountId;this.runId=runId;}
  public void Apply(EquipmentInventoryModel value){
   if(value==null||value.AccountId!=accountId||value.RunId!=runId)throw new ArgumentException("Equipment owner mismatch");
   if(Snapshot!=null){if(value.Revision<Snapshot.Revision)return;if(value.Revision==Snapshot.Revision){if(value.Signature!=Snapshot.Signature)throw new ArgumentException("Conflicting equipment revision");return;}}
   Snapshot=value;if(Selected==null)SelectedId=null;Changed?.Invoke();
  }
  public void Select(string id){if(Snapshot==null||!Snapshot.Items.Any(i=>i.Id==id))return;if(SelectedId==id)return;SelectedId=id;Changed?.Invoke();}
  public void Back(){if(SelectedId==null)return;SelectedId=null;Changed?.Invoke();}
  public void Invalidate(){Snapshot=null;SelectedId=null;Changed?.Invoke();}
 }
}
