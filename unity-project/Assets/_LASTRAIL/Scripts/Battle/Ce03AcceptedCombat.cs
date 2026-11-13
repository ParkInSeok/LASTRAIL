using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Contract validation only. This cannot attest a runner, import assets, or enter
// combat. The caller must obtain the response from its authenticated local host.
public sealed class Ce03AcceptedCombat {
 public string RunId{get;}public uint Seed{get;}public string SnapshotHash{get;}
 public string OriginalJson{get;}public bool IsPlayer{get;}
 public Ce03AcceptedCombat(JObject response,JObject prepared,string owner,string quoteId){
  Ce03CommandContract.Equal(response,"scope","DISPOSABLE_LOCAL");Ce03CommandContract.Equal(response,"startStatus","STARTED");
  if(response["accepted"]?.Type!=JTokenType.Boolean||!(bool)response["accepted"]||(string)response["operationPreviewId"]!=quoteId||string.IsNullOrWhiteSpace(quoteId))throw new FormatException("Unaccepted original quote.");
  RunId=(string)response["runId"];if(string.IsNullOrWhiteSpace(RunId))throw new FormatException();
  var combat=response["combat"] as JObject;Ce03CommandContract.Fields(combat,"schemaVersion","profile","profileSha256","contentManifest","contentSha256","seed","snapshot","snapshotSha256");IsPlayer=(string)combat["schemaVersion"]=="ce03-accepted-player-combat-v1";Ce03CommandContract.Equal(combat,"schemaVersion",IsPlayer?"ce03-accepted-player-combat-v1":"ce03-accepted-combat-v1");
  foreach(var pair in new[]{new[]{"profile","profileSha256"},new[]{"contentManifest","contentSha256"},new[]{"snapshot","snapshotSha256"}}){if(combat[pair[0]] is not JObject||Ce03CommandContract.Sha(Ce03CommandContract.Canonical(combat[pair[0]]))!=(string)combat[pair[1]])throw new FormatException("P/C/S mismatch.");}
  var profile=combat["profile"] as JObject;Ce03CommandContract.Fields(profile,"schemaVersion","baseProfile","commandContract");string commandVersion=(string)profile["commandContract"]?["version"];Ce03CommandContract.Equal(profile,"schemaVersion",IsPlayer&&commandVersion=="ce03-command-contract-v3"?"ce03-isolated-command-profile-r9":IsPlayer&&commandVersion=="ce03-command-contract-v2"?"ce03-isolated-command-profile-r8":"ce03-isolated-command-profile-r5");
  var basis=profile["baseProfile"] as JObject;Ce03CommandContract.Equal(basis,"schemaVersion","ce03-isolated-profile-r4");
  var draft=prepared["draft"] as JObject;if(draft==null)throw new FormatException();
  Ce03CommandContract.Fields(basis,"schemaVersion","profileId","engineProfileVersion","effectiveStatsVersion","roundingVersion","simulationVersion","template","executionPolicy","sourceFiles","executionEvidence");
  foreach(var name in new[]{"profileId","engineProfileVersion","effectiveStatsVersion","roundingVersion","simulationVersion","template","executionPolicy","sourceFiles"})Same(basis[name],draft[name]);
  Same(profile["commandContract"],prepared["commandContract"]);var commands=new Ce03CommandContract(profile["commandContract"] as JObject);commands.ValidatePrepared(prepared,owner,commands.Revision);
  var content=combat["contentManifest"] as JObject;
  if(IsPlayer){
   Ce03PlayerAssetContract.Validate(prepared["playerAssetBinding"] as JObject,prepared["ownedSelection"] as JObject);
   Ce03CommandContract.Fields(content,"schemaVersion","version","profileSha256","resolver","playerAssetBinding");Ce03CommandContract.Equal(content,"schemaVersion","ce03-player-content-v1");Ce03CommandContract.Equal(content,"version","ce03.player-content.v1.20261022");Same(content["playerAssetBinding"],prepared["playerAssetBinding"]);
  }else if(prepared.Property("presentationManifest")!=null){
   Ce03CommandContract.Fields(content,"schemaVersion","version","profileSha256","resolver","legacyBindings","presentationManifest");Ce03CommandContract.Equal(content,"schemaVersion","ce03-isolated-presentation-content-r6");
   new Ce03PresentationManifest(content["presentationManifest"] as JObject);Same(content["presentationManifest"],prepared["presentationManifest"]);Same(content["legacyBindings"],draft["bindings"]);
   Ce03CommandContract.Equal(content,"version","ce03.approved-presentation.20260912.001");
  }else{Ce03CommandContract.Fields(content,"schemaVersion","version","profileSha256","resolver","bindings");Ce03CommandContract.Equal(content,"schemaVersion","ce03-isolated-content-r4");Same(content["bindings"],draft["bindings"]);Same(content["version"],basis["profileId"]);}
  Same(content["profileSha256"],combat["profileSha256"]);
  var resolver=content["resolver"] as JObject;if(IsPlayer){Ce03CommandContract.Fields(resolver,"mode");Ce03CommandContract.Equal(resolver,"mode","UNITY_SERIALIZED_PLAYER_BINDING");}else{if(prepared.Property("playerAssetBinding")!=null)throw new FormatException("Legacy START cannot authorize Player assets.");Ce03CommandContract.Fields(resolver,"mode","playerBuildAllowed");Ce03CommandContract.Equal(resolver,"mode","UNITY_EDITOR_ASSET_DATABASE");if(resolver["playerBuildAllowed"]?.Type!=JTokenType.Boolean||(bool)resolver["playerBuildAllowed"])throw new FormatException("Unsupported player resolver.");}
  var snapshot=combat["snapshot"] as JObject;var snapshotFields=new[]{"version","accountId","runId","operationPreviewId","prepareSha256","profileSha256","contentSha256","seed","ownedSelection","carriageOwnership","commandContract","attestation"};Ce03CommandContract.Fields(snapshot,IsPlayer?snapshotFields.Concat(new[]{"playerAssetBinding"}).ToArray():snapshotFields);Ce03CommandContract.Equal(snapshot,"version",IsPlayer?"ce03-player-snapshot-v1":"ce03-local-snapshot-v1");if(IsPlayer)Same(snapshot["playerAssetBinding"],prepared["playerAssetBinding"]);
  if((string)snapshot["accountId"]!=owner||(string)snapshot["runId"]!=RunId||(string)snapshot["operationPreviewId"]!=quoteId)throw new FormatException("Snapshot owner/run/quote mismatch.");
  foreach(var field in new[]{"ownedSelection","carriageOwnership","commandContract","prepareSha256"})Same(snapshot[field],prepared[field]);
  foreach(var field in new[]{"profileSha256","contentSha256","seed"})Same(snapshot[field],combat[field]);
  var attestation=snapshot["attestation"] as JObject;Ce03CommandContract.Equal(attestation,"status","ATTESTATION_VERIFIED");Same(attestation["executionEvidence"],basis["executionEvidence"]);if((string)attestation["draftSha256"]!=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(draft)))throw new FormatException("Attested draft mismatch.");
  var seed=combat["seed"] as JObject;Ce03CommandContract.Fields(seed,"version","generation","simulation","zeroNormalization","byteOrder","lifetime","purpose","issuedAt","value");Ce03CommandContract.Equal(seed,"version","combat-seed-local-v1");Ce03CommandContract.Equal(seed,"generation","WEB_CRYPTO_UINT32");Ce03CommandContract.Equal(seed,"simulation","XORSHIFT32_13_17_5_V1");Ce03CommandContract.Equal(seed,"byteOrder","LITTLE_ENDIAN");Ce03CommandContract.Equal(seed,"lifetime","ORIGINAL_RUN");Ce03CommandContract.Equal(seed,"purpose","LOCAL_COMBAT_SIMULATION");
  if(Ce03CommandContract.Positive(seed,"zeroNormalization")!=1831565813||Ce03CommandContract.Positive(seed,"issuedAt")<1||seed["value"]?.Type!=JTokenType.Integer||(long)seed["value"]<0||(long)seed["value"]>uint.MaxValue)throw new FormatException("Invalid original seed.");Seed=(uint)seed["value"];SnapshotHash=(string)combat["snapshotSha256"];OriginalJson=Ce03CommandContract.Canonical(response);
 }
 static void Same(JToken a,JToken b){if(a==null||b==null||!JToken.DeepEquals(a,b))throw new FormatException("Frozen data changed.");}
}
}

