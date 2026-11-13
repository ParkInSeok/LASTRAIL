#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace LastRail.Battle {
// Plaintext disposable Editor QA port, never Android credential storage.
public sealed class Ce03EditorClaimJournal:IRunOperationJournal {
 readonly string path,owner;readonly object gate=new object();
 public Ce03EditorClaimJournal(string path,string owner){if(!Path.IsPathRooted(path)||string.IsNullOrWhiteSpace(owner))throw new ArgumentException();this.path=path;this.owner=owner;}
 List<PendingRunOperation> Load(){if(!File.Exists(path))return new List<PendingRunOperation>();JObject root;using(var reader=new JsonTextReader(new StringReader(File.ReadAllText(path))){DateParseHandling=DateParseHandling.None})root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});Ce03CommandContract.Fields(root,"schema","owner","rows");if((string)root["schema"]!="ce03-editor-claim-journal-v1"||(string)root["owner"]!=owner)throw new FormatException("Claim journal owner mismatch");return ((JArray)root["rows"]).Select(r=>new PendingRunOperation(PendingRunKind.Claim,owner,(string)r["target"],(string)r["key"],(string)r["body"],new AppBuildSnapshot((string)r["appVersion"],(long?)r["appVersionCode"]),DateTimeOffset.Parse((string)r["composedAt"]),(string)r["response"])).ToList();}
 bool Save(List<PendingRunOperation> rows){string raw=Ce03CommandContract.Canonical(new JObject{["schema"]="ce03-editor-claim-journal-v1",["owner"]=owner,["rows"]=new JArray(rows.Select(r=>new JObject{["target"]=r.TargetId,["key"]=r.IdempotencyKey,["body"]=r.BodyJson,["appVersion"]=r.Build.AppVersion,["appVersionCode"]=r.Build.AppVersionCode,["composedAt"]=r.ComposedAt.ToString("O"),["response"]=r.AcceptedResponseJson}))});if(raw.Length>1024*1024)return false;string temp=path+".pending";using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){var bytes=new System.Text.UTF8Encoding(false).GetBytes(raw);s.Write(bytes,0,bytes.Length);s.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);return File.ReadAllText(path)==raw;}
 public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account){lock(gate){if(account!=owner)throw new InvalidOperationException("Claim journal owner mismatch");return Task.FromResult<IReadOnlyList<PendingRunOperation>>(Load());}}
 public Task<bool> PutAsync(PendingRunOperation operation){lock(gate){if(operation.Kind!=PendingRunKind.Claim||operation.AccountId!=owner)throw new InvalidOperationException("Claim only local port");var rows=Load();var prior=rows.SingleOrDefault(r=>r.TargetId==operation.TargetId||r.IdempotencyKey==operation.IdempotencyKey);if(prior!=null)return Task.FromResult(prior.SameRequest(operation));rows.Add(operation);return Task.FromResult(Save(rows));}}
 public Task<bool> ConfirmAsync(PendingRunOperation operation,string response){lock(gate){var rows=Load();var prior=rows.SingleOrDefault(r=>r.SameRequest(operation));if(prior==null)return Task.FromResult(false);rows[rows.IndexOf(prior)]=prior.Confirmed(response);return Task.FromResult(Save(rows));}}
 public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key){lock(gate){if(account!=owner||kind!=PendingRunKind.Claim)return Task.FromResult(false);var rows=Load();var prior=rows.SingleOrDefault(r=>r.IdempotencyKey==key);if(prior==null||prior.AcceptedResponseJson==null)return Task.FromResult(false);rows.Remove(prior);return Task.FromResult(Save(rows));}}
}
}
#endif
