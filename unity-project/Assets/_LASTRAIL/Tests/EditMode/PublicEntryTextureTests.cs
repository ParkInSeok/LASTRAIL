using System;
using System.Collections;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class PublicEntryTextureTests
    {
        // EditMode does not dispatch activation callbacks for ordinary MonoBehaviours.
        static void Lifecycle(GameObject root,string name)=>typeof(PublicEntryTexture).GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(root.GetComponent<PublicEntryTexture>(),null);
        sealed class Deferred:AsyncOperationBase<Texture2D>
        {
            public int Releases;
            protected override void Execute(){}
            public void Finish(Texture2D texture)=>Complete(texture,true,(string)null);
            protected override void Destroy(){Releases++;}
        }
        [Test]public void LoginArtworkUsesCacheOnlyProviderAndNormalInitialDownload()
        {
            var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var group=settings.FindGroup(PublicEntryTexture.Label);
            Assert.That(group.entries.Count,Is.EqualTo(4));
            Assert.That(group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>().AssetBundleProviderType.Value,Is.EqualTo(typeof(CacheOnlyAssetBundleProvider)));
            foreach(var entry in group.entries)Assert.That(entry.labels.Contains(AddressableContentBackend.RequiredLabel),Is.True);
        }
        [Test]public void PublicArtworkCannotResolveGameplayKeys()
        {
            Assert.Throws<ArgumentException>(()=>PublicEntryTexture.Path("OperatorIdentity/op_kyle"));
            Assert.That(PublicEntryTexture.Path("login-background"),Does.StartWith(PublicEntryTexture.Root));
        }
        [UnityTest]public IEnumerator ClosingPendingViewBlocksOldResultAndReentryReleasesEachHandleOnce()
        {
            var old=PublicEntryTexture.EditorLoad;var first=new Deferred();var second=new Deferred();int calls=0;
            PublicEntryTexture.EditorLoad=_=>Addressables.ResourceManager.StartOperation(++calls==1?first:second,default(AsyncOperationHandle));
            var root=new GameObject("Public artwork lifecycle");var texture=new Texture2D(2,2);Texture2D shown=null;
            try
            {
                root.AddComponent<PublicEntryTexture>().Bind("login-background",value=>shown=value);
                root.SetActive(false);Lifecycle(root,"OnDisable");root.SetActive(true);
                if(calls==1)Lifecycle(root,"OnEnable");
                Assert.That(calls,Is.EqualTo(2));second.Finish(texture);
                for(int i=0;i<30&&shown==null;i++)yield return null;
                Assert.That(shown,Is.SameAs(texture));
                first.Finish(texture);yield return null;
                Assert.That(first.Releases,Is.EqualTo(1));Assert.That(shown,Is.SameAs(texture));
                root.SetActive(false);Lifecycle(root,"OnDisable");yield return null;
                Assert.That(shown,Is.Null);Assert.That(second.Releases,Is.EqualTo(1));
                UnityEngine.Object.DestroyImmediate(root);Assert.That(second.Releases,Is.EqualTo(1));
            }
            finally{PublicEntryTexture.EditorLoad=old;if(root!=null)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
