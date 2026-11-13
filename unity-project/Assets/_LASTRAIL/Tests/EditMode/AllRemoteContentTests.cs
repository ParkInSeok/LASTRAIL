using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
public sealed class AllRemoteContentTests {
 [Test] public void SharedUiIsRemoteAndCannotBePulledIntoBootstrap(){
  const string path="Assets/_LASTRAIL/Content/RemoteResources/ApprovedUi/Connections20261110.asset";
  Assert.IsFalse(System.IO.File.Exists("Assets/_LASTRAIL/Resources/ApprovedUi/Connections20261110.asset"));
  var guid=AssetDatabase.AssetPathToGUID(path);Assert.IsNotEmpty(guid);
  var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);Assert.NotNull(entry);
  Assert.IsTrue(entry.labels.Contains("launch_required"));
  Assert.That(AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",true),Does.Not.Contain(path));
 }
}
