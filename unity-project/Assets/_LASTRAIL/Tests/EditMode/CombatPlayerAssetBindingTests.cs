using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatPlayerAssetBindingTests
{
    static readonly string[] Squad={"op_kyle","op_reina","op_eve","op_sera","op_mira"};
    ScriptableObject binding;
    object Bake()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CombatPlayerAssetBindingBuilder")).FirstOrDefault(t=>t!=null);
        Assert.IsNotNull(type,"A Player asset binding must be built from actual approved references.");
        binding=(ScriptableObject)type.GetMethod("Bake").Invoke(null,null);return binding;
    }
    static object Call(object target,string method,params object[] args)
    {
        try{return target.GetType().GetMethod(method).Invoke(target,args);}
        catch(TargetInvocationException e){throw e.InnerException;}
    }
    static string Hash(object target)=>(string)target.GetType().GetProperty("ManifestHash").GetValue(target);
    [TearDown] public void Cleanup(){if(binding!=null)UnityEngine.Object.DestroyImmediate(binding);}
    [Test] public void TwoConsumersReleaseOnlyTheirLeaseAndKeepBorrowedAssetsAlive()
    {
        var asset=Bake();var first=Call(asset,"Acquire",Hash(asset),Squad);var second=Call(asset,"Acquire",Hash(asset),Squad);
        var config=(CombatPresentationAssets)Call(first,"ReadAssets");Assert.IsNotNull(config.ExteriorMap);Assert.IsNotNull(config.ActorAtlas("op_kyle"));
        ((IDisposable)first).Dispose();Assert.Throws<ObjectDisposedException>(()=>Call(first,"ReadAssets"));
        Assert.AreSame(config,Call(second,"ReadAssets"));((IDisposable)second).Dispose();Assert.IsTrue(config!=null);
    }
    [Test] public void WrongContentVersionOrUnsupportedFormationCannotAcquire()
    {
        var asset=Bake();Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",new string('0',64),Squad));
        Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",Hash(asset),new[]{"op_kyle","op_reina","op_eve","op_sera","unknown"}));
        Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",Hash(asset),new[]{"op_kyle","op_kyle","op_eve","op_sera","op_mira"}));
    }
    [Test] public void ReplacedPresentationReferenceCannotReuseVerifiedBinding()
    {
        var asset=Bake();var lease=Call(asset,"Acquire",Hash(asset),Squad);
        var config=(CombatPresentationAssets)Call(lease,"ReadAssets");var original=config.ExteriorMap;
        try{config.ExteriorMap=config.Interior;Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",Hash(asset),Squad));}
        finally{config.ExteriorMap=original;((IDisposable)lease).Dispose();}
    }
    [Test] public void ReorderedActorIdsCannotReassignVerifiedArtwork()
    {
        var asset=Bake();var lease=Call(asset,"Acquire",Hash(asset),Squad);
        var config=(CombatPresentationAssets)Call(lease,"ReadAssets");var ids=(string[])config.OperatorIds.Clone();
        try
        {
            var first=config.OperatorIds[0];config.OperatorIds[0]=config.OperatorIds[1];config.OperatorIds[1]=first;
            Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",Hash(asset),Squad));
        }
        finally{config.OperatorIds=ids;((IDisposable)lease).Dispose();}
    }
    [Test] public void AddingAnUnverifiedNestedArtRegistryCannotReuseBinding()
    {
        var asset=Bake();var lease=Call(asset,"Acquire",Hash(asset),Squad);
        var config=(CombatPresentationAssets)Call(lease,"ReadAssets");var original=config.OperatorFrames;
        var frames=ScriptableObject.CreateInstance<CombatOperatorFrames>();frames.Entries=new[]{new CombatOperatorFrames.Entry{OperatorId="op_kyle",Portrait=config.ExteriorMap}};
        try{config.OperatorFrames=frames;Assert.Throws<InvalidOperationException>(()=>Call(asset,"Acquire",Hash(asset),Squad));}
        finally{config.OperatorFrames=original;((IDisposable)lease).Dispose();UnityEngine.Object.DestroyImmediate(frames);}
    }
    [Test] public void OrdinaryPrefabBindingRequiresOnlyPlayerObjects()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CombatPlayerAssetBindingBuilder")).First(t=>t!=null);
        var config=UnityEditor.AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>("Assets/Resources/CombatPresentation/OrdinaryPresentation.asset");Assert.NotNull(config);
        binding=(ScriptableObject)type.GetMethod("BakeConfiguration").Invoke(null,new object[]{config});
        var manifest=Newtonsoft.Json.Linq.JObject.Parse(((CombatPlayerAssetBinding)binding).ManifestJson);
        Assert.IsFalse(manifest["runtimeObjects"].Any(r=>((string)r["type"]).StartsWith("UnityEditor",StringComparison.Ordinal)));
        Assert.IsFalse(manifest["runtimeObjects"].Any(r=>(string)r["type"]=="UnityEngine.Object"&&(string)r["name"]==""));
        using var lease=((CombatPlayerAssetBinding)binding).Acquire(Hash(binding),Squad);
        int duration=config.OperatorFrames.Entries[0].DurationsMs[0];
        try{config.OperatorFrames.Entries[0].DurationsMs[0]++;Assert.Throws<InvalidOperationException>(()=>((CombatPlayerAssetBinding)binding).Acquire(Hash(binding),Squad));}
        finally{config.OperatorFrames.Entries[0].DurationsMs[0]=duration;}
    }
}
