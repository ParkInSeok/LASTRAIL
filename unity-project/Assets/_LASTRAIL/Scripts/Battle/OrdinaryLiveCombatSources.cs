using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Pure projections of pinned native authoring. Numeric/event authorities and entity lives remain borrowed.
 internal sealed partial class OrdinaryLiveCombatSources {
  readonly OrdinaryFiveCombatFactory factory;
  readonly JObject definition,layout;
  readonly Dictionary<string,JObject> entities;
  readonly Dictionary<string,string> authoring=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly Func<long,long> front;
  readonly Func<string,long,long> generation;
  readonly Func<string,long,string> gunnerMotion;
  readonly Func<string,long,string> gunnerNoTargetStatus;
  readonly string definitionHash;
  readonly long initialFront;
  long latestTick=-1,latestFront;
  Func<string,long,long[]> entityOffsets;
  OrdinaryBoardingExecution boarding;
  internal void BindBoarding(OrdinaryBoardingExecution owner){if(boarding!=null||owner==null)throw new InvalidOperationException("One boarding owner binding required.");boarding=owner;}
  internal void BindEntityOffsets(Func<string,long,long[]> value){if(entityOffsets!=null||value==null)throw new InvalidOperationException("One owned motion binding required.");entityOffsets=value;}
  internal JArray CaptureBlockers(long tick)=>Blockers(FrontAt(tick));
  internal OrdinaryNodeBasicSources Basic {get;}
  internal OrdinaryNodeGunnerSources Gunner {get;}
  internal OrdinaryLiveCombatSources(OrdinaryFiveCombatFactory factory,string definitionJson,Func<long,long> frontAtTick,
   Func<string,long,long> entityGeneration,Func<string,long,CombatExactValue> effectivePeriod,
   Func<string,string,long,JObject> basicAttack,Func<string,CombatExactValue,long,JObject> gunnerAttack,
   Func<string,long,JObject> defense,Func<string,long,string> kyleMode,
   Func<string,long,JObject> interception,Func<string,long,string> gunnerMotion,Func<string,long,string> gunnerNoTargetStatus){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));front=frontAtTick??throw new ArgumentNullException(nameof(frontAtTick));generation=entityGeneration??throw new ArgumentNullException(nameof(entityGeneration));this.gunnerMotion=gunnerMotion??throw new ArgumentNullException(nameof(gunnerMotion));
   this.gunnerNoTargetStatus=gunnerNoTargetStatus??throw new ArgumentNullException(nameof(gunnerNoTargetStatus));
   Need(factory.NodeInput!=null&&factory.Attempt!=null,"Original node factory required");
   definition=ReadDefinition(definitionJson,factory.NodeInput.Source.GeometryBinding?.Definition?.DefinitionHash);definitionHash=BasicCombatSimulation.Digest(definitionJson);
   Need((string)definition["definitionId"]==factory.NodeInput.Source.GeometryBinding.Definition.DefinitionId,"Geometry definition ID differs");layout=(JObject)definition["source"]["liveLayout"];initialFront=(long)definition["source"]["obstacle"]["initialFront"];
   entities=((JArray)definition["entities"]).OfType<JObject>().ToDictionary(e=>(string)e["authoringId"],StringComparer.Ordinal);
   foreach(var actor in factory.Original.Actors){Need(entities.TryGetValue(actor.OperatorId,out var row)&&(string)row["role"]=="OPERATOR"&&(string)row["definitionRef"]==actor.OperatorId,"Original actor geometry missing");Need((long?)layout[actor.OperatorId]["formationSlot"]==actor.Slot,"Original formation and authored slot differ");authoring.Add(actor.OperatorId,actor.OperatorId);}
   Need(factory.NodeInput.Source.Enemies.Gunners.Count==2,"Two original gunners required");
   for(int i=0;i<2;i++){string id="Gunner"+i;Need(entities.TryGetValue(id,out var row)&&(string)row["role"]=="ENEMY"&&(string)row["definitionRef"]=="enemy_spike_cart_gunner"&&(long?)layout[id]["gunnerOrdinal"]==i,"Original gunner geometry missing");authoring.Add(factory.NodeInput.Source.Enemies.Gunners[i].InstanceId,id);}
   if(basicAttack==null||gunnerAttack==null||defense==null||interception==null)throw new ArgumentNullException("Explicit numeric and interception authorities required");
   JObject Attack(JObject value){Need(value!=null,"Original attack proof missing");var copy=(JObject)value.DeepClone();CombatProjectileAttackSnapshot.Capture(copy);return copy;}
   JObject Defense(string id,long tick){var value=defense(id,tick);Need(value!=null,"Original defense proof missing");var copy=(JObject)value.DeepClone();if((string)copy["version"]==CombatProjectileAttackSnapshot.MitigationArrivalVersion){BDirectAttackInput.Fields(copy,"version","defense","effects","weakening","reductions","mitigationRateReduction");Need(copy["mitigationRateReduction"] is JArray,"Explicit mitigation source required");}else{BDirectAttackInput.Fields(copy,"version","defense","effects","weakening","reductions");Need((string)copy["version"]==CombatProjectileAttackSnapshot.Version,"Defense version differs");}return copy;}
   Basic=new OrdinaryNodeBasicSources(ObserveBasic,effectivePeriod,(actor,target,tick)=>Attack(basicAttack(actor,target,tick)),Defense,kyleMode,KyleContact);
   Gunner=new OrdinaryNodeGunnerSources(ObserveGunner,(actor,raw,tick)=>Attack(gunnerAttack(actor,raw,tick)),Defense,interception,factory.Original.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version?effectivePeriod:null);
  }
  static void Need(bool ok,string reason){if(!ok)throw new ArgumentException("Ordinary live geometry: "+reason);}
  static long[] Point(JToken token){Need(token is JArray a&&a.Count==3&&a.All(v=>v.Type==JTokenType.Integer),"Integer millimeter point required");var p=token.Select(v=>(long)v).ToArray();Need(p.All(v=>v>=-1000000000&&v<=1000000000),"Point outside supported bounds");return p;}
  static bool IdentityBasis(JToken token)=>JToken.DeepEquals(token,new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1)))||token is JArray rows&&rows.Count==3&&Enumerable.Range(0,3).All(i=>rows[i] is JArray row&&row.Count==3&&Enumerable.Range(0,3).All(j=>(double?)row[j]==(i==j?1d:0d)));
  internal static JObject ReadDefinition(string bytes,string pinnedHash){
   Need(bytes!=null&&bytes.Length<=524288&&pinnedHash!=null&&BasicCombatSimulation.Digest(bytes)==pinnedHash,"Original geometry raw bytes/hash required");var value=JObject.Parse(bytes);
   BDirectAttackInput.Fields(value,"version","definitionId","units","millimetersPerUnit","axis","coordinateSpace","source","entities");
   Need((string)value["version"]==OrdinaryGeometryReference.Version&&(string)value["units"]=="MILLIMETERS"&&(long?)value["millimetersPerUnit"]==1000&&(string)value["axis"]=="X_FORWARD_Y_UP_Z_SIDE"&&(string)value["coordinateSpace"]=="BOUND_FRAME_LOCAL","Unsupported geometry coordinate contract");
   Need(value["source"] is JObject source&&(string)source["scope"]=="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION"&&source["liveLayout"] is JObject,"Explicit local authoring provenance required");
   var layout=(JObject)value["source"]["liveLayout"];Need(value["source"]["obstacle"]?["initialFront"]?.Type==JTokenType.Integer,"Initial front required");long initial=(long)value["source"]["obstacle"]["initialFront"];
   Need(value["entities"] is JArray all&&all.Count>0&&all.Count<=128,"Geometry entity set required");var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject row in (JArray)value["entities"]){
    BDirectAttackInput.Fields(row,"authoringId","role","definitionRef","framePath","assetPath","assetHash","anchors","shapes");string id=(string)row["authoringId"];Need(!string.IsNullOrWhiteSpace(id)&&ids.Add(id),"Duplicate geometry entity");
    Need(layout[id] is JObject&&IdentityBasis(layout[id]["basis"]),"Only authored axis-aligned untranslated basis supported");var p=Point(layout[id]["originMm"]);var relative=Point(layout[id]["relativeToInitialFrontMm"]);Need(p[0]-initial==relative[0]&&p[1]==relative[1]&&p[2]==relative[2],"Measured initial frame differs");
    Need(row["anchors"] is JArray&&row["shapes"] is JArray,"Geometry elements required");
    foreach(JObject anchor in (JArray)row["anchors"])Point(anchor["positionMm"]);
    foreach(JObject shape in (JArray)row["shapes"]){Point(shape["centerMm"]);string kind=(string)shape["kind"];Need(kind=="BOX"||kind=="CAPSULE_Y","Unsupported native shape");Need((string)shape["role"]=="HIT_VOLUME"||(string)shape["role"]=="OCCLUDER","Registered shape role required");if(kind=="BOX"){BDirectAttackInput.Fields(shape,"id","role","kind","centerMm","sizeMm");Need(Point(shape["sizeMm"]).All(n=>n>0),"Positive box size required");}else{BDirectAttackInput.Fields(shape,"id","role","kind","centerMm","radiusMm","heightMm");Need(shape["radiusMm"]?.Type==JTokenType.Integer&&shape["heightMm"]?.Type==JTokenType.Integer&&(long)shape["radiusMm"]>0&&(long)shape["heightMm"]>=2*(long)shape["radiusMm"],"Valid native capsule required");}}
   }
   Need(layout.Count==ids.Count,"Unexpected live frame");return value;
  }
  internal long[] Foot(string id,long tick)=>boarding?.Contains(id)==true?Point(boarding.Target(id,tick,FrontAt(tick))["feetMm"]):World(authoring[id],Anchor(authoring[id],"FOOT"),FrontAt(tick));
  internal long[] CombatBody(string id,long tick)=>World(authoring[id],Anchor(authoring[id],"HIT"),FrontAt(tick));
  internal long FrontAt(long tick){Need(tick>=0,"Absolute logical tick required");long value=front(tick);Need(value>=0&&value<=1000000000,"Front outside supported bounds");if(tick==latestTick)Need(value==latestFront,"Geometry changed within logical tick");if(tick>latestTick){latestTick=tick;latestFront=value;}return value;}
  long Generation(string id,long tick){long value=generation(id,tick);Need(value>=1,"Original entity generation missing");return value;}
  long[] Origin(string id,long currentFront){var point=Point(layout[id]["originMm"]);if((string)entities[id]["role"]!="WORLD")point[0]=checked(point[0]+currentFront-initialFront);if(entityOffsets!=null){string runtime=authoring.FirstOrDefault(p=>p.Value==id).Key;if(runtime!=null){var delta=entityOffsets(runtime,latestTick);for(int i=0;i<3;i++)point[i]=checked(point[i]+delta[i]);}}return point;}
  long[] World(string id,JToken local,long currentFront){var origin=Origin(id,currentFront);var p=Point(local);return Enumerable.Range(0,3).Select(i=>checked(origin[i]+p[i])).ToArray();}
  JToken Anchor(string id,string role){var rows=((JArray)entities[id]["anchors"]).Where(a=>(string)a["role"]==role).ToArray();Need(rows.Length==1,"One measured "+role+" anchor required");return rows[0]["positionMm"];}
  JObject Capsule(string id){var rows=((JArray)entities[id]["shapes"]).OfType<JObject>().Where(s=>(string)s["kind"]=="CAPSULE_Y"&&(string)s["role"]=="HIT_VOLUME").ToArray();Need(rows.Length==1,"One native hit capsule required");return rows[0];}
  // Slot zero is the front enemy car; existing Engine/Combat/Gun remain ordered behind it.
  long CarOrder(string id)=>factory.Original.Rail.LocalFiveCar&&boarding!=null&&factory.Original.Actors.Any(a=>a.OperatorId==id)?boarding.PartyCarOrder(id):(long)layout[id]["carOrder"];
  long Slot(string id)=>(string)entities[id]["role"]=="ENEMY"?0:checked(CarOrder(id)+1);
  internal JObject CaptureGeometry(long tick){long currentFront=FrontAt(tick);return new JObject{["definitionHash"]=definitionHash,["tick"]=tick,["frontMm"]=currentFront,["entities"]=new JArray(entities.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new JObject{["authoringId"]=p.Key,["originMm"]=new JArray(Origin(p.Key,currentFront))})),["blockers"]=Blockers(currentFront)};}
  JArray Blockers(long currentFront){var rows=new JArray();foreach(var pair in entities)foreach(JObject shape in (JArray)pair.Value["shapes"]){if((string)shape["role"]!="OCCLUDER"||pair.Key=="FallingObstacle")continue;Need((string)shape["kind"]=="BOX","Box occluder required");rows.Add(new JObject{["id"]=pair.Key+":"+(string)shape["id"],["centerMm"]=new JArray(World(pair.Key,shape["centerMm"],currentFront)),["sizeMm"]=shape["sizeMm"].DeepClone()});}return rows;}
  JObject Context(long tick)=>new JObject{["tick"]=tick,["revision"]=checked(tick+1),["nodeId"]=factory.Attempt.NodeId,["snapshotHash"]=factory.Attempt.SnapshotHash,["attemptGeneration"]=factory.Attempt.Generation};
  internal JObject ObserveBasic(string actor,long tick){
   Need(authoring.ContainsKey(actor)&&factory.Original.Actors.Any(a=>a.OperatorId==actor),"Unknown original actor");long currentFront=FrontAt(tick);var original=factory.Original.Basic.Profiles.Single(p=>p.OperatorId==actor);var profile=new JObject{["operatorId"]=actor,["abilityId"]=original.AbilityId,["launchSource"]=original.LaunchSource,["ballistics"]=new JObject{["speedMmPerSecond"]=original.SpeedMmPerSecond,["radiusMm"]=original.RadiusMm,["maxDistanceMm"]=original.MaximumFlightDistanceMm,["adjacentCars"]=original.AdjacentCars,["maxFlightUs"]=checked((original.FlightNumeratorMs*1000+original.FlightDenominator-1)/original.FlightDenominator)}};
   var result=Context(tick);var basis=new JObject{["positionMm"]=new JArray(0,0,0),["rotation"]=new JArray(0,0,0,1)};
   result["version"]="mira.judgement-observation.v1.20261015";result["profile"]=profile;result["profileHash"]=BDirectAttackInput.Digest(profile);result["layoutHash"]=definitionHash;result["basis"]=basis;result["basisHash"]=BDirectAttackInput.Digest(basis);
   result["source"]=new JObject{["id"]=actor,["generation"]=Generation(actor,tick),["executionEntityId"]=actor,["slot"]=Slot(actor),["domain"]="RAIL",["muzzleMm"]=new JArray(World(actor,Anchor(actor,"EMISSION"),currentFront))};
   var targets=new JArray();foreach(var enemy in factory.NodeInput.Source.Enemies.Gunners){string id=authoring[enemy.InstanceId];var capsule=Capsule(id);var feet=World(id,capsule["centerMm"],currentFront);Need((long)capsule["heightMm"]%2==0,"Exact integer capsule feet required");feet[1]-=(long)capsule["heightMm"]/2;targets.Add(new JObject{["id"]=enemy.InstanceId,["generation"]=Generation(enemy.InstanceId,tick),["slot"]=Slot(id),["domain"]="RAIL",["eligible"]=true,["anchorMm"]=new JArray(World(id,Anchor(id,"HIT"),currentFront)),["feetMm"]=new JArray(feet),["heightMm"]=capsule["heightMm"].DeepClone(),["radiusMm"]=capsule["radiusMm"].DeepClone()});}
   if(boarding!=null)foreach(string id in factory.EnemyIds.Where(boarding.Contains))if(boarding.Registered(id))targets.Add(boarding.Target(id,tick,currentFront));
   result["targets"]=targets;result["blockers"]=Blockers(currentFront);return result;
  }
  internal bool HasKyleContactAnchor=>((JArray)entities["op_kyle"]["anchors"]).Count(a=>(string)a["role"]=="MELEE_CONTACT")==1;
  internal bool KyleContact(string target,long tick){
   if(!HasKyleContactAnchor)throw new NotSupportedException("Measured Kyle fist contact anchor required.");
   var observation=ObserveBasic("op_kyle",tick);var row=observation["targets"].SingleOrDefault(t=>(string)t["id"]==target);
   if(row==null||(bool?)row["eligible"]!=true||(long)row["slot"]!=(long)observation["source"]["slot"]||boarding?.Contains(target)!=true||boarding.Category(target)!="ROOFTOP")return false;
   var profile=factory.Original.Basic.Profiles.Single(p=>p.OperatorId=="op_kyle");var body=CombatBody("op_kyle",tick);var hit=Point(row["anchorMm"]);
   System.Numerics.BigInteger distance=0;for(int i=0;i<3;i++){System.Numerics.BigInteger delta=(System.Numerics.BigInteger)body[i]-hit[i];distance+=delta*delta;}
   if(distance>(System.Numerics.BigInteger)profile.MeleeMaximumDistanceMm.Value*profile.MeleeMaximumDistanceMm.Value)return false;
   var fist=World("op_kyle",Anchor("op_kyle","MELEE_CONTACT"),FrontAt(tick));var feet=Point(row["feetMm"]);
   return MiraSweepKernel.Capsule(fist,fist,feet,feet,(long)row["heightMm"],(long)row["radiusMm"],profile.MeleeContactRadiusMm.Value).HasValue
    &&!((JArray)observation["blockers"]).Any(b=>MiraSweepKernel.Box(body,hit,Point(b["centerMm"]),Point(b["centerMm"]),Point(b["sizeMm"]),0).HasValue);
  }
  internal KyleTauntPolicy.Candidate ObserveTaunt(string caster,string enemy,string currentTarget,long tick,int order){
   var body=CombatBody(caster,tick);var hit=CombatBody(enemy,tick);var blockers=Blockers(FrontAt(tick));
   System.Numerics.BigInteger square=0;for(int i=0;i<3;i++){System.Numerics.BigInteger d=(System.Numerics.BigInteger)body[i]-hit[i];square+=d*d;}
   bool los=!blockers.Any(b=>MiraSweepKernel.Box(body,hit,Point(b["centerMm"]),Point(b["centerMm"]),Point(b["sizeMm"]),0).HasValue);
   bool threat=currentTarget!=null&&factory.Original.Actors.Any(a=>a.OperatorId==currentTarget)&&CarOrder(currentTarget)==CarOrder(caster);
   return new KyleTauntPolicy.Candidate(enemy,Generation(enemy,tick),square,order,threat,los,false);
  }
  internal JObject ObserveGunner(string gunner,long tick){
   Need(authoring.ContainsKey(gunner)&&factory.NodeInput.Source.Enemies.Gunners.Any(g=>g.InstanceId==gunner),"Unknown original gunner");long currentFront=FrontAt(tick);string id=authoring[gunner];var muzzle=World(id,Anchor(id,"EMISSION"),currentFront);var blockers=Blockers(currentFront);var result=Context(tick);string motion=gunnerMotion(gunner,tick);Need(motion=="STATIONARY"||motion=="MOVING","Original gunner motion required");
   result["source"]=new JObject{["id"]=gunner,["generation"]=Generation(gunner,tick),["executorId"]=gunner,["muzzleMm"]=new JArray(muzzle),["motionStatus"]=motion};var targets=new JArray();
   foreach(var actor in factory.Original.Actors){var hit=World(actor.OperatorId,Anchor(actor.OperatorId,"HIT"),currentFront);bool los=!blockers.Any(b=>MiraSweepKernel.Box(muzzle,hit,Point(b["centerMm"]),Point(b["centerMm"]),Point(b["sizeMm"]),0).HasValue);targets.Add(new JObject{["id"]=actor.OperatorId,["generation"]=Generation(actor.OperatorId,tick),["carOrder"]=CarOrder(actor.OperatorId),["pointMm"]=new JArray(hit),["lineOfSight"]=los});}
   result["targets"]=targets;result["noTargetStatus"]=gunnerNoTargetStatus(gunner,tick);return result;
  }
 }
}
