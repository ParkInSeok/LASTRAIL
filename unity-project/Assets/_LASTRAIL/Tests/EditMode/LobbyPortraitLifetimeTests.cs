using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyPortraitLifetimeTests
    {
        [UnityTest]
        public IEnumerator PlayModeCancelsLatePortraitLoadAndReleasesOnEveryHide()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            yield return new EnterPlayMode();
            yield return ExerciseLifetime();
            yield return new ExitPlayMode();
        }
        static IEnumerator ExerciseLifetime()
        {
            var current=typeof(PreparedContent).GetField("current",BindingFlags.Static|BindingFlags.NonPublic);var previous=current.GetValue(null);
            int acquired=0,released=0;bool delayed=true;
            var pending=new List<(TaskCompletionSource<PreparedContent.AssetLease>,Texture2D)>();
            var session=new PreparedContent.Session(key=>{
                acquired++;var texture=new Texture2D(4,4);var lease=new PreparedContent.AssetLease(texture,()=>{released++;Object.Destroy(texture);});
                if(!delayed)return Task.FromResult(lease);
                var completion=new TaskCompletionSource<PreparedContent.AssetLease>();pending.Add((completion,texture));return completion.Task;
            });
            current.SetValue(null,session);
            var root=new GameObject("PortraitLifetime",typeof(RectTransform));var images=new RawImage[5];var plus=new GameObject[5];
            for(int i=0;i<5;i++){var node=new GameObject("Portrait"+i,typeof(RectTransform),typeof(RawImage));node.transform.SetParent(root.transform,false);images[i]=node.GetComponent<RawImage>();plus[i]=new GameObject("Plus"+i);plus[i].transform.SetParent(root.transform,false);}
            var view=root.AddComponent<LobbySquadPortraits>();
            try{
                view.Bind(images,plus);view.Render(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"});
                root.SetActive(false);
                foreach(var item in pending){var texture=item.Item2;item.Item1.SetResult(new PreparedContent.AssetLease(texture,()=>{released++;Object.Destroy(texture);}));}
                yield return null;yield return null;
                Assert.AreEqual(acquired,released,"A cancelled late load must release every acquired handle");
                foreach(var image in images)Assert.IsNull(image.texture);
                delayed=false;
                for(int i=0;i<3;i++){
                    root.SetActive(true);yield return null;foreach(var image in images)Assert.NotNull(image.texture);
                    root.SetActive(false);yield return null;foreach(var image in images)Assert.IsNull(image.texture);Assert.AreEqual(acquired,released);
                }
                root.SetActive(true);yield return null;Object.Destroy(root);yield return null;Assert.AreEqual(acquired,released);
            }
            finally{if(root!=null)Object.Destroy(root);current.SetValue(null,previous);session.Dispose();}
        }
    }
}
