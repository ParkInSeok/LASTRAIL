using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Sole owner of event-point records. Staging shares immutable records; public captures clone.
 internal sealed class BNativeOriginalEventJournal {
  internal const string Version="unity.combat-b.original-event-journal.v1.20261006";
  readonly List<JObject> records;int waves;bool terminal;
  internal BNativeOriginalEventJournal(){records=new List<JObject>();}
  BNativeOriginalEventJournal(BNativeOriginalEventJournal other){records=new List<JObject>(other.records);waves=other.waves;terminal=other.terminal;}
  internal BNativeOriginalEventJournal Clone()=>new BNativeOriginalEventJournal(this);
  internal void Observe(BWaveEntryOwner entry,string outcome,JObject command,int revision,Func<JObject> checkpoint){
   if(entry.CompletedWaves==waves&&(outcome=="RUNNING"||terminal))return;
   var point=checkpoint();
   foreach(var raw in ((JArray)entry.Capture()["events"]).Where(x=>(string)x["kind"]=="WAVE_CLEAR"&&(int)x["waveOrdinal"]>waves))Add("WAVE_CLEAR",(JObject)raw,command,revision,point);
   waves=entry.CompletedWaves;
   if(outcome!="RUNNING"&&!terminal){Add("TERMINAL",new JObject{["kind"]="TERMINAL",["outcome"]=outcome,["tick"]=point["tick"].DeepClone(),["sequence"]=point["sequence"].DeepClone()},command,revision,point);terminal=true;}
  }
  void Add(string kind,JObject raw,JObject command,int revision,JObject point){
   var binding=new JObject{["kind"]=kind,["rawEvent"]=raw.DeepClone(),["originalCommand"]=command.DeepClone(),["commandRevision"]=revision,["checkpointHash"]=BDirectAttackInput.Digest(point)};
   records.Add(new JObject{["eventRef"]=BDirectAttackInput.Digest(binding),["binding"]=binding,["checkpoint"]=point.DeepClone()});
  }
  internal JObject Capture()=>new JObject{["version"]=Version,["provenance"]="ORIGINAL_UNITY_LOCAL_NOT_SERVER_ACCEPTANCE",["records"]=new JArray(records.Select(x=>x.DeepClone()))};
 }
}
