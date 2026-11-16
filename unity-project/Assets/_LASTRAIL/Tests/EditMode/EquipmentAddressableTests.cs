using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
public sealed class EquipmentAddressableTests {
 const string Path="Assets/_LASTRAIL/Content/ApprovedUi20261110/d01-remaining-art-v01/items-atlas.png";
 [Test] public void AtlasIsRemoteRequiredAndNotPackagedInResources(){
  var guid=AssetDatabase.AssetPathToGUID(Path);Assert.IsNotEmpty(guid,"Atlas must move out of Resources with its GUID");
  var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);Assert.NotNull(entry);
  Assert.AreEqual(Path,entry.address);Assert.IsTrue(entry.labels.Contains(AddressableContentBackend.RequiredLabel));
  Assert.AreEqual("remote_equipment_icons",entry.parentGroup.Name);
  var schema=entry.parentGroup.GetSchema<BundledAssetGroupSchema>();Assert.AreEqual(typeof(CacheOnlyAssetBundleProvider),schema.AssetBundleProviderType.Value);
  Assert.IsFalse(System.IO.File.Exists("Assets/_LASTRAIL/Resources/EquipmentIcons/items-atlas.png"));
  Assert.That(AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",true),Does.Not.Contain(Path));
 }
 [Test] public async Task DisposedLeaseDropsItsTextureReference(){var texture=new Texture2D(1,1);try{var pool=new EquipmentIconResources(()=>Task.FromResult(texture),_=>{});var lease=await pool.AcquireAsync();lease.Dispose();Assert.IsNull(lease.Texture);}finally{Object.DestroyImmediate(texture);}}

 public static async Task<AddressableContentBackend> LocalBackend(){
  using var timeout=new System.Threading.CancellationTokenSource(System.TimeSpan.FromMinutes(5));
  var policy=new DownloadAccessPolicy();policy.SetNetwork(ContentNetwork.Wifi);
  var backend=new AddressableContentBackend(null,true,policy,source:ContentResourceSource.EditorLocal);
  var gate=typeof(ClientContentGate).GetMethod("Approved",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"test-release","test-version","https://unused.invalid/catalog.bin","sha256:"+new string('0',64),false});
  typeof(AddressableContentBackend).GetField("approvedGate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(backend,gate);
  try{await backend.InitializeAsync(timeout.Token);await backend.UpdateCatalogAsync(timeout.Token);await backend.RequiredBytesAsync(timeout.Token);await backend.DownloadAsync((a,b)=>{},timeout.Token);return backend;}catch{backend.Dispose();throw;}
 }
 [Test] public void UnapprovedBackendCannotLoadIcons(){using(var backend=new AddressableContentBackend(null,true)){var pool=new EquipmentIconResources(backend);Assert.ThrowsAsync<System.InvalidOperationException>(async()=>await pool.AcquireAsync());}}
 [Test] public async Task FailedLoadReleasesOwnershipAndNextAttemptSucceeds(){int attempts=0,releases=0;var texture=new Texture2D(1,1);try{var pool=new EquipmentIconResources(()=>++attempts==1?Task.FromException<Texture2D>(new System.InvalidOperationException("fixture")):Task.FromResult(texture),_=>releases++);Assert.ThrowsAsync<System.InvalidOperationException>(async()=>await pool.AcquireAsync());var lease=await pool.AcquireAsync();Assert.AreSame(texture,lease.Texture);lease.Dispose();Assert.AreEqual(2,attempts);Assert.AreEqual(1,releases);}finally{Object.DestroyImmediate(texture);}}
 [Test] public async Task CloseBeforeLoadAndReentryCannotRepaintOldView(){
  var pending=new TaskCompletionSource<Texture2D>();int loads=0,releases=0;
  var pool=new EquipmentIconResources(()=>{loads++;return pending.Task;},_=>releases++);
  var texture=new Texture2D(1,1);var root=new GameObject("InventoryCancel",typeof(RectTransform),typeof(Canvas));
  try{
   var json=System.IO.File.ReadAllText("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/equipment-inventory/server-read-fixture.json");var packet=Newtonsoft.Json.Linq.JObject.Parse(json);var model=EquipmentInventoryModel.Read("a",(string)packet["runId"],json);var owner=new EquipmentInventoryController("a",model.RunId);owner.Apply(model);
   var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
   var old=EquipmentInventoryView.Open(root.transform,font,owner,null,pool);old.Unbind();old.gameObject.SetActive(false);
   var next=EquipmentInventoryView.Open(root.transform,font,owner,null,pool);Assert.AreEqual(1,loads);
   pending.SetResult(texture);await Task.Yield();await Task.Yield();
   foreach(var image in old.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))if(image.name.StartsWith("EquipmentIcon:"))Assert.IsNull(image.texture);
   Assert.AreSame(texture,next.transform.Find("List/Rows/Row0/EquipmentIcon:equipment.category.atk").GetComponent<UnityEngine.UI.RawImage>().texture);
   Assert.AreEqual(0,releases);next.Unbind();Assert.AreEqual(1,releases);
   next.Bind(owner);await Task.Yield();Assert.AreEqual(2,loads);next.Unbind();Assert.AreEqual(2,releases);
  }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(texture);}
 }

 [Test] public async Task DestroyDuringLoadReleasesLateResultWithoutReentry(){
  var pending=new TaskCompletionSource<Texture2D>();int releases=0;
  var pool=new EquipmentIconResources(()=>pending.Task,_=>releases++);var texture=new Texture2D(1,1);var root=new GameObject("DestroyedInventory",typeof(RectTransform),typeof(Canvas));
  try{var json=System.IO.File.ReadAllText("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/equipment-inventory/server-read-fixture.json");var packet=Newtonsoft.Json.Linq.JObject.Parse(json);var model=EquipmentInventoryModel.Read("a",(string)packet["runId"],json);var owner=new EquipmentInventoryController("a",model.RunId);owner.Apply(model);
   var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");EquipmentInventoryView.Open(root.transform,font,owner,null,pool);
   Object.DestroyImmediate(root);pending.SetResult(texture);await Task.Yield();await Task.Yield();Assert.AreEqual(1,releases);
  }finally{if(root!=null)Object.DestroyImmediate(root);Object.DestroyImmediate(texture);}
 }
}
