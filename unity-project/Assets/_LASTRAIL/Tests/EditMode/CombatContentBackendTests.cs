using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatContentBackendTests
    {
        const string Root="https://content.example.test/releases/";
        static readonly string Hash="sha256:"+new string('a',64);
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(target,value);
        static AddressableContentBackend Backend(ResourceLocationMap locator)
        {
            var backend=new AddressableContentBackend(null,false,trustedCatalogRoot:Root);
            var gate=ClientAdmissionContract.Parse("{\"accepted\":true,\"maintenance\":{\"enabled\":false},\"clientAdmission\":{\"status\":\"ACCEPTED\",\"installedVersionCode\":\"42\",\"minimumVersionCode\":\"40\",\"updateRequired\":false},\"contentRelease\":{\"status\":\"APPROVED_COMPATIBLE\",\"releaseId\":\"fixture-content\",\"contentVersion\":\"fixture-content\",\"catalogUrl\":\""+Root+"fixture-content/catalog.bin\",\"catalogHash\":\""+Hash+"\",\"compatibility\":{\"minimumVersionCode\":\"40\",\"maximumVersionCode\":null}}}","42","https://api.lastrail.app/v1/client-config",Root);
            Set(backend,"approvedGate",gate);Set(backend,"approvedLocator",locator);Set(backend,"<ContentRevision>k__BackingField","fixture-content:"+Hash);return backend;
        }
        [Test] public async Task RejectsMismatchedAdmissionMissingAndAmbiguousLocations()
        {
            var map=new ResourceLocationMap("fixture");using var backend=Backend(map);
            using var loader=CombatContentLoaderTests.Fixture((k,r,c)=>backend.LoadApprovedCombatAssetAsync<TextAsset>(k,r.ContentVersion,r.CatalogHash,c));
            Assert.That((await loader.LoadAsync(new CombatContentRequest(CombatContentLoaderTests.Input()))).Diagnostic,Is.EqualTo("asset.approved_location"));
            var data=CombatContentLoaderTests.Input();data["catalogHash"]="sha256:"+new string('b',64);
            Assert.That((await loader.LoadAsync(new CombatContentRequest(data))).Diagnostic,Is.EqualTo("catalog.admission_mismatch"));
            data=CombatContentLoaderTests.Input();data["contentVersion"]="wrong";
            Assert.That((await loader.LoadAsync(new CombatContentRequest(data))).Status,Is.EqualTo(CombatContentStatus.Invalid));
            var location=new ResourceLocationBase("fixture-manifest","fixture-manifest",typeof(BundledAssetProvider).FullName,typeof(TextAsset));map.Add("fixture-manifest",location);map.Add("fixture-manifest",location);
            Assert.That((await loader.LoadAsync(new CombatContentRequest(CombatContentLoaderTests.Input()))).Status,Is.EqualTo(CombatContentStatus.Invalid));
        }
        sealed class Provider : ResourceProviderBase
        {
            public TextAsset Asset;public int Releases;public bool Delay;public ProvideHandle? Held;
            public override string ProviderId=>typeof(BundledAssetProvider).FullName;
            public override void Provide(ProvideHandle handle){if(Delay)Held=handle;else handle.Complete(Asset,true,null);}
            public void Complete(){Held.Value.Complete(Asset,true,null);Held=null;}
            public override void Release(IResourceLocation location,object asset){Releases++;}
        }
        sealed class DependencyProvider : ResourceProviderBase
        {
            sealed class Bundle : IAssetBundleResource { public AssetBundle GetAssetBundle()=>null; }
            public override string ProviderId=>typeof(CacheOnlyAssetBundleProvider).FullName;
            public override void Provide(ProvideHandle handle)=>handle.Complete<IAssetBundleResource>(new Bundle(),true,null);
        }
        [Test] public async Task ActualTypedLoadIsPinnedAndInvalidatedAdmissionReleasesLateAsset()
        {
            // Only provider/catalog fixtures. No global key registration, shipping binding or gameplay readiness.
            var field=typeof(Addressables).GetField("m_AddressablesInstance",BindingFlags.Static|BindingFlags.NonPublic);var previous=field.GetValue(null);
            var isolated=Activator.CreateInstance(previous.GetType(),new object[]{new DefaultAllocationStrategy()});Set(isolated,"hasStartedInitialization",true);
            foreach(var pair in new[]{new[]{"m_OnHandleCompleteAction","OnHandleCompleted"},new[]{"m_OnSceneHandleCompleteAction","OnSceneHandleCompleted"},new[]{"m_OnHandleDestroyedAction","OnHandleDestroyed"}}) {
                var action=isolated.GetType().GetField(pair[0],BindingFlags.Instance|BindingFlags.NonPublic);
                action.SetValue(isolated,Delegate.CreateDelegate(action.FieldType,isolated,isolated.GetType().GetMethod(pair[1],BindingFlags.Instance|BindingFlags.NonPublic)));
            }
            field.SetValue(null,isolated);var manager=Addressables.ResourceManager;var asset=new TextAsset("fixture");
            try {
                var provider=new Provider{Asset=asset};manager.ResourceProviders.Add(provider);manager.ResourceProviders.Add(new DependencyProvider());var map=new ResourceLocationMap("approved-only");
                var dependency=new ResourceLocationBase("fixture-dependency",System.IO.Path.GetFullPath("Library/combat-fixture.bundle"),typeof(CacheOnlyAssetBundleProvider).FullName,typeof(object)) { Data=new AssetBundleRequestOptions {BundleName="combat-fixture"} };
                map.Add("fixture-manifest",new ResourceLocationBase("fixture-manifest","Assets/fixture-manifest",typeof(BundledAssetProvider).FullName,typeof(TextAsset),dependency));
                using var backend=Backend(map);using var loader=CombatContentLoaderTests.Fixture((k,r,c)=>backend.LoadApprovedCombatAssetAsync<TextAsset>(k,r.ContentVersion,r.CatalogHash,c));
                using(var probe=await backend.LoadApprovedCombatAssetAsync<TextAsset>("fixture-manifest","fixture-content",Hash,System.Threading.CancellationToken.None)) Assert.That(probe.Asset,Is.SameAs(asset)); manager.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{0f}); provider.Releases=0; var result=await loader.LoadAsync(new CombatContentRequest(CombatContentLoaderTests.Input()));
                Assert.That(result.Status,Is.EqualTo(CombatContentStatus.Loaded),result.Diagnostic);Assert.That(loader.Asset,Is.SameAs(asset));Assert.That(result.Ready,Is.False);
                loader.SynchronizeOwner(new object(),2);manager.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{0f});Assert.That(provider.Releases,Is.EqualTo(1));
                provider.Delay=true;var pending=loader.LoadAsync(new CombatContentRequest(CombatContentLoaderTests.Input()));
                Assert.That(provider.Held.HasValue,Is.True);Set(backend,"admissionGeneration",2);provider.Complete();manager.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{0f});
                Assert.That((await pending).Status,Is.EqualTo(CombatContentStatus.Cancelled));manager.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{0f});Assert.That(provider.Releases,Is.EqualTo(2));Assert.That(loader.Asset,Is.Null);
            }finally{manager.Dispose();field.SetValue(null,previous);UnityEngine.Object.DestroyImmediate(asset);}
        }
    }
}
