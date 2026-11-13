using LastRail.Battle;
namespace LastRail.Presentation.MissionShell {
public sealed partial class LobbyCommandDeck {
 Ce03LocalClient localCommandClient;Ce03LocalDeparture localDeparture;
 public void BindLocalDeparture(Ce03LocalDeparture flow){if(localDeparture!=null)localDeparture.Changed-=UpdateDepartureDialog;localDeparture=flow;if(isActiveAndEnabled){if(flow!=null)flow.Changed+=UpdateDepartureDialog;UpdateDepartureDialog();}}
 // Only the disposable-host composition injects this. Public account departure
 // retains its existing transport and cannot silently switch local hosts.
 public void BindLocalCommandClient(Ce03LocalClient client){localCommandClient=client;if(isActiveAndEnabled)UpdateDepartureDialog();}
 public void OpenLocalCommandSelection(){if(localCommandClient==null)return;CloseDialog();Ce03SupportSelectionPanel.Open(transform,localCommandClient,()=>{});}
}
}
