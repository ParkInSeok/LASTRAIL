using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Only the authority's initial receipt supplies these values; progressing owners never reapply it.
 public sealed class OrdinaryTrainInitialState {
  public string TrainId{get;}
  public int Hp{get;}
  public int MaxHp{get;}
  internal readonly JObject Binding;
  internal OrdinaryTrainInitialState(JObject receipt,string account,string run,OrdinaryFiveStartInput original=null){
   var s=receipt["trainInitialState"] as JObject;
   Need(s!=null,"initial state missing");
   BDirectAttackInput.Fields(s,"version","accountId","runId","trainId","ordinaryOriginHash","sourceHash","healthTargetId","healthRevision","stage","logicalTimeMs","activeShields");
   TrainId=(string)s["trainId"];Need(!string.IsNullOrWhiteSpace(TrainId)&&s["trainId"].Type==JTokenType.String,"train identity");
   Need((string)s["version"]=="combat.ordinary-train-initial-state.v1.20261027"&&(string)s["accountId"]==account&&(string)s["runId"]==run&&(string)s["healthTargetId"]==TrainId,"scope");
   Need(s["healthRevision"]?.Type==JTokenType.Integer&&(long)s["healthRevision"]==1&&s["logicalTimeMs"]?.Type==JTokenType.Integer&&(long)s["logicalTimeMs"]==0&&(string)s["stage"]=="INITIAL_BEFORE_COMBAT"&&s["activeShields"] is JArray shields&&shields.Count==0,"initial phase");
   Need(JToken.DeepEquals(receipt["ordinaryTrainBinding"],new JObject{["version"]=OrdinaryTrainSource.InitialHealthVersion,["ordinaryOriginHash"]=s["ordinaryOriginHash"],["sourceHash"]=s["sourceHash"]}),"source binding");
   var health=receipt["health"];Need((long?)health?["revision"]==1&&(string)health?["state"]?["version"]=="combat.health-state.v2","health revision");
   var targets=health?["state"]?["targets"] as JArray;Need(targets!=null&&targets.Count==6,"six targets");
   var rows=targets.Where(t=>(string)t["id"]==TrainId).ToArray();Need(rows.Length==1,"health target");
   var h=rows[0]["health"] as JObject;BDirectAttackInput.Fields(h,"version","hp","maxHp","anchorHp","anchorMaxHp");
   Need((string)h["version"]=="health.max-hp-rebase.v1","health policy");
   Hp=checked((int)BDirectAttackInput.Nat(h,"hp",1,int.MaxValue));MaxHp=checked((int)BDirectAttackInput.Nat(h,"maxHp",1,int.MaxValue));
   Need(Hp==MaxHp&&JToken.DeepEquals(h["anchorHp"],h["hp"])&&JToken.DeepEquals(h["anchorMaxHp"],h["maxHp"]),"initial anchors");
   var initial=health["state"]["initialTargets"] as JArray;
   Need(initial!=null&&initial.Count==6&&initial.Count(t=>(string)t["id"]==TrainId)==1&&JToken.DeepEquals(initial.Single(t=>(string)t["id"]==TrainId),new JObject{["id"]=TrainId,["hp"]=Hp,["maxHp"]=MaxHp})&&health["state"]["ledger"] is JArray ledger&&ledger.Count==0,"initial health source");
   if(original!=null)Need(original.Train?.SourceVersion==OrdinaryTrainSource.InitialHealthVersion&&original.Train.TrainId==TrainId&&original.Train.PermanentMaxHp==MaxHp&&(string)s["sourceHash"]==original.Train.SourceHash&&(string)s["ordinaryOriginHash"]==original.OriginHash&&!original.Actors.Any(a=>a.OperatorId==TrainId),"original START");
   Binding=(JObject)s.DeepClone();
  }
  static void Need(bool ok,string why){if(!ok)throw new FormatException("Ordinary train initialization differs: "+why);}
 }
}
