using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AugmentSelectionRuntimeTests
{
    sealed class Journal:IAugmentSelectionJournal{public string Value;public string Load(string key)=>Value;public void SaveNew(string key,string command){if(Value!=null)throw new InvalidOperationException();Value=command;}public void Complete(string key,string expected){if(Value!=expected)throw new InvalidOperationException();Value=null;}}
    sealed class Wire:IGameApiAuthenticatedHttpTransport{public string Reply;public int Calls;public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){Calls++;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply)));}}
    [TestCase(false)][TestCase(true)]public async Task ViewSelectUsesInstalledHttpReceiverStoreAndDisposalUnbinds(bool persistent)
    {
        var type=typeof(AugmentSelectionController).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectionRuntime");Assert.IsNotNull(type);
        var root=new GameObject("ActualSelectionHost",typeof(RectTransform),typeof(Canvas));IDisposable runtime=null;
        try{
            var view=AugmentSelectionView.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
            var packet=CombatO22OriginalTests.Packet();var confirmed=(JObject)packet["definition"]["selectionSource"]["selection"];var initial=(JObject)confirmed.DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
            var wire=new Wire{Reply=new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,["result"]=new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=packet}}.ToString()};
            var session=new GameApiAuthSessionSnapshot("account","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
            var owner=new AugmentSelectionOwner("account","TEST");var scope=new AugmentSelectionScope(owner,"run","grant");var journal=new Journal();string saved=null;
            var store=new CombatO22ApplicationStore(Path.GetFullPath("../LocalTemp/o22-host-"+Guid.NewGuid().ToString("N")),()=>saved,s=>saved=s);
            string journalPath=Path.GetFullPath("../LocalTemp/selection-host-"+Guid.NewGuid().ToString("N"));
            IAndroidKeystoreNativeStoragePort crypto=null;
            if(persistent){
                crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(CombatO22StoreTests).GetNestedType("TestCrypto",System.Reflection.BindingFlags.NonPublic),true);
                var create=type.GetMethod("CreatePersistent");Assert.IsNotNull(create);
                Func<string,string,AugmentCardPresentation> definitions=(version,id)=>new AugmentCardPresentation("원정의 "+id,"증강","원정의 설명",null,"상세");
                runtime=(IDisposable)create.Invoke(null,new object[]{view,scope,wire,new Func<GameApiAuthSessionSnapshot>(()=>session),new Func<bool>(()=>true),crypto,journalPath,store,new string('a',64),definitions});
            }else runtime=(IDisposable)Activator.CreateInstance(type,view,scope,wire,new Func<GameApiAuthSessionSnapshot>(()=>session),new Func<bool>(()=>true),journal,store,new string('a',64));
            await (Task)type.GetMethod("OpenAsync").Invoke(runtime,new object[]{AugmentSelectionSnapshot.Parse(initial.ToString())});
            Button Find(string id)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==id);
            if(persistent)Assert.IsTrue(view.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="원정의 O22"));
            Assert.IsFalse(Find("Free0").interactable);Find("Select0").onClick.Invoke();
            Assert.AreEqual(1,wire.Calls);Assert.IsNull(journal.Value);Assert.IsNotNull(saved);Assert.IsFalse(store.Load("account","run","grant","TEST",new string('a',64)).Applied);
            if(persistent){Assert.IsTrue(File.Exists(journalPath));Assert.IsNull(new AugmentSelectionFileJournal(scope,crypto,journalPath).Load(scope.StorageKey));}
            runtime.Dispose();Find("Select1").onClick.Invoke();Assert.AreEqual(1,wire.Calls);
        }finally{runtime?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
    }
}
