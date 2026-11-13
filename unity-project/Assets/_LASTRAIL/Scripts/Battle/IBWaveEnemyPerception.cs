namespace LastRail.Battle {
 // Read-only lifetime projection borrowed for one native stage. HP and clock stay in Session.
 internal interface IBWaveEnemyPerception {
  bool Targetable(string id);bool AttackEligible(string id);int Slot(string id);string CategoryAtImpact(string id);
 }
 internal interface IBWaveSpatialPerception {
  bool HasSpatial {get;}bool TryGeometry(string source,string target,long tick,out Newtonsoft.Json.Linq.JObject frame);string SpatialTarget(string id);bool Protection(string actor,string target);
 }
}
