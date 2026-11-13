using System;
using System.IO;
using System.Linq;
using System.Text;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Separate local START identity. Never projected into the public one-field
// PendingRunOperation or its response-size limit. Contains no bearer credential.
public sealed class Ce03OriginalStart {
 public const int MaximumBytes=8*1024*1024;
 readonly JObject value;readonly Ce03AcceptedCombat accepted;string modelConfiguration;
 public string Owner=>(string)value["owner"];
 public string Origin=>(string)value["origin"];
 public string RunId=>accepted.RunId;
 public string Key=>(string)value["key"];
 public uint Seed=>accepted.Seed;
 public string SnapshotHash=>accepted.SnapshotHash;
 public bool IsPlayer=>accepted.IsPlayer;
 public Func<BasicCombatSimulation> CreateVerifiedPlayerFactory(CombatPlayerAssetBinding binding){if(!IsPlayer||binding==null)throw new InvalidOperationException("A Player-bound accepted START is required.");var p=Prepare();var formation=Ce03PlayerAssetContract.Validate(p["playerAssetBinding"] as JObject,p["ownedSelection"] as JObject);using(var lease=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,formation)){lease.ReadAssets();}return CreateModel;}
 public string Json=>Ce03CommandContract.Canonical(value);
 public JObject Copy()=>(JObject)value.DeepClone();
 public JObject Prepare()=>(JObject)value["prepared"].DeepClone();
 public Ce03OriginalStart(string raw,string expectedOwner,Uri expectedOrigin){
  if(raw==null||Encoding.UTF8.GetByteCount(raw)>MaximumBytes)throw new FormatException("Local START size exceeded.");
  using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None,MaxDepth=64}){value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});if(reader.Read()||value.Descendants().Any(t=>t.Type==JTokenType.Comment))throw new FormatException("Invalid original START JSON.");}
  Ce03CommandContract.Fields(value,"version","owner","origin","key","body","responseRaw","prepared");Ce03CommandContract.Equal(value,"version","ce03-original-start-v1");
  if(expectedOrigin==null||expectedOrigin.Scheme!="http"||expectedOrigin.Host!="127.0.0.1"||expectedOrigin.AbsolutePath!="/"||expectedOrigin.Query!=""||expectedOrigin.Fragment!=""||expectedOrigin.UserInfo!=""||string.IsNullOrWhiteSpace(expectedOwner)||Owner!=expectedOwner||Origin!=expectedOrigin.GetLeftPart(UriPartial.Authority)||!RunRequestKey.IsSupported(Key))throw new FormatException("Original START owner/origin/key mismatch.");
  var body=value["body"] as JObject;Ce03CommandContract.Fields(body,"operationPreviewId","expectedSupportRevision","prepareSha256");
  var prepared=value["prepared"] as JObject;var commands=new Ce03CommandContract(prepared?["commandContract"] as JObject);commands.ValidatePrepared(prepared,Owner,commands.Revision);
  if((long?)body["expectedSupportRevision"]!=commands.Revision||!JToken.DeepEquals(body["prepareSha256"],prepared["prepareSha256"]))throw new FormatException("Original START preparation mismatch.");
  if(value["responseRaw"]?.Type!=JTokenType.String)throw new FormatException("Original response missing.");
  JObject response;using(var reader=new JsonTextReader(new StringReader((string)value["responseRaw"])){DateParseHandling=DateParseHandling.None,MaxDepth=64}){response=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});if(reader.Read()||response.Descendants().Any(t=>t.Type==JTokenType.Comment))throw new FormatException("Invalid response JSON.");}
  accepted=new Ce03AcceptedCombat(response,prepared,Owner,(string)body["operationPreviewId"]);
 }
 public static Ce03OriginalStart FromReceipt(string owner,Uri origin,JObject receipt){
  var request=receipt?["request"] as JObject;if((string)request?["kind"]!="start"||receipt["responseRaw"]?.Type!=JTokenType.String||request["prepared"] is not JObject)throw new FormatException("Retained full START required.");
  return new Ce03OriginalStart(Ce03CommandContract.Canonical(new JObject{["version"]="ce03-original-start-v1",["owner"]=owner,["origin"]=origin.GetLeftPart(UriPartial.Authority),["key"]=request["key"].DeepClone(),["body"]=request["body"].DeepClone(),["responseRaw"]=receipt["responseRaw"].DeepClone(),["prepared"]=request["prepared"].DeepClone()}),owner,origin);
 }
 // Actual asset/source verification is performed by the Editor consumer before
 // this factory. Seed comes only from the verified original accepted envelope.
 internal BasicCombatSimulation CreateModel(){var p=Prepare();var c=new Ce03CommandContract((JObject)p["commandContract"]);var model=c.CreateLocalTest(p,Owner,c.Revision,Seed);model.BindEffectRun(RunId,(string)JObject.Parse((string)value["responseRaw"])["combat"]["profileSha256"]);return model;}
 internal void ValidateModel(BasicCombatSimulation model){if(modelConfiguration==null)modelConfiguration=(string)JObject.Parse(CreateModel().CaptureLocalState())["configurationSha256"];if(model==null||model.LocalOwnerAccount!=Owner||(string)JObject.Parse(model.CaptureLocalState())["configurationSha256"]!=modelConfiguration)throw new InvalidOperationException("Model differs from original accepted seed/configuration.");}
#if UNITY_EDITOR
 public Func<BasicCombatSimulation> CreateVerifiedEditorFactory(){Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(Prepare()["presentationManifest"] as JObject));return CreateModel;}
 public BasicCombatSimulation CreateVerifiedEditorModel(){Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(Prepare()["presentationManifest"] as JObject));return CreateModel();}
#endif
}
}

