using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03LocalClientTests {
 sealed class Wire:IRunApiHttpTransport {
  public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();public bool Lost;public TaskCompletionSource<RunApiHttpResponse> Read;
  public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);if(Read!=null&&request.Method=="GET")return Read.Task;if(Lost)return Task.FromResult(RunApiHttpResponse.NetworkError("lost"));var body=JObject.Parse(request.BodyJson);return Task.FromResult(RunApiHttpResponse.Success(200,new JObject{["rulesVersion"]=body["rulesVersion"],["catalogVersion"]=body["catalogVersion"],["revision"]=(long)body["expectedRevision"]+1,["slots"]=body["slots"]}.ToString()));}
 }
 static readonly Uri Local=new Uri("http://127.0.0.1:12345/");static readonly string[] Ids={"barrier","support_fire","emergency_repair"};
 [Test] public async Task DelayedSupportReadCannotRollBackConfirmedSaveRevision(){var w=new Wire{Read=new TaskCompletionSource<RunApiHttpResponse>()};var c=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),_=>{});var read=c.ReadSupportAsync();await c.SaveSupportAsync(1,Ids);w.Read.SetResult(RunApiHttpResponse.Success(200,"{\"selection\":{\"revision\":1}}"));await read;Assert.AreEqual(2,c.KnownSupportRevision);}
 [Test] public void DiskFailureNeverSendsRequest(){var w=new Wire();var c=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),s=>throw new IOException());Assert.ThrowsAsync<IOException>(async()=>await c.SaveSupportAsync(0,Ids));Assert.AreEqual(0,w.Requests.Count);Assert.IsFalse(c.HasPending);}
 [Test] public async Task LostResponseRestoresOriginalKeyAndOrder(){var w=new Wire{Lost=true};string saved=null;var c=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),s=>saved=s);Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.SaveSupportAsync(0,Ids));Assert.IsTrue(c.HasPending);var original=w.Requests[0];w.Lost=false;var restored=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),s=>saved=s,saved);await restored.RetryAsync();Assert.AreEqual(original.BodyJson,w.Requests[1].BodyJson);CollectionAssert.AreEqual(original.Headers.Select(h=>h.Value),w.Requests[1].Headers.Select(h=>h.Value));Assert.IsFalse(restored.HasPending);Assert.AreEqual(1,((JObject)JObject.Parse(saved)["receipts"]).Count);}
 [Test] public async Task ReceiptWriteFailureKeepsPendingOriginal(){var w=new Wire();int writes=0;var c=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),s=>{if(++writes==2)throw new IOException();});Assert.ThrowsAsync<IOException>(async()=>await c.SaveSupportAsync(0,Ids));Assert.IsTrue(c.HasPending);await c.RetryAsync();Assert.AreEqual(w.Requests[0].BodyJson,w.Requests[1].BodyJson);Assert.IsFalse(c.HasPending);}
 [Test] public void DifferentOwnerHostAndNewRequestCannotReplacePending(){var w=new Wire{Lost=true};string saved=null;var c=new Ce03LocalClient(Local,"a",w,new GameApiAccessToken("lst1.test"),s=>saved=s);Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.SaveSupportAsync(0,Ids));Assert.Throws<FormatException>(()=>new Ce03LocalClient(Local,"b",w,new GameApiAccessToken("lst1.test"),s=>{},saved));Assert.Throws<FormatException>(()=>new Ce03LocalClient(new Uri("http://127.0.0.1:23456/"),"a",w,new GameApiAccessToken("lst1.test"),s=>{},saved));Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.SaveSupportAsync(1,Ids.Reverse().ToArray()));Assert.AreEqual(1,w.Requests.Count);}
}
