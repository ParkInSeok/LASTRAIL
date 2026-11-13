using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class CombatPlayerSceneTests
{
    GameObject hostObject,inputObject; Component host; CombatPlayerAssetBinding binding;
    CombatPlayerAssetBinding.Lease spectator; CombatSceneDriver driver; bool owner=true;
    static readonly string[] Squad={"op_kyle","op_reina","op_eve","op_sera","op_mira"};
    static BasicCombatSimulation Fresh(){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);}
    object Call(string method,params object[] args){try{if(method=="Bind"&&args.Length==6)args=args.Concat(new object[]{null}).ToArray();return host.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(host,args);}catch(TargetInvocationException e){throw e.InnerException;}}
    void Open()
    {
        owner=true;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Battle.CombatPlayerScene")).FirstOrDefault(t=>t!=null);
        Assert.IsNotNull(type,"A Player scene consumer must own first-render gating and teardown.");
        binding=AssetDatabase.LoadAssetAtPath<CombatPlayerAssetBinding>("Assets/_LASTRAIL/QA/CombatAssetBinding/89e63936213b4ce090e6a03fbb98bbe7/Binding.asset");Assert.IsNotNull(binding);
        spectator=binding.Acquire(binding.ManifestHash,Squad);
        driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>owner);driver.Open();
        hostObject=new GameObject("Test combat host");host=hostObject.AddComponent(type);
        inputObject=new GameObject("Borrowed input",typeof(EventSystem));
        Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null);
    }
    [TearDown] public void Cleanup(){if(hostObject!=null)UnityEngine.Object.DestroyImmediate(hostObject);if(inputObject!=null)UnityEngine.Object.DestroyImmediate(inputObject);spectator?.Dispose();}
    [Test] public void PreparationNeverAdvancesAndClosedSceneCannotAdvanceOrDestroySharedAssets()
    {
        Open();string before=driver.StateJson;Call("AdvanceFrame",10d);Assert.AreEqual(before,driver.StateJson);
        var staleClick=hostObject.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Speed").onClick;
        Call("Close");staleClick.Invoke();Call("AdvanceFrame",10d);Assert.AreEqual(before,driver.StateJson);
        Assert.IsNotNull(spectator.ReadAssets().ExteriorMap);Assert.IsTrue(inputObject!=null);
        Assert.AreEqual(0,hostObject.transform.childCount);
    }
    [Test] public void DuplicateBindingIsRejectedWithoutReplacingCurrentDriver()
    {
        Open();var extra=binding.Acquire(binding.ManifestHash,Squad);
        try{Assert.Throws<InvalidOperationException>(()=>Call("Bind",driver,extra,inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null));}
        finally{extra.Dispose();}
        Assert.AreEqual(0,driver.Tick);
    }
    [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)][TestCase(true,true)] public void ChangedSourceIsHiddenBeforeFirstCameraDraw(bool changedOwner,bool paused)
    {
        Open();var camera=(Camera)host.GetType().GetProperty("SceneCamera").GetValue(host);var target=new RenderTexture(720,1280,24);
        try{
            int failures=0;host.GetType().GetField("failed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(host,(Action<Exception>)(_=>failures++));
            if(paused)Call("OnApplicationPause",true);
            if(changedOwner)owner=false;else driver.SetSpeed(4);
            string original=driver.StateJson;camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();
            Assert.IsFalse(hostObject.GetComponent<CombatPlayerScene>().View.gameObject.activeInHierarchy,"A stale run or revision must be hidden before its first camera draw.");
            Assert.AreEqual(0,camera.cullingMask);Assert.AreEqual(CameraClearFlags.Nothing,camera.clearFlags);
            Assert.IsFalse((bool)host.GetType().GetProperty("IsRunning").GetValue(host));
            // No log-only failure callback: teardown is checked separately by Close.
            Assert.AreEqual(original,driver.StateJson);Call("Close");Assert.AreEqual(0,hostObject.transform.childCount);
        }finally{if(camera!=null)camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    [Test] public void RestoredDamageIsTheFirstDisplayedHpAndNextRunDoesNotReuseIt()
    {
        Open();Call("Close");var original=Fresh();var state=JObject.Parse(original.CaptureLocalState());
        string train=(string)((JArray)original.ReadHudState()["actors"]).Single(x=>(bool)x["train"])["id"];
        ((JArray)state["actors"]).Single(x=>(string)x["id"]==train)["vitals"]["hp"]=1;
        var restored=BasicCombatSimulation.RestoreLocalState(state.ToString(),Fresh);
        var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{restored,7L},null);
        driver=new CombatSceneDriver(()=>throw new Exception("Restored run must not use fresh defaults"),()=>checkpoint,(s,r)=>throw new Exception("Display must not save or advance"),()=>owner);driver.Open();
        Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null);
        var meter=hostObject.GetComponentsInChildren<CombatMeter>(true).Single(x=>x.name=="TrainHp");
        Assert.Less(meter.Value,1);Assert.AreEqual(1f/(int)driver.HudState["trainMaxHp"],meter.Value,0.00001f);
        Assert.AreEqual(1,(int)driver.HudState["trainHp"]);Call("Close");Assert.IsTrue(meter==null);
        driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>owner);driver.Open();
        Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null);
        Assert.AreEqual(1,hostObject.GetComponentsInChildren<CombatMeter>(true).Single(x=>x.name=="TrainHp").Value);
        Assert.IsFalse((bool)host.GetType().GetProperty("IsRunning").GetValue(host));Assert.AreEqual(0,driver.Tick);
    }
    [Test] public void FailedAssetBindingLeavesNoDefaultBattleAndDoesNotAdvanceRun()
    {
        Open();Call("Close");var lease=binding.Acquire(binding.ManifestHash,Squad);lease.Dispose();string original=driver.StateJson;
        Assert.Throws<ObjectDisposedException>(()=>Call("Bind",driver,lease,inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null));
        Assert.AreEqual(0,hostObject.transform.childCount);Assert.AreEqual(original,driver.StateJson);Assert.IsFalse((bool)host.GetType().GetProperty("IsOpen").GetValue(host));
        Assert.IsNotNull(spectator.ReadAssets().ExteriorMap);
    }
    [Test] public void ActualCameraRenderArmsOnceThenOwnerChangeBlocksFurtherFrames()
    {
        Open();var camera=(Camera)host.GetType().GetProperty("SceneCamera").GetValue(host);var target=new RenderTexture(720,1280,24);
        try
        {
            Assert.IsFalse(driver.RecoveryRequired,"Before render");camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();Assert.IsFalse(driver.RecoveryRequired,"After render");
            Call("AdvanceFrame",30d);Assert.AreEqual(0,driver.Tick,"Loading frame must be discarded.");
            Call("AdvanceFrame",.05d);Assert.AreEqual(1,driver.Tick);
            camera.Render();Call("AdvanceFrame",.05d);Assert.AreEqual(2,driver.Tick,"Repeated render must not restart.");
            string before=driver.StateJson;owner=false;Call("AdvanceFrame",1d);Assert.AreEqual(before,driver.StateJson);
        }
        finally{if(camera!=null)camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);}
    }
    [Test] public void ClosingPresentationDestroysItsGeneratedSpritesAndPreservesBorrowedTexture()
    {
        var before=UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>().Select(s=>s.GetInstanceID()).ToArray();
        Open();
        var loaded=UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>().Where(s=>!before.Contains(s.GetInstanceID())).ToArray();
        Debug.Log("Sprite lifetime fixture: newly loaded imported="+loaded.Count(EditorUtility.IsPersistent)+", generated="+loaded.Count(s=>!EditorUtility.IsPersistent(s)));
        var generated=loaded.Where(s=>!EditorUtility.IsPersistent(s)).ToArray();
        Assert.GreaterOrEqual(generated.Length,4);
        var texture=generated[0].texture;
        Call("Close");
        Assert.IsTrue(generated.All(s=>s==null),"Presentation-created sprites must be destroyed by their owner.");
        Assert.IsTrue(texture!=null,"Borrowed texture must survive presentation release.");
    }
    [Test] public void OwnerChangeBeforeActivationAlsoReleasesGeneratedSprites()
    {
        Open();Call("Close");
        var before=UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>().Select(s=>s.GetInstanceID()).ToArray();
        int checks=0;
        Assert.Throws<InvalidOperationException>(()=>Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>++checks==1),null,null));
        Assert.IsFalse(UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>().Any(s=>!before.Contains(s.GetInstanceID())));
        Assert.AreEqual(0,driver.Tick);
    }
    [Test] public void ExternalTerminalCommitCompletesWithoutPollingAnotherHostFrame()
    {
        Open();Call("Close");int notifications=0;
        Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),(Action<CombatSceneResult>)(_=>notifications++),null);
        ((Camera)host.GetType().GetProperty("SceneCamera").GetValue(host)).Render();
        for(int i=0;i<2000&&driver.Outcome==BasicCombatOutcome.Running;i++)driver.Advance(5000000,10);
        Assert.AreEqual(1,notifications,"The terminal commit itself must complete the scene.");
        Call("AdvanceFrame",.05d);Assert.AreEqual(1,notifications);
    }
    [Test] public void TerminalCallbackCanBindNextSceneWithoutOldCleanupClosingIt()
    {
        Open();Call("Close");
        for(int i=0;i<2000&&driver.Outcome==BasicCombatOutcome.Running;i++)driver.Advance(5000000,10);
        Assert.AreNotEqual(BasicCombatOutcome.Running,driver.Outcome);
        int notifications=0;var next=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>owner);next.Open();
        Action<CombatSceneResult> finished=result=>{notifications++;Call("Bind",next,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),null,null);};
        Call("Bind",driver,binding.Acquire(binding.ManifestHash,Squad),inputObject.GetComponent<EventSystem>(),(Func<bool>)(()=>owner),finished,null);
        var oldCamera=(Camera)host.GetType().GetProperty("SceneCamera").GetValue(host);oldCamera.Render();
        Call("AdvanceFrame",.05d);Call("AdvanceFrame",.05d);
        Assert.AreEqual(1,notifications);Assert.IsNotNull(host.GetType().GetProperty("SceneCamera").GetValue(host));
        Assert.AreEqual(0,next.Tick);Call("AdvanceFrame",10d);Assert.AreEqual(0,next.Tick);
    }
}



