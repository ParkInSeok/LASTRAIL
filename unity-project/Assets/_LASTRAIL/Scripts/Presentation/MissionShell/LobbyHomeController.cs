using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed class LobbyHomeState
    {
        public IReadOnlyList<string> Balances {get;}
        public IReadOnlyList<string> Operators {get;}
        public string Notice {get;}
        public string CurrencyDetails {get;}
        public LobbyHomeState(AccountScreenRuntime owner)
        {
            Balances=Array.AsReadOnly(Enumerable.Range(0,3).Select(i=>owner?.BalanceText(i)??"—").ToArray());
            Operators=Array.AsReadOnly(Enumerable.Range(0,5).Select(i=>owner?.Account?.ActiveOperators.FirstOrDefault(o=>o.Number("slot")==i)?.Text("operatorId")).ToArray());
            Notice=owner?.AccountNotice??"";
            CurrencyDetails=owner==null?"재화 정보를 아직 불러오지 못했습니다.":"연료  "+Balances[0]+"\n크레딧  "+Balances[1]+"\n크리스탈  "+Balances[2]+"\n\n"+Notice;
        }
    }
    public sealed class LobbyHomeController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly Func<bool> session;
        Action<LobbyHomeState> render;Action<string> showCurrency;bool disposed;long request;
        public LobbyHomeController(AccountScreenRuntime owner,Action<LobbyHomeState> render,Action<string> showCurrency)
        {this.owner=owner;session=owner?.CaptureScreenSession()??(()=>true);this.render=render;this.showCurrency=showCurrency;}
        public bool IsCurrent=>!disposed&&session();
        public void Render(){if(IsCurrent)render?.Invoke(new LobbyHomeState(owner));}
        public async Task OpenCurrencyAsync()
        {
            if(!IsCurrent)return;var stamp=++request;if(owner!=null)await owner.EnsureSharedAsync();
            if(IsCurrent&&stamp==request)showCurrency?.Invoke(new LobbyHomeState(owner).CurrencyDetails);
        }
        public void CancelPopupRequest(){request++;}
        public void Dispose(){disposed=true;request++;render=null;showCurrency=null;}
    }
}
