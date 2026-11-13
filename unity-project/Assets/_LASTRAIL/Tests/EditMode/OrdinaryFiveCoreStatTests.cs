using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryFiveCoreStatTests
{
    static JObject Fixture()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Stats589/fixture.json"));
    static Type Type(string name){var t=typeof(CombatSceneDriver).Assembly.GetType("LastRail.Battle."+name);Assert.IsNotNull(t,"Exact ordinary-five source consumer missing");return t;}
    static object Invoke(object owner,string method,params object[] args){try{return owner.GetType().GetMethod(method).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
    static object Property(object owner,string property)=>owner.GetType().GetProperty(property).GetValue(owner);
    static string Digest(JObject value)=>BDirectAttackInput.Digest(value).Substring(7);
    static string Rehash(JObject row){var p=(JObject)row["packet"];p["sourceHash"]=Digest((JObject)row["state"]);var b=(JObject)p.DeepClone();b.Remove("packetHash");p["packetHash"]=Digest(b);return (string)p["packetHash"];}
    static object Feed(JObject f)=>Activator.CreateInstance(Type("OrdinaryFiveCoreStatFeed"),"a","future-run",(string)f["originHash"]);
    static object Publish(object feed,JObject row,string hash=null)=>Invoke(feed,"Publish",row["packet"],row["state"],hash??(string)row["packet"]["packetHash"]);
    [Test] public void ActualServerPacketsPreserveDamagedHpAndExactEquipmentResultWithoutStacking()
    {
        var f=Fixture();var feed=Feed(f);int changes=0;feed.GetType().GetEvent("Changed").AddEventHandler(feed,(Action)(()=>changes++));
        Assert.AreEqual(true,Publish(feed,(JObject)f["first"]));Assert.AreEqual(false,Publish(feed,(JObject)f["first"]));
        Assert.AreEqual(true,Publish(feed,(JObject)f["second"]));Assert.AreEqual(2,changes);
        var current=Property(feed,"Current");var actor=Invoke(current,"Actor","op_kyle");
        Assert.AreEqual(699L,Property(actor,"CurrentHp"));Assert.AreEqual(1484L,Property(actor,"MaxHp"));
        Assert.AreEqual("358",((CombatExactValue)Property(actor,"Defense")).Snapshot()["numerator"].Value<string>());
        f["second"]["packet"]["actors"][0]["currentHp"]=0;
        Assert.AreEqual(699L,Property(actor,"CurrentHp"));
    }
    [Test] public void OriginalAndLatestAreBoundAndStaleOrEqualRevisionConflictsCannotReplaceCurrent()
    {
        var f=Fixture();var feed=Feed(f);Publish(feed,(JObject)f["second"]);var latest=Property(feed,"Current");
        Assert.Throws<ArgumentException>(()=>Publish(feed,(JObject)f["first"]));Assert.AreSame(latest,Property(feed,"Current"));
        var conflict=(JObject)f["second"].DeepClone();conflict["state"]["actors"][0]["currentHp"]=698;conflict["packet"]["actors"][0]["currentHp"]=698;Rehash(conflict);
        Assert.Throws<ArgumentException>(()=>Publish(feed,conflict));Assert.AreSame(latest,Property(feed,"Current"));
    }
    [TestCase("missingProof")][TestCase("missingProofVersion")][TestCase("foreignRun")][TestCase("wrongOrigin")][TestCase("wrongHash")][TestCase("wrongHp")][TestCase("duplicateActor")][TestCase("unsupportedEnchant")][TestCase("missingSlot")]
    public void MissingUnsupportedOrMismatchedSourcesAreNotTreatedAsEmpty(string change)
    {
        var f=Fixture();var row=(JObject)f["second"];var feed=Feed(f);var p=(JObject)row["packet"];var s=(JObject)row["state"];
        if(change=="missingProof")((JObject)s["modifiers"]).Remove("temporary");
        if(change=="missingProofVersion")((JObject)s["modifiers"]["research"]).Remove("version");
        if(change=="foreignRun"){p["runId"]="other";s["runId"]="other";}
        if(change=="wrongOrigin")p["originHash"]=new string('0',64);
        if(change=="wrongHp")p["actors"][0]["currentHp"]=1400;
        if(change=="duplicateActor")p["actors"][1]["operatorId"]="op_kyle";
        if(change=="unsupportedEnchant"){p["actors"][0]["equipment"][0]["enchantment"]=new JObject();s["actors"][0]["slots"][0]["enchantment"]=new JObject();}
        if(change=="missingSlot")((JArray)s["actors"][0]["slots"]).RemoveAt(2);
        var hash=Rehash(row);if(change=="wrongHash")hash=new string('0',64);
        Assert.Throws<ArgumentException>(()=>Publish(feed,row,hash));Assert.IsNull(Property(feed,"Current"));
    }
    [Test] public void FeedRejectsUpdatesOutsideItsOwningThread()
    {
        var f=Fixture();var feed=Feed(f);
        var failure=System.Threading.Tasks.Task.Run(()=>{try{Publish(feed,(JObject)f["first"]);return (Exception)null;}catch(Exception e){return e;}}).GetAwaiter().GetResult();
        Assert.IsInstanceOf<InvalidOperationException>(failure);Assert.IsNull(Property(feed,"Current"));
    }
    [TestCase("inventory")][TestCase("placement")][TestCase("item")][TestCase("effect")][TestCase("definition")][TestCase("proof")]
    public void SourceVersionRegressionCannotPublishEvenWithNewerPacketRevision(string change)
    {
        var f=Fixture();var before=(JObject)f["second"];var p=(JObject)before["packet"];var s=(JObject)before["state"];
        p["actors"][0]["equipment"][0]["instanceRevision"]=2;s["actors"][0]["slots"][0]["instanceRevision"]=2;
        p["actors"][0]["equipment"][0]["effectRevision"]=2;s["actors"][0]["slots"][0]["effectRevision"]=2;
        s["modifiers"]["temporary"]["revision"]=2;Rehash(before);
        var feed=Feed(f);Publish(feed,before);var original=Property(feed,"Current");
        var next=(JObject)before.DeepClone();p=(JObject)next["packet"];s=(JObject)next["state"];p["revision"]=3;s["revision"]=3;
        if(change=="inventory"){p["inventoryRevision"]=1;s["inventoryRevision"]=1;}
        if(change=="placement"){p["actors"][0]["equipment"][0]=null;s["actors"][0]["slots"][0]=null;}
        if(change=="item"||change=="effect"||change=="definition"){
            p["inventoryRevision"]=3;s["inventoryRevision"]=3;
            string key=change=="item"?"instanceRevision":change=="effect"?"effectRevision":"star";int value=change=="definition"?2:1;
            p["actors"][0]["equipment"][0][key]=value;s["actors"][0]["slots"][0][key]=value;
        }
        if(change=="proof")s["modifiers"]["temporary"]["revision"]=1;
        Rehash(next);Assert.Throws<ArgumentException>(()=>Publish(feed,next));Assert.AreSame(original,Property(feed,"Current"));
    }
}
