using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class AdSkipAccountService : IDisposable
    {
        readonly IRunApiHttpTransport wire;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<long> generation;
        long request;
        bool disposed;
        public AdSkipSnapshot Snapshot { get; private set; }
        public bool Loading { get; private set; }
        public string Notice { get; private set; }="";
        public event Action Changed;
        public AdSkipAccountService(IRunApiHttpTransport wire,Func<GameApiAuthSessionSnapshot> session,Func<long> generation)
        {this.wire=wire??throw new ArgumentNullException(nameof(wire));this.session=session??throw new ArgumentNullException(nameof(session));this.generation=generation??throw new ArgumentNullException(nameof(generation));}
        bool Current(GameApiAuthSessionSnapshot owner,long stamp,long serial)=>!disposed&&serial==request&&ReferenceEquals(owner,session())&&stamp==generation();
        public async Task RefreshAsync()
        {
            if(disposed)return;var owner=session();long stamp=generation(),serial=++request;
            Snapshot=null;Loading=false;
            if(owner==null||owner.AccountStatus!="ACTIVE"||owner.AccessTokenExpiresAt<=DateTime.UtcNow){Notice="로그인 상태를 확인해 주세요.";Changed?.Invoke();return;}
            Loading=true;Notice="보유 여부 확인 중";Changed?.Invoke();
            try
            {
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/ad-skip",null,null),owner.AccessToken));
                if(!Current(owner,stamp,serial))return;
                if(response.IsNetworkError||response.StatusCode!=200){Notice="이용권 확인 필요 · 다시 확인해 주세요.";return;}
                Snapshot=AdSkipSnapshot.Parse(response.BodyJson);Notice="";
            }
            catch(Exception exception) when(exception is FormatException||exception is Newtonsoft.Json.JsonException||exception is ArgumentException||exception is OverflowException)
            {if(Current(owner,stamp,serial)){Snapshot=null;Notice="이용권 응답 형식을 확인하지 못했습니다.";}}
            catch(Exception)
            {if(Current(owner,stamp,serial)){Snapshot=null;Notice="이용권 조회를 완료하지 못했습니다. 다시 확인해 주세요.";}}
            finally{if(Current(owner,stamp,serial)){Loading=false;Changed?.Invoke();}}
        }
        // The account composition publishes its completed reset once all account-owned models are cleared.
        public void Reset(){request++;Snapshot=null;Loading=false;Notice="";}
        public void Dispose(){disposed=true;Reset();Changed=null;}
    }
}
