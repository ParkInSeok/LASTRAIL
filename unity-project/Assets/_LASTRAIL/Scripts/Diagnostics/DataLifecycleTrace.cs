using System;
using System.Diagnostics;
using LastRail.Client;
using Newtonsoft.Json.Linq;
namespace LastRail.Diagnostics {
 public enum LifecycleData { Other,Profile,Story,Account,Wallet,Growth,Train,Ultimate,Banners,RunStart,RunSelect,RunResult,RunClaim,Memory }
 public enum LifecycleEvent { Requested,Sent,Received,CacheHit,Joined,Invalidated,ReferenceReleased,Retry,Discarded,Conflict,CredentialsChanged }
 public enum LifecycleReason { Entry,ExplicitRefresh,Command,SessionChanged,OwnerDisposed,CredentialChanged,Response,Failure,OlderGeneration,UserExport }
 // Diagnostic observer only. Fixed vocabulary excludes account IDs, tokens,
 // raw endpoints and payloads. ReferenceReleased never means GC completed.
 public sealed class DataLifecycleTrace:IDisposable {
  const int Capacity=256;
  readonly bool dev;readonly Row[] rows;readonly long origin=Stopwatch.GetTimestamp();
  int start,count;long sequence,dropped;bool disposed;
  struct Row {public long Sequence,Utc,Elapsed,Generation,Version;public int Count;public LifecycleData Data;public LifecycleEvent Event;public LifecycleReason Reason;}
  public bool Enabled=>dev&&!disposed;
  public int Count=>count;
  public DataLifecycleTrace(ClientEnvironment environment,string configUrl){dev=environment==ClientEnvironment.Dev&&string.Equals(configUrl,ClientEnvironmentBootstrap.DevClientConfigUrl,StringComparison.Ordinal);rows=dev?new Row[Capacity]:Array.Empty<Row>();}
  public void Record(LifecycleData data,LifecycleEvent kind,LifecycleReason reason,long generation=0,long version=-1,int records=0){
   if(!Enabled)return;
   var row=new Row{Sequence=++sequence,Utc=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),Elapsed=Stopwatch.GetTimestamp()-origin,Generation=generation,Version=version,Count=records,Data=data,Event=kind,Reason=reason};
   if(count==Capacity){rows[start]=row;start=(start+1)%Capacity;dropped++;}else{rows[(start+count)%Capacity]=row;count++;}
  }
  public string Export(){
   if(!Enabled)return "";var values=new JArray();
   for(int i=0;i<count;i++){var r=rows[(start+i)%Capacity];values.Add(new JObject{["sequence"]=r.Sequence,["clientUtcMs"]=r.Utc,["monotonicTicks"]=r.Elapsed,["generation"]=r.Generation,["data"]=r.Data.ToString(),["event"]=r.Event.ToString(),["reason"]=r.Reason.ToString(),["version"]=r.Version<0?JValue.CreateNull():new JValue(r.Version),["records"]=r.Count});}
   return new JObject{["version"]="data.lifecycle.v1.20261021",["clockFrequency"]=Stopwatch.Frequency,["capacity"]=Capacity,["droppedOldest"]=dropped,["records"]=values,["memoryRule"]="reference-release is not GC; texture CPU/GPU reclamation is not measured by this event log"}.ToString();
  }
  public void Dispose(){if(disposed)return;disposed=true;Array.Clear(rows,0,rows.Length);start=count=0;}
 }
}
