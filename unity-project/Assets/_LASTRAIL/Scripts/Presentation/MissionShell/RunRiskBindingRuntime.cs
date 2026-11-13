using System;
using LastRail.Auth;
using LastRail.Run;
namespace LastRail.Presentation.MissionShell {
 public sealed partial class AccountScreenRuntime {
  RunRiskBindingClient riskRunBinding;
  public RunRiskBindingClient RiskRunBinding {get {if(disposed)throw new ObjectDisposedException(nameof(AccountScreenRuntime));if(riskRunBinding==null){riskRunBinding=new RunRiskBindingClient(new GameApiAuthenticatedHttpTransport(wire),session,generation);riskRunBinding.Changed+=RiskRunBindingChanged;}return riskRunBinding;}}
  void RiskRunBindingChanged()=>Changed?.Invoke();
  void DisposeRiskRunBinding(){if(riskRunBinding==null)return;riskRunBinding.Changed-=RiskRunBindingChanged;riskRunBinding.Dispose();riskRunBinding=null;}
 }
}
