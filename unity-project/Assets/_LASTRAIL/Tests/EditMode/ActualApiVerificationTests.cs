#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Support;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class ActualApiVerificationTests
    {
        sealed class ObservedWire : IRunApiHttpTransport
        {
            readonly IRunApiHttpTransport inner;
            public bool MalformNext, LosePost, HoldNext, DenyNext;
            public readonly List<int> Statuses=new List<int>();
            public int Posts, Gets;
            public TaskCompletionSource<bool> Arrived, Release;
            public ObservedWire(IRunApiHttpTransport inner) { this.inner=inner; }
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) {
                if(request.Method=="POST") Posts++; else Gets++;
                if(DenyNext) { DenyNext=false; request=GameApiBearerAuthRequestFactory.WithBearerToken(request,new GameApiAccessToken("lst1.invalid-test-token")); }
                var response=await inner.SendAsync(request); Statuses.Add(response.IsNetworkError ? 0 : response.StatusCode);
                if(HoldNext) { HoldNext=false; Arrived.SetResult(true); await Release.Task; }
                if(MalformNext) { MalformNext=false; Check(!response.IsNetworkError&&response.StatusCode==200); return RunApiHttpResponse.Success(200,"{broken"); }
                if(LosePost && request.Method=="POST") { LosePost=false; Check(!response.IsNetworkError&&response.StatusCode==200); return RunApiHttpResponse.NetworkError("QA response loss after HTTP commit"); }
                return response;
            }
        }
        static void Check(bool ok) { if(!ok) throw new InvalidOperationException("Verification expectation failed."); }
        static GameApiAuthSessionSnapshot Session(ApiVerificationConfiguration c) => new GameApiAuthSessionSnapshot(c.AccountId,"ACTIVE",
            new GameApiAccessToken(c.Token),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("test-only-unused"),DateTime.UtcNow.AddMinutes(10));
        static async Task Case(JArray cases,string id,string kind,ObservedWire wire,Func<Task> action) {
            int start=wire.Statuses.Count;
            try { await action(); cases.Add(new JObject{["case"]=id,["kind"]=kind,["status"]="PASS"}); }
            catch { cases.Add(new JObject{["case"]=id,["kind"]=kind,["status"]="FAIL",["reason"]="EXPECTATION_OR_CALL_FAILED"}); }
            ((JObject)cases.Last)["observedHttpStatuses"]=new JArray(wire.Statuses.Skip(start));
            // Do not persist exception messages, HTTP bodies, headers, account or receipt identifiers.
        }
        static void Pending(JArray cases,string id,string reason,string status="BLOCKED") => cases.Add(new JObject{
            ["case"]=id,["kind"]="ACTUAL_HTTP",["status"]=status,["reason"]=reason });
        [Test]
        public async Task ConfiguredActualHttpAndConsumerChecks()
        {
            var path=Environment.GetEnvironmentVariable("LASTRAIL_API_VERIFICATION_CONFIG");
            if(string.IsNullOrEmpty(path)) Assert.Ignore("Isolated configuration required; use Tools/run-api-verification.ps1.");
            var output=Environment.GetEnvironmentVariable("LASTRAIL_API_VERIFICATION_REPORT");
            Check(!string.IsNullOrWhiteSpace(output) && Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase));
            ApiVerificationConfiguration c;
            try { c=ApiVerificationConfiguration.Parse(JObject.Parse(File.ReadAllText(path))); }
            catch { Assert.Fail("Invalid isolated configuration; no request sent."); return; }
            var cases=new JArray(); var report=c.SafeMetadata(); report["cases"]=cases;
            var token=new GameApiAccessToken(c.Token); var session=Session(c);
            var actual=new InstalledAdmissionTransport(new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(c.Origin,10)),
                new AppBuildSnapshot("1.0.0-api-qa",c.VersionCode),"android",null);
            try {
                if(c.Profile=="ACCOUNT_READ_LOCAL") {
                    var observed=new ObservedWire(actual);
                    using(var model=new AccountScreenRuntime(observed,null,()=>session,()=>1)) {
                        await Case(cases,"GET /v1/account/state:consumer-schema-and-model","ACTUAL_HTTP",observed,async()=>{
                            await model.RefreshAccountAsync(); Check(model.AccountStatus==AccountStateReadStatus.Ready && model.Account!=null
                                && model.Account.AccountId==c.AccountId && model.Account.MutationSequence>=0 && model.Account.Currencies.Values.All(x=>x>=0));
                        });
                        var before=model.Account;
                        await Case(cases,"GET /v1/account/state:missing-auth","ACTUAL_HTTP",observed,async()=>{
                            var reply=await observed.SendAsync(new RunApiHttpRequest("GET","/v1/account/state",null,null));
                            Check(!reply.IsNetworkError && reply.StatusCode==401); Check(JObject.Parse(reply.BodyJson)["errorCode"]?.Type==JTokenType.String);
                            Check(ReferenceEquals(before,model.Account));
                        });
                        await Case(cases,"GET /v1/account/state:invalid-token-model-unchanged","ACTUAL_HTTP",observed,async()=>{
                            observed.DenyNext=true;
                            await model.RefreshAccountAsync(); Check(observed.Statuses.Last()==401 && model.AccountStatus==AccountStateReadStatus.AuthRequired
                                && ReferenceEquals(before,model.Account) && model.AccountIsStale);
                            await model.RefreshAccountAsync(); Check(observed.Statuses.Last()==200 && model.AccountStatus==AccountStateReadStatus.Ready && !model.AccountIsStale); Check(model.Account.MutationSequence==before.MutationSequence
                                && model.Account.Currencies.All(x=>before.Currencies[x.Key]==x.Value));
                        });
                        await Case(cases,"GET /v1/account/state:malformed-after-real-response","INJECTED_AFTER_HTTP",observed,async()=>{
                            var stable=model.Account; observed.MalformNext=true; await model.RefreshAccountAsync();
                            Check(model.AccountStatus==AccountStateReadStatus.ContractMismatch && ReferenceEquals(stable,model.Account) && model.AccountIsStale);
                        });
                        await Case(cases,"GET /v1/account/state:dispose-before-late-response","INJECTED_AFTER_HTTP",observed,async()=>{
                            observed.HoldNext=true; observed.Arrived=new TaskCompletionSource<bool>(); observed.Release=new TaskCompletionSource<bool>();
                            var stable=model.Account; int events=0; model.Changed+=()=>events++;
                            var pending=model.RefreshAccountAsync(); await observed.Arrived.Task; model.Dispose(); int ended=events;
                            observed.Release.SetResult(true); await pending; Check(model.Account==null && events==ended);
                        });
                    }
                    Pending(cases,"support-and-public-current-contracts","SERVER_LISTENING_ISOLATED_ENVIRONMENT_REQUIRED");
                } else {
                    var observed=new ObservedWire(actual);
                    var client=new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(observed),()=>new AppBuildSnapshot("1.0.0-api-qa",c.VersionCode));
                    var model=client.CreateController(session);
                    await Case(cases,"POST /v1/support/tickets + GET /receipt:lost-response-original-key","INJECTED_AFTER_HTTP",observed,async()=>{
                        Check(model.Edit("BUG_OTHER","Isolated automated API verification.")); observed.LosePost=true;
                        await model.SubmitAsync(); Check(model.State==InquiryState.Uncertain && observed.Posts==1);
                        var original=model.Pending; await model.ReconcileAsync();
                        Check(model.State==InquiryState.Received && ReferenceEquals(original,model.Pending) && observed.Posts==1);
                        var receipt=await client.SubmitAsync(original,session);
                        Check(receipt.Delivery==InquiryDelivery.Received && receipt.TicketId==model.Receipt.TicketId && observed.Posts==2);
                        var list=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets?limit=50",null,null),token));
                        Check(!list.IsNetworkError && list.StatusCode==200);
                        var json=JObject.Parse(list.BodyJson); Check((string)json["contractVersion"]=="private-support-list-v1"
                            && ((JArray)json["items"]).Count(x=>(string)x["ticketId"]==receipt.TicketId)==1);
                    });
                    var normal=client.CreateController(session);
                    await Case(cases,"support:normal-submit-consumer","ACTUAL_HTTP",observed,async()=>{
                        Check(normal.Edit("BUG_OTHER","Second isolated socket verification inquiry."));await normal.SubmitAsync();
                        Check(normal.State==InquiryState.Received && normal.Receipt.TicketId!=null);
                    });
                    await Case(cases,"support:receipt-and-original-key-replay","ACTUAL_HTTP",observed,async()=>{
                        var receipt=await client.ReconcileAsync(normal.Pending,session);Check(receipt.Delivery==InquiryDelivery.Received && receipt.TicketId==normal.Receipt.TicketId);
                        var replay=await client.SubmitAsync(normal.Pending,session);Check(replay.Delivery==InquiryDelivery.Received && replay.TicketId==receipt.TicketId);
                    });
                    await Case(cases,"support:list-and-detail-private-contract","ACTUAL_HTTP",observed,async()=>{
                        var list=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets?limit=50",null,null),token));
                        Check(!list.IsNetworkError&&list.StatusCode==200);var rows=JObject.Parse(list.BodyJson);Check((string)rows["contractVersion"]=="private-support-list-v1" && ((JArray)rows["items"]).Count(x=>(string)x["ticketId"]==normal.Receipt.TicketId)==1);
                        var detail=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets/"+normal.Receipt.TicketId,null,null),token));
                        Check(!detail.IsNetworkError&&detail.StatusCode==200);var row=JObject.Parse(detail.BodyJson);Check((string)row["contractVersion"]=="private-support-ticket-v1" && (string)row["ticket"]["ticketId"]==normal.Receipt.TicketId && row["ticket"]["internalNote"]==null);
                    });
                    await Case(cases,"support:same-key-changed-body-conflict","ACTUAL_HTTP",observed,async()=>{
                        var req=new RunApiHttpRequest("POST","/v1/support/tickets",new JObject{["category"]="BUG_OTHER",["body"]="Changed frozen inquiry body for conflict verification."}.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key",normal.Pending.IdempotencyKey)});
                        var response=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(req,token));Check(!response.IsNetworkError&&response.StatusCode==409);
                    });
                    if(c.EnvironmentName=="LOCAL_SQLJS" && JObject.Parse(File.ReadAllText(path))["otherAccount"] is JObject)
                    await Case(cases,"support:other-account-detail-and-receipt404","ACTUAL_HTTP",observed,async()=>{
                        var raw=JObject.Parse(File.ReadAllText(path));var other=ApiVerificationConfiguration.ParseOther((JObject)raw["otherAccount"],c);var otherToken=new GameApiAccessToken(other.Token);
                        var detail=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets/"+normal.Receipt.TicketId,null,null),otherToken));Check(!detail.IsNetworkError&&detail.StatusCode==404);
                        var receipt=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets/receipt",null,new[]{new RunApiHttpHeader("Idempotency-Key",normal.Pending.IdempotencyKey)}),otherToken));Check(!receipt.IsNetworkError&&receipt.StatusCode==404);
                        var list=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets?limit=50",null,null),otherToken));Check(!list.IsNetworkError&&list.StatusCode==200 && ((JArray)JObject.Parse(list.BodyJson)["items"]).Count==0);
                    });
                    else Pending(cases,"support:other-account-detail-and-receipt404","LOCAL_MULTI_ACCOUNT_FIXTURE_REQUIRED");
                    await Case(cases,"support:missing-and-invalid-auth401","ACTUAL_HTTP",observed,async()=>{
                        var missing=await observed.SendAsync(new RunApiHttpRequest("GET","/v1/support/tickets?limit=50",null,null));Check(!missing.IsNetworkError&&missing.StatusCode==401);
                        var invalid=await observed.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/support/tickets?limit=50",null,null),new GameApiAccessToken("lst1.invalid-test-token")));Check(!invalid.IsNetworkError&&invalid.StatusCode==401);
                    });
                    Pending(cases,"support:expired-token","EXPIRED_SESSION_FIXTURE_NOT_PROVIDED","NOT_RUN");
                    Pending(cases,"public:published-source-and-version","SERVER_APPROVED_PUBLIC_SOURCE_REQUIRED");
                }
                Pending(cases,"player-device","TARGET_DEVICE_NOT_RUN","NOT_RUN");
                Pending(cases,"real-network-timeout-disconnect-cancel","CONTROLLED_NETWORK_ENVIRONMENT_REQUIRED","NOT_RUN");
                Pending(cases,"START-selection-shop-settlement-coop-world","OWNER_CONTRACT_AND_ISOLATED_ENVIRONMENT_REQUIRED");
            } finally {
                report["reportedAtUtc"]=DateTimeOffset.UtcNow.ToString("O");
                File.WriteAllText(output,report.ToString()+"\n");
            }
            Assert.That(cases.Count(x=>(string)x["status"]=="FAIL"),Is.Zero,"See sanitized case status report; raw responses are intentionally omitted.");
        }
    }
}
#endif




