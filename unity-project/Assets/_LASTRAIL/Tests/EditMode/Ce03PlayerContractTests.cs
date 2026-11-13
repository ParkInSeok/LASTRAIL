using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class Ce03PlayerContractTests
{
    // Schema compatibility fixture only: never used as authenticated admission or a live model.
    public static (JObject Prepared,JObject Response) Fixture()
    {
        var source=Ce03R6RecoveryChecks.ReadJson(File.ReadAllText("QA/Ce03CommandIntegration/synthetic-r6-transport.json"));
        var p=(JObject)source["prepare"].DeepClone();var r=(JObject)source["startResponse"].DeepClone();p.Remove("presentationManifest");
        p["playerAssetBinding"]=new JObject{["version"]="ce03.player-asset-binding.v1.20261022",["manifestVersion"]="combat-player-assets-v1",["manifestSha256"]="59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c",["unityVersion"]="6000.3.22f1",["configurationPath"]="Assets/_LASTRAIL/Resources/CombatPresentation/approved.asset",["formation"]=new JArray("op_kyle","op_reina","op_eve","op_sera","op_mira"),["admissionGranted"]=false};
        var hash=new JObject();foreach(var key in new[]{"ownedSelection","carriageOwnership","draft","commandContract","playerAssetBinding"})hash[key]=p[key].DeepClone();p["prepareSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(hash));
        var c=(JObject)r["combat"];c["schemaVersion"]="ce03-accepted-player-combat-v1";
        c["contentManifest"]=new JObject{["schemaVersion"]="ce03-player-content-v1",["version"]="ce03.player-content.v1.20261022",["profileSha256"]=c["profileSha256"].DeepClone(),["resolver"]=new JObject{["mode"]="UNITY_SERIALIZED_PLAYER_BINDING"},["playerAssetBinding"]=p["playerAssetBinding"].DeepClone()};
        c["contentSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(c["contentManifest"]));
        c["snapshot"]["version"]="ce03-player-snapshot-v1";c["snapshot"]["playerAssetBinding"]=p["playerAssetBinding"].DeepClone();c["snapshot"]["prepareSha256"]=p["prepareSha256"].DeepClone();c["snapshot"]["contentSha256"]=c["contentSha256"].DeepClone();
        c["snapshotSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(c["snapshot"]));return(p,r);
    }
    static void Accept(JObject p,JObject r)=>new Ce03AcceptedCombat(r,p,(string)p["ownedSelection"]["accountId"],(string)r["operationPreviewId"]);
    [Test] public void PlayerEnvelopePinsPreparedContentAndSnapshotWithoutChangingOriginalBytes()
    {
        var (p,r)=Fixture();string before=r.ToString();Accept(p,r);Assert.AreEqual(before,r.ToString());
    }
    [Test] public void PlayerBindingTamperCannotHideInsideRecomputedPrepareHash()
    {
        var (p,r)=Fixture();p["playerAssetBinding"]["admissionGranted"]=true;
        Assert.Throws<FormatException>(()=>Accept(p,r));
        (p,r)=Fixture();p["playerAssetBinding"]["formation"][0]="unknown";Assert.Throws<FormatException>(()=>Accept(p,r));
    }
    [Test] public void ConfirmedPlayerConsumerKeepsOriginalAndCheckpointWhileClosingPresentation()
    {
        var (p,r)=Fixture();var origin=new Uri("http://127.0.0.1:12345/");
        var source=Ce03R6RecoveryChecks.ReadJson(File.ReadAllText("QA/Ce03CommandIntegration/synthetic-r6-transport.json"));
        var envelope=new JObject{["version"]="ce03-original-start-v1",["owner"]="a",["origin"]=origin.GetLeftPart(UriPartial.Authority),["key"]=source["startIdempotencyKey"].DeepClone(),["body"]=new JObject{["operationPreviewId"]=r["operationPreviewId"].DeepClone(),["expectedSupportRevision"]=p["commandContract"]["support"]["revision"].DeepClone(),["prepareSha256"]=p["prepareSha256"].DeepClone()},["responseRaw"]=Ce03CommandContract.Canonical(r),["prepared"]=p};
        var original=new Ce03OriginalStart(Ce03CommandContract.Canonical(envelope),"a",origin);string before=original.Json,saved=null;
        var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");
        var store=new CombatCheckpointStore("synthetic-player-controller-test",()=>saved,raw=>saved=raw);
        var sceneObject=new UnityEngine.GameObject("ConsumerTestScene");var inputObject=new UnityEngine.GameObject("ConsumerInput",typeof(UnityEngine.EventSystems.EventSystem));
        using(var controller=new Ce03PlayerBattleController(original,asset,store,()=>true))
        try
        {
            var scene=sceneObject.AddComponent<CombatPlayerScene>();controller.Present(scene,inputObject.GetComponent<UnityEngine.EventSystems.EventSystem>(),null,null);
            scene.SceneCamera.Render();var speed=System.Linq.Enumerable.Single(scene.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="Speed").onClick;
            string initial=saved;speed.Invoke();Assert.AreNotEqual(initial,saved);
            controller.ClosePresentation();string committed=saved;speed.Invoke();Assert.AreEqual(committed,saved);Assert.AreEqual(before,original.Json);
        }
        finally{UnityEngine.Object.DestroyImmediate(sceneObject);UnityEngine.Object.DestroyImmediate(inputObject);}
    }
}

