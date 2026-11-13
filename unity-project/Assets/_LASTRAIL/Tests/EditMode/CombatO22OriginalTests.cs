using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatO22OriginalTests
{
    // Contract-shaped synthetic packet, never a real grant/production authority.
    static JToken Ordered(JToken t)=>t is JObject o?new JObject(o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Ordered(p.Value)))):t is JArray a?new JArray(a.Select(Ordered)):t.DeepClone();
    static string Hash(JToken t){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Ordered(t).ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();}
    static JObject Pin(JObject j){j["packetHash"]=Hash(j);return j;}
    public static JObject Packet()
    {
        var selection=JObject.Parse("{runId:'run',eventId:'grant',stageId:'stage',catalogVersion:'augment.utility-definitions.v7.20261019',grantRevision:1,state:'CONFIRMED',slots:[{slotIndex:0,candidateId:'O22',freeRemaining:3,extraRemaining:1},{slotIndex:1,candidateId:'O23',freeRemaining:3,extraRemaining:1},{slotIndex:2,candidateId:'O06',freeRemaining:3,extraRemaining:1}],selectedCandidateId:'O22'}");
        string h=new string('a',64);
        var source=Pin(new JObject{["version"]="combat.augment-selection-source.v1.20261016",["accountId"]="account",["environment"]="TEST",["runId"]="run",["eventId"]="grant",["stageId"]="stage",["catalogVersion"]=selection["catalogVersion"],["storeRevision"]=2,["issueHash"]=h,["checkpointHash"]=h,["startSnapshotHash"]=h,["selection"]=selection,["sourceStatus"]="ORIGINAL_GRANT_SELECTION_NOT_EFFECT_OWNERSHIP",["effectBinding"]=null,["effectOwnershipEstablished"]=false,["runtimeSupported"]=false,["canStart"]=false});
        var definition=Pin(new JObject{["version"]="combat.utility-selection-definition.v1.20261019",["accountId"]="account",["environment"]="TEST",["runId"]="run",["eventId"]="grant",["startSnapshotHash"]=h,["selectionSource"]=source,["definitionCatalog"]=new JObject{["version"]=selection["catalogVersion"],["definitionHash"]=h},["selectedDefinition"]=JObject.Parse("{id:'O22',name:'흡혈 회로',rules:{damageToHealBps:1200,eligible:'ALL_CONFIRMED_ENEMY_HP_DAMAGE_ATTRIBUTED_TO_THIS_OPERATOR',addedRecoveryCap:null,internalCooldownMs:0}}"),["candidateDefinitions"]=new JArray(),["composition"]=new JObject(),["lifecycle"]=new JObject(),["resistance"]=new JObject(),["eligibility"]=new JObject(),["bindingStatus"]="CONFIRMED_SELECTION_DEFINITION_ONLY",["effectOwnershipEstablished"]=false,["applied"]=false,["runtimeSupported"]=false,["canStart"]=false,["missingBindings"]=new JArray()});
        var request=new JObject{["accountId"]="account",["runId"]="run",["eventId"]="grant",["environment"]="TEST",["expectedStoreRevision"]=2,["expectedCatalogVersion"]=selection["catalogVersion"],["expectedStartSnapshotHash"]=h};
        return new JObject{["version"]="combat.o22-application-original.v1.20261019",["applicationId"]="o22:"+Hash(request),["accountId"]="account",["runId"]="run",["grantEventId"]="grant",["environment"]="TEST",["effectId"]="O22",["status"]="SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED",["combatOwner"]="UNITY_LOCAL",["applied"]=false,["definition"]=definition};
    }
    static object Read(JObject p,string start=null)
    {
        var type=typeof(CombatVitals).Assembly.GetType("LastRail.Run.CombatO22ApplicationOriginal");Assert.IsNotNull(type);
        return type.GetMethod("Read").Invoke(null,new object[]{p.ToString(),"account","run","grant","TEST",start??new string('a',64)});
    }
    [Test]public void ValidSelectionOriginalIsRetainedWithoutClaimingApplied()
    {
        var p=Packet();var original=Read(p);Assert.AreEqual(false,original.GetType().GetProperty("Applied").GetValue(original));
        Assert.AreEqual((string)p["applicationId"],original.GetType().GetProperty("ApplicationId").GetValue(original));
    }
    [Test]public void ChangedScopePinStartOrAppliedStatusIsRejected()
    {
        foreach(string key in new[]{"accountId","applicationId","combatOwner"}){var p=Packet();p[key]="changed";Assert.Throws<TargetInvocationException>(()=>Read(p));}
        var changed=Packet();changed["definition"]["selectedDefinition"]["rules"]["damageToHealBps"]=9999;Assert.Throws<TargetInvocationException>(()=>Read(changed));
        changed=Packet();changed["applied"]=true;Assert.Throws<TargetInvocationException>(()=>Read(changed));
        Assert.Throws<TargetInvocationException>(()=>Read(Packet(),new string('b',64)));
    }
}
