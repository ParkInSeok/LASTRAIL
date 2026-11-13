using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
public sealed class EquipmentInventoryConnectionTests {
 const string QA="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/equipment-inventory/";
 static string Packet(string account="a",string run="r")=>new JObject{["version"]="equipment.inventory-display.v1.20261111",["accountId"]=account,["runId"]=run,["revision"]=1,["capacitySlots"]=30,["usedSlots"]=0,["items"]=new JArray()}.ToString();
 static GameApiAuthSessionSnapshot Session(string id)=>new GameApiAuthSessionSnapshot(id,"ACTIVE",new GameApiAccessToken("lst1.fixture-token"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("refresh"),DateTime.UtcNow.AddMinutes(10));
 sealed class Wire:IRunApiHttpTransport {public int Count;public Func<Task<RunApiHttpResponse>> Reply;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Count++;return Reply();}}
 [Test] public async Task ReadsCoalesceAndReopenUsesAccountOwnedSnapshot(){var session=Session("a");var reply=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>reply.Task};using(var account=new AccountScreenRuntime(wire,null,()=>session,()=>1)){var first=account.ReadEquipmentInventoryAsync("r");var second=account.ReadEquipmentInventoryAsync("r");Assert.AreSame(first,second);reply.SetResult(RunApiHttpResponse.Success(200,Packet()));await first;await account.ReadEquipmentInventoryAsync("r");Assert.AreEqual(1,wire.Count);Assert.NotNull(account.EquipmentInventory("r").Snapshot);}}
 [Test] public async Task CredentialRotationReusesValidDataAndExplicitRefreshReadsOnce(){var session=Session("a");var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Packet()))};using(var account=new AccountScreenRuntime(wire,null,()=>session,()=>1)){await account.ReadEquipmentInventoryAsync("r");var snapshot=account.EquipmentInventory("r").Snapshot;session=Session("a");await account.ReadEquipmentInventoryAsync("r");Assert.AreSame(snapshot,account.EquipmentInventory("r").Snapshot);Assert.AreEqual(1,wire.Count);await account.ReadEquipmentInventoryAsync("r",true);Assert.AreEqual(2,wire.Count);}}
 [Test] public async Task OldAccountResponseCannotPopulateNewAccount(){var session=Session("a");long generation=1;var reply=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>reply.Task};using(var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation)){var old=account.ReadEquipmentInventoryAsync("r");session=Session("b");generation++;var next=account.EquipmentInventory("r");reply.SetResult(RunApiHttpResponse.Success(200,Packet()));try{await old;Assert.Fail("Old request must be rejected");}catch(OperationCanceledException){}Assert.IsNull(next.Snapshot);}}
 [Test] public async Task SharedAtlasLeaseUnloadsOnlyAfterLastOwnerAndCanReload(){int loads=0,unloads=0;var texture=new Texture2D(1,1);try{var pending=new TaskCompletionSource<Texture2D>();var pool=new EquipmentIconResources(()=>{loads++;return pending.Task;},t=>unloads++);var first=pool.AcquireAsync();var second=pool.AcquireAsync();Assert.AreEqual(1,loads);pending.SetResult(texture);var a=await first;var b=await second;a.Dispose();a.Dispose();Assert.AreEqual(0,unloads);b.Dispose();Assert.AreEqual(1,unloads);var c=await pool.AcquireAsync();Assert.AreEqual(2,loads);c.Dispose();Assert.AreEqual(2,unloads);}finally{UnityEngine.Object.DestroyImmediate(texture);}}
 [Test] public void UnknownResourceKeyNeverBecomesAPath(){Assert.IsFalse(EquipmentIconResources.TryRegion("../../private",out _));Assert.IsTrue(EquipmentIconResources.TryRegion("equipment.category.atk",out _));}
 [UnityTest] public IEnumerator ServerSnapshotRendersKeyedIconAndReleasesOwnedReferences(){
  UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
  yield return new EnterPlayMode();
  var preparation=EquipmentAddressableTests.LocalBackend();while(!preparation.IsCompleted)yield return null;if(preparation.IsFaulted)throw preparation.Exception;var backend=preparation.Result;var pool=new EquipmentIconResources(backend);
  string json=System.IO.File.ReadAllText(QA+"server-read-fixture.json");var packet=JObject.Parse(json);var model=EquipmentInventoryModel.Read("a",(string)packet["runId"],json);Assert.AreEqual("공격력 장비",model.Items[0].Name);StringAssert.Contains("25%",model.Items[0].Effects);
  var root=new GameObject("ConnectedInventory",typeof(RectTransform),typeof(Canvas));var cameraObject=new GameObject("InventoryCamera",typeof(Camera));var target=new RenderTexture(1600,900,24);var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try{var controller=new EquipmentInventoryController("a",model.RunId);controller.Apply(model);var camera=cameraObject.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var view=EquipmentInventoryView.Open(root.transform,font,controller,null,pool);controller.Select(model.Items[0].Id);
   System.IO.File.WriteAllText(QA+"addressable-lifecycle-diagnostic.json",new JObject{["playing"]=Application.isPlaying,["immediateFirstRowActive"]=view.transform.Find("List/Rows/Row0").gameObject.activeInHierarchy}.ToString());
   yield return null;
   var icon=view.transform.Find("List/Rows/Row0/EquipmentIcon:equipment.category.atk").GetComponent<RawImage>();for(int i=0;i<180&&icon.texture==null;i++)yield return null;Assert.NotNull(icon.texture);var texture=icon.texture;
   long bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();System.IO.File.WriteAllBytes(QA+"addressable-connected-list-detail.png",pixels.EncodeToPNG());
   view.gameObject.SetActive(false);Assert.IsFalse((bool)typeof(EquipmentInventoryView).GetField("bound",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view),"OnDisable detaches the view");Assert.IsNull(icon.texture);var users=(int)typeof(EquipmentIconResources).GetField("users",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(pool);var pending=typeof(EquipmentIconResources).GetField("pending",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(pool);System.IO.File.WriteAllText(QA+"addressable-release-diagnostic.json",new JObject{["users"]=users,["cacheRetainsTask"]=pending!=null,["textureAliveImmediately"]=texture!=null}.ToString());yield return null;Assert.AreEqual(0,users);Assert.IsNull(pending);System.IO.File.WriteAllText(QA+"addressable-atlas-memory.json",new JObject{["loadedTextureBytes"]=bytes,["ownedReferencesReleased"]=true,["nativeTextureInvalidated"]=texture==null,["scope"]="Editor texture allocation only, not process memory savings"}.ToString());
  }finally{backend.Dispose();RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);}
  yield return new ExitPlayMode();
 }
}
