using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
namespace LastRail.Presentation.MissionShell {
 public sealed partial class AccountScreenRuntime {
  string equipmentRun;EquipmentInventoryController equipmentInventory;Task equipmentRead;long equipmentGeneration;
  void ResetEquipmentInventory(){equipmentGeneration++;equipmentInventory?.Invalidate();equipmentInventory=null;equipmentRun=null;equipmentRead=null;}
  public EquipmentInventoryController EquipmentInventory(string run){SynchronizeSession();if(owner==null||disposed)throw new InvalidOperationException("Authentication required");if(string.IsNullOrWhiteSpace(run))throw new ArgumentException("Run required");if(equipmentRun!=run){ResetEquipmentInventory();equipmentRun=run;equipmentInventory=new EquipmentInventoryController(owner.AccountId,run);}return equipmentInventory;}
  public Task ReadEquipmentInventoryAsync(string run,bool refresh=false){
   var target=EquipmentInventory(run);if(!refresh&&target.Snapshot!=null)return Task.CompletedTask;
   if(equipmentRead!=null&&!equipmentRead.IsCompleted)return equipmentRead;
   var original=owner;long stamp=ownerGeneration,request=equipmentGeneration;return equipmentRead=Read();
   async Task Read(){var reply=await new GameApiAuthenticatedHttpTransport(wire).SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/equipment",null,null),original.AccessToken);
    if(!Current(original,stamp)||request!=equipmentGeneration||target!=equipmentInventory)throw new OperationCanceledException("Equipment owner changed");
    if(reply.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded||reply.Response.StatusCode!=200)throw new InvalidOperationException("Equipment inventory request failed");
    target.Apply(EquipmentInventoryModel.Read(original.AccountId,run,reply.Response.BodyJson));
   }
  }
 }
}
