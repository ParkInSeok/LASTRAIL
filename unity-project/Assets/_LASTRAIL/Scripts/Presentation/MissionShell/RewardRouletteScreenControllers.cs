using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
 public sealed class OfflineRouletteScreenState
 {
  public JObject Result {get;}
  public JArray Lines {get;}
  public string Probability {get;}
  public int[] Multipliers {get;}
  public string Notice {get;}
  public string Label {get;}
  public bool Enabled {get;}
  public bool Busy {get;}
  public bool Paid {get;}
  public bool SeparateRefresh {get;}
  public OfflineRouletteScreenState(AccountScreenRuntime state)
  {
   var intent=state.AdIntent;var result=intent?.Result;
   bool terminal=intent?.Status=="CLAIMED"||intent?.Status=="EXPIRED"||intent?.Status=="SUPPLY_CONSUMED";
   bool paid=intent?.Status=="CLAIMED"||intent?.Status=="SUPPLY_CONSUMED";
   string label=terminal?"확인":state.CanSpinAd?"룰렛 돌리기":state.Pending?.Kind=="ad-intent"&&state.AdProviderAvailable?"광고 시청":state.HasPendingAd?"다시 확인":"광고 시청";
   bool owned=state.AdSkip.Snapshot?.Owned==true,unknown=state.AdSkip.Snapshot==null;
   if(!terminal&&!state.CanSpinAd){if(state.Pending?.Kind=="reward-approval")label="사용 결과 재확인";else if(owned)label="광고 없이 룰렛";else if(unknown)label=state.HasPendingAd?"다시 확인":"이용권 다시 확인";}
   if(!terminal&&!state.CanSpinAd&&!owned&&state.HasPendingAd&&state.RuntimeAds.Status!=AdRuntimePolicyStatus.Enabled)label="다시 확인";
   string notice=state.AdSkipUse.Notice;
   if(string.IsNullOrEmpty(notice))notice=unknown&&!state.HasPendingAd?state.AdSkip.Notice:state.AdNotice??"";
   if(!state.Busy&&intent==null&&state.AdSkip.Snapshot?.Status=="REVOKED"&&string.IsNullOrEmpty(state.AdSkipUse.Notice))notice="이용권 상태가 변경되었습니다. 기본 보급은 받을 수 있습니다.";
   if(result!=null||state.CanSpinAd&&!state.Busy)notice="";else if(intent?.Status=="SUPPLY_CONSUMED")notice="기본 보급을 이미 수령했습니다.";
   else if(string.IsNullOrEmpty(notice))notice=state.Busy?"결과 확인 중…":intent?.Status=="AD_PENDING"?"광고 확인 중…":!state.AdProviderAvailable?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"";
   var rows=new JArray();if(state.Offline!=null)foreach(var reward in state.Offline.RewardLines)rows.Add(new JObject{["id"]=reward.RewardCode,["kind"]=reward.Kind,["source"]=reward.Source,["quantity"]=reward.Quantity});
   if(intent?.BaseLines!=null)rows=intent.BaseLines;
   Probability=intent==null?"보급 정보를 먼저 확인해 주세요.":"가능 배수  "+string.Join(" · ",intent.Multipliers.Select(x=>x+"배"))+"\n\n"+intent.ProbabilityText;
   Multipliers=intent?.Multipliers.ToArray();Result=result;Lines=rows;Notice=notice;Label=label;Busy=state.Busy;Paid=paid;
   SeparateRefresh=state.HasPendingAd&&!terminal&&label!="다시 확인";
   Enabled=terminal||state.HasPendingAd||state.Pending?.Kind=="reward-approval"||state.Pending==null&&(owned||unknown||state.AdProviderAvailable);
  }
 }
 public sealed class OfflineRouletteScreenController:IDisposable
 {
  readonly AccountScreenRuntime account;readonly Func<bool> current,sessionCurrent;Action<OfflineRouletteScreenState> render;Action back;bool disposed;
  bool Current=>!disposed&&current()&&sessionCurrent();
  public OfflineRouletteScreenController(AccountScreenRuntime account,Action<OfflineRouletteScreenState> render,Action back,Func<bool> current){this.account=account;this.render=render;this.back=back;this.current=current;sessionCurrent=account.CaptureScreenSession();}
  public void Render(){if(Current)render(new OfflineRouletteScreenState(account));}
  public Task EnterAsync(bool use)=>Current&&use?account.UseAdSkipOfflineAsync():Task.CompletedTask;
  public Task RecheckAsync()=>Current?account.RecheckAdAsync():Task.CompletedTask;
  public void Back(){if(Current)back();}
  public async Task ActAsync()
  {
   if(!Current)return;
   if(account.AdIntent?.Status=="CLAIMED"||account.AdIntent?.Status=="EXPIRED"||account.AdIntent?.Status=="SUPPLY_CONSUMED"){account.AcknowledgeAdResult();if(Current)back();return;}
   if(account.Busy)return;
   if(account.Pending?.Kind=="reward-approval")await account.RetryPendingAsync();
   else if(account.CanSpinAd)await account.SpinAdAsync();
   else if(account.AdSkip.Snapshot?.Owned==true)await account.UseAdSkipOfflineAsync();
   else if(account.AdSkip.Snapshot==null){if(account.HasPendingAd)await account.RecheckAdAsync();else await account.RefreshAdSkipAsync();}
   else if(account.Pending?.Kind=="ad-intent"&&account.AdIntent?.Status=="AD_PENDING"&&account.AdProviderAvailable&&account.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled)await account.PresentAdAsync();
   else if(account.HasPendingAd)await account.RecheckAdAsync();else await account.BeginAdAsync();
  }
  public void Dispose(){disposed=true;render=null;back=null;}
 }
 public sealed class CombatRouletteDisplayState
 {
  public JObject Result {get;}
  public JArray Lines {get;}
  public string Notice {get;}
  public string Label {get;}
  public bool Enabled {get;}
  public bool Busy {get;}
  public bool Paid {get;}
  public bool SeparateRefresh {get;}
  public bool CanBasic {get;}
  public CombatRouletteDisplayState(CombatAdIntent intent,string message,bool busy,bool provider,bool pending,bool canBasic,bool canPresent=false,string mainLabel=null,bool? canMain=null)
  {
   Paid=intent?.Status=="CLAIMED"||intent?.Status=="REWARD_CONSUMED";Result=intent?.Result;Lines=intent?.BaseLines;Busy=busy;CanBasic=canBasic;
   Label=mainLabel??(intent?.Terminal==true?"확인":intent?.CanSpin==true?"룰렛 돌리기":canPresent?"광고 시청":pending?"다시 확인":"광고 시청");
   var notice=message;
   if(Result!=null||intent?.CanSpin==true&&!busy)notice="";else if(intent?.Status=="REWARD_CONSUMED")notice="기본 보상 수령 내역은 작전 결과에서 확인하세요.";
   else if(string.IsNullOrEmpty(notice))notice=busy?"결과 확인 중…":intent?.Status=="AD_PENDING"?"광고 확인 중…":!provider&&!pending?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"";
   Notice=notice;SeparateRefresh=pending&&intent?.Terminal!=true&&Label!="다시 확인";Enabled=canMain??(Paid||intent?.Terminal==true||pending||provider);
  }
 }
 public sealed class CombatRouletteScreenState
 {
  public CombatAdIntent Intent {get;}
  public string Notice {get;}
  public string Label {get;}
  public bool Busy {get;}
  public bool Provider {get;}
  public bool Pending {get;}
  public bool CanBasic {get;}
  public bool CanPresent {get;}
  public CombatRouletteDisplayState Display {get;}
  public CombatRouletteScreenState(AccountScreenRuntime owner,string run)
  {
   var c=owner.CombatAds;var intent=c.Intent?.RunId==run?c.Intent:null;
   bool owned=owner.AdSkip.Snapshot?.Owned==true,unknown=owner.AdSkip.Snapshot==null,approval=owner.Pending?.Kind=="reward-approval";
   string label=intent?.Terminal==true||intent?.CanSpin==true?null:approval?"사용 결과 재확인":owned?"광고 없이 룰렛":unknown?c.HasPending(run)?"다시 확인":"이용권 다시 확인":null;
   if(intent?.Terminal!=true&&intent?.CanSpin!=true&&!owned&&c.HasPending(run)&&owner.RuntimeAds.Status!=AdRuntimePolicyStatus.Enabled)label="다시 확인";
   string notice=!string.IsNullOrEmpty(owner.AdSkipUse.Notice)?owner.AdSkipUse.Notice:unknown&&!c.HasPending(run)?owner.AdSkip.Notice:c.Notice;
   if(!owner.Busy&&intent==null&&!c.HasPending(run)&&!unknown&&!owned&&string.IsNullOrEmpty(owner.AdSkipUse.Notice))notice=owner.AdSkip.Snapshot.Status=="REVOKED"?"이용권 상태가 변경되었습니다. 기본 보상은 받을 수 있습니다.":!c.ProviderAvailable?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"광고 시청 후 룰렛을 이용할 수 있습니다.";
   Intent=intent;Notice=notice;Label=label;Busy=owner.Busy;Provider=c.ProviderAvailable;Pending=c.HasPending(run);CanBasic=owner.CanClaimRunReward(run);CanPresent=c.CanPresent(run);
   Display=new CombatRouletteDisplayState(Intent,Notice,Busy,Provider,Pending,CanBasic,CanPresent,Label,Label==null?null:(bool?)true);
  }
 }
 public sealed class CombatRouletteScreenController:IDisposable
 {
  readonly AccountScreenRuntime account;readonly string run;readonly Func<bool> current,sessionCurrent;Action<CombatRouletteScreenState> render;Action close;bool disposed;
  bool Current=>!disposed&&current()&&sessionCurrent();
  public CombatRouletteScreenController(AccountScreenRuntime account,string run,Action<CombatRouletteScreenState> render,Action close,Func<bool> current){this.account=account;this.run=run;this.render=render;this.close=close;this.current=current;sessionCurrent=account.CaptureScreenSession();}
  public void Render(){if(Current)render(new CombatRouletteScreenState(account,run));}
  public async Task EnterAsync(bool use,string preview){if(!Current)return;if(use&&preview!=null)await account.UseAdSkipCombatAsync(run,preview);else await account.RunCombatAdAsync(run,"read");Render();}
  public async Task ActAsync()
  {
   if(!Current||account.Busy)return;var c=account.CombatAds;
   if(c.Intent?.RunId==run&&c.Intent.Terminal){c.Acknowledge();if(Current)close();return;}
   if(account.Pending?.Kind=="reward-approval")await account.RetryPendingAsync();
   else if(c.Intent?.RunId==run&&c.Intent.CanSpin)await account.RunCombatAdAsync(run,"spin");
   else if(account.AdSkip.Snapshot?.Owned==true){var preview=account.Lifecycle?.RunId==run?account.Lifecycle.PreviewId:null;if(preview!=null)await account.UseAdSkipCombatAsync(run,preview);}
   else if(account.AdSkip.Snapshot==null){if(c.HasPending(run))await account.RunCombatAdAsync(run,"read");else await account.RefreshAdSkipAsync();}
   else await account.RunCombatAdAsync(run,c.CanPresent(run)&&account.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled?"begin":c.HasPending(run)?"read":"begin");Render();
  }
  public async Task RefreshAsync(){if(!Current)return;await account.RunCombatAdAsync(run,"read");Render();}
  public async Task ClaimBasicAsync(){if(!Current)return;await account.ClaimRunRewardAsync(run);if(!Current)return;await account.RunCombatAdAsync(run,"read");Render();}
  public void Close(){if(Current)close();}
  public void Dispose(){disposed=true;render=null;close=null;}
 }
}
