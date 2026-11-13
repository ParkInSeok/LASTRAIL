using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Explicit graph fixtures only. Ordinary B prefab/movement/socket adapters are not supplied.
 public sealed class BWaveBoardingLocalPaths {
  public const string Version="unity.combat-b.boarding-local-path-proof.v1.20261004";
  public const string MeasuredVersion="unity.combat-b.measured-boarding-paths.v1.20261020";
  readonly JObject original;readonly Dictionary<int,(string left,string right)> sockets=new Dictionary<int,(string,string)>();
  public string Hash {get;}public bool RuntimeGeometrySupported=>false;
  BWaveBoardingLocalPaths(JObject p,string hash,BDirectAttackInput input,bool measured=false){BuffWire.Fields(p,"version","provenance","inputHash","carriages");BuffWire.Need((string)p["version"]==(measured?MeasuredVersion:Version)&&(string)p["provenance"]==(measured?"MEASURED_AUTHORED_DRAFT_NOT_RUNTIME_ADMITTED":"EXPLICIT_LOCAL_GRAPH_FIXTURE_NOT_ORDINARY_RUNTIME")&&(string)p["inputHash"]==input.OriginalHash&&BDirectAttackInput.Digest(p)==hash,"b_wave_path_proof");var ids=new HashSet<string>();foreach(var token in BuffWire.Array(p["carriages"],1,5)){var car=BuffWire.Object(token);BuffWire.Fields(car,"carriageId","slot","left","right");int slot=(int)BuffWire.Number(car["slot"],0,input.Formation.Slots.Count-1);BuffWire.Need((string)car["carriageId"]==input.Formation.Slots[slot].CarriageId&&!sockets.ContainsKey(slot),"b_wave_path_carriage");var left=Side(BuffWire.Object(car["left"]),ids);var right=Side(BuffWire.Object(car["right"]),ids);sockets.Add(slot,(left,right));}original=(JObject)p.DeepClone();Hash=hash;}
  static string Side(JObject side,HashSet<string> ids){BuffWire.Fields(side,"socketId","approachPathMm","climbPathMm");string id=BuffWire.Text(side["socketId"]);BuffWire.Need(ids.Add(id),"b_wave_socket_duplicate");var approach=Path(side["approachPathMm"]);var climb=Path(side["climbPathMm"]);BuffWire.Need(JToken.DeepEquals(approach.Last,climb.First),"b_wave_disconnected_path");return id;}
  static JArray Path(JToken value){var p=BuffWire.Array(value,2,64);foreach(var token in p){var point=BuffWire.Array(token,3,3);foreach(var coordinate in point)BuffWire.Need(coordinate.Type==JTokenType.Integer&&(long)coordinate>=-1000000000&&(long)coordinate<=1000000000,"b_wave_real_mm");}return p;}
  public static BWaveBoardingLocalPaths Read(JObject p,string independentlyTrustedHash,BDirectAttackInput input)=>new BWaveBoardingLocalPaths((JObject)(p??throw new ArgumentNullException(nameof(p))).DeepClone(),independentlyTrustedHash,input??throw new ArgumentNullException(nameof(input)));
  internal static BWaveBoardingLocalPaths ReadMeasured(JObject p,string hash,BDirectAttackInput input)=>new BWaveBoardingLocalPaths(p,hash,input,true);
  internal BWaveBoardingLocalPaths(string geometryHash,IReadOnlyList<string> hosts,JArray cars){
   var ids=new HashSet<string>();var rows=new JArray();
   foreach(JObject car in cars){int slot=checked((int)(long)car["carOrder"]);BuffWire.Need(slot>=0&&slot<hosts.Count&&!sockets.ContainsKey(slot),"ordinary_boarding_host");
    JObject Bound(string name){var side=(JObject)car["sockets"][name];var p=new JObject{["socketId"]=hosts[slot]+":"+(string)side["socketId"],["approachPathMm"]=side["approachPathMm"].DeepClone(),["climbPathMm"]=side["climbPathMm"].DeepClone()};Side(p,ids);return p;}
    var left=Bound("left");var right=Bound("right");sockets.Add(slot,((string)left["socketId"],(string)right["socketId"]));rows.Add(new JObject{["carriageId"]=hosts[slot],["slot"]=slot,["left"]=left,["right"]=right});
   }
   BuffWire.Need(sockets.Count==hosts.Count,"ordinary_boarding_host_coverage");original=new JObject{["version"]="ordinary.boarding-path-binding.v1.20261107",["geometryHash"]=geometryHash,["carriages"]=rows};Hash=BDirectAttackInput.Digest(original);
  }
  public JObject Capture()=>(JObject)original.DeepClone();
  internal bool HasRoute(int slot)=>sockets.ContainsKey(slot);
  internal string Socket(int slot,int side)=>side==0?sockets[slot].left:sockets[slot].right;
 }
}
