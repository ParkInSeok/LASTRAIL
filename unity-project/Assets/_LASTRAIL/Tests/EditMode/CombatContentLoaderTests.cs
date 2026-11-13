using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatContentLoaderTests
    {
        static readonly string Hash="sha256:"+new string('a',64);
        internal static JObject Input(string binding="fixture-v1")=>new JObject {
            ["operationId"]="operation-fixture",["contentVersion"]="fixture-content",["startAugmentPackageRef"]="augment-fixture",
            ["startTacticPackageRef"]="tactic-fixture",["encounterType"]="ORDINARY_15_WAVE",["raidId"]=null,["bossId"]=null,
            ["bossPhaseRefs"]=new JArray("phase-fixture"),["bindingVersion"]=binding==null?JValue.CreateNull():new JValue(binding),["catalogHash"]=binding==null?JValue.CreateNull():new JValue(Hash) };
        internal static CombatContentLoader<TextAsset> Fixture(Func<string,CombatContentRequest,CancellationToken,Task<CombatAssetLease<TextAsset>>> source,
            Func<TextAsset,CombatContentRequest,bool> validate=null,Func<CombatContentRequest,string> resolve=null)
        {
            var loader=CombatContentLoader<TextAsset>.CreateFixture("fixture-v1",resolve??(_=>"fixture-manifest"),validate??((a,r)=>a.text=="fixture"),source);
            loader.SynchronizeOwner(new object(),1);return loader;
        }
        [Test] public async Task ShippingRegistryCannotLoadFixtureOrGrantReady()
        {
            using var backend=new AddressableContentBackend(null,false);using var loader=new CombatContentLoader<TextAsset>(backend);loader.SynchronizeOwner(new object(),1);
            var missing=await loader.LoadAsync(new CombatContentRequest(Input(null)));
            Assert.That(missing.Status,Is.EqualTo(CombatContentStatus.Missing),missing.Diagnostic);Assert.That(missing.Diagnostic,Is.EqualTo("binding.unapproved"));
            var unsupported=await loader.LoadAsync(new CombatContentRequest(Input()));
            Assert.That(unsupported.Status,Is.EqualTo(CombatContentStatus.Unsupported));Assert.That(unsupported.Ready,Is.False);Assert.That(loader.Asset,Is.Null);
        }
        [Test] public async Task InputIsImmutableAndMalformedHashDoesNotLoad()
        {
            var data=Input();var request=new CombatContentRequest(data);data["operationId"]="changed";request.RequiredContent["bossPhaseRefs"][0]="changed";
            Assert.That((string)request.RequiredContent["operationId"],Is.EqualTo("operation-fixture"));Assert.That((string)request.RequiredContent["bossPhaseRefs"][0],Is.EqualTo("phase-fixture"));
            var calls=0;using var loader=Fixture((k,r,c)=>{calls++;throw new Exception();});data["catalogHash"]="invalid";
            var result=await loader.LoadAsync(new CombatContentRequest(data));Assert.That(result.Status,Is.EqualTo(CombatContentStatus.Invalid));Assert.That(result.Diagnostic,Does.EndWith("catalogHash"));Assert.That(calls,Is.Zero);
        }
        [Test] public async Task MissingMappingFailureAndEmptyAssetAreDistinct()
        {
            using var missing=Fixture((k,r,c)=>throw new Exception(),resolve:_=>null);
            Assert.That((await missing.LoadAsync(new CombatContentRequest(Input()))).Diagnostic,Is.EqualTo("binding.asset_mapping"));
            using var failed=Fixture((k,r,c)=>throw new Exception("private detail"));
            Assert.That((await failed.LoadAsync(new CombatContentRequest(Input()))).Diagnostic,Is.EqualTo("asset.load_failed"));
            using var empty=Fixture((k,r,c)=>Task.FromResult<CombatAssetLease<TextAsset>>(null));
            Assert.That((await empty.LoadAsync(new CombatContentRequest(Input()))).Status,Is.EqualTo(CombatContentStatus.LoadFailed));
        }
        [Test] public async Task InvalidAssetReleasesAndLoadedNeverMeansReady()
        {
            var asset=new TextAsset("wrong");var released=0;
            try {
                using var invalid=Fixture((k,r,c)=>Task.FromResult(CombatAssetLease<TextAsset>.CreateFixture(asset,()=>released++)));
                Assert.That((await invalid.LoadAsync(new CombatContentRequest(Input()))).Status,Is.EqualTo(CombatContentStatus.Invalid));Assert.That(released,Is.EqualTo(1));
                using var loaded=Fixture((k,r,c)=>Task.FromResult(CombatAssetLease<TextAsset>.CreateFixture(asset,()=>released++)),(a,r)=>true);
                var result=await loaded.LoadAsync(new CombatContentRequest(Input()));Assert.That(result.Status,Is.EqualTo(CombatContentStatus.Loaded));Assert.That(loaded.Asset,Is.SameAs(asset));Assert.That(result.Ready,Is.False);
                loaded.Dispose();loaded.Dispose();Assert.That(released,Is.EqualTo(2));Assert.That(loaded.Asset,Is.Null);
            }finally{UnityEngine.Object.DestroyImmediate(asset);}
        }
        [Test] public async Task NewRequestDiscardsLateSuccessAndKeepsNewest()
        {
            var first=new TaskCompletionSource<CombatAssetLease<TextAsset>>();var second=new TaskCompletionSource<CombatAssetLease<TextAsset>>();var calls=0;var releases=0;var asset=new TextAsset("fixture");
            try {
                using var loader=Fixture((k,r,c)=>++calls==1?first.Task:second.Task);var older=loader.LoadAsync(new CombatContentRequest(Input()));var newer=loader.LoadAsync(new CombatContentRequest(Input()));
                second.SetResult(CombatAssetLease<TextAsset>.CreateFixture(asset,()=>releases++));Assert.That((await newer).Status,Is.EqualTo(CombatContentStatus.Loaded));
                first.SetResult(CombatAssetLease<TextAsset>.CreateFixture(asset,()=>releases++));Assert.That((await older).Status,Is.EqualTo(CombatContentStatus.Cancelled));Assert.That(releases,Is.EqualTo(1));
                Assert.That(loader.Result.Status,Is.EqualTo(CombatContentStatus.Loaded));Assert.That(loader.Asset,Is.SameAs(asset));loader.SynchronizeOwner(new object(),2);Assert.That(releases,Is.EqualTo(2));
            }finally{UnityEngine.Object.DestroyImmediate(asset);}
        }
        [Test] public async Task OwnerGenerationAndExternalCancellationReleaseLateAssets()
        {
            foreach(var external in new[]{false,true}) {
                var promise=new TaskCompletionSource<CombatAssetLease<TextAsset>>();var asset=new TextAsset("fixture");var releases=0;
                try {
                    using var cancel=new CancellationTokenSource();using var loader=Fixture((k,r,c)=>promise.Task);var owner=new object();loader.SynchronizeOwner(owner,1);
                    var running=loader.LoadAsync(new CombatContentRequest(Input()),cancel.Token);if(external)cancel.Cancel();else loader.SynchronizeOwner(owner,2);
                    promise.SetResult(CombatAssetLease<TextAsset>.CreateFixture(asset,()=>releases++));Assert.That((await running).Status,Is.EqualTo(CombatContentStatus.Cancelled));Assert.That(releases,Is.EqualTo(1));Assert.That(loader.Asset,Is.Null);
                }finally{UnityEngine.Object.DestroyImmediate(asset);}
            }
        }
        [Test] public async Task DisposalDoesNotPublishLateFailure()
        {
            var promise=new TaskCompletionSource<CombatAssetLease<TextAsset>>();var loader=Fixture((k,r,c)=>promise.Task);var running=loader.LoadAsync(new CombatContentRequest(Input()));
            loader.Dispose();promise.SetException(new Exception());Assert.That((await running).Status,Is.EqualTo(CombatContentStatus.Cancelled));Assert.That(loader.Result.Diagnostic,Is.EqualTo("loader.disposed"));
        }
    }
}
