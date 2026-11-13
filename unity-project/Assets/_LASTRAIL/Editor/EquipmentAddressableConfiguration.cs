#if UNITY_EDITOR
using LastRail.Resources;
using LastRail.Presentation.MissionShell;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
namespace LastRail.Editor {
 public static class EquipmentAddressableConfiguration {
  public static void Configure(){
   var settings=AddressableAssetSettingsDefaultObject.Settings;
   const string name="remote_equipment_icons";
   var group=settings.FindGroup(name)??settings.CreateGroup(name,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
   var schema=group.GetSchema<BundledAssetGroupSchema>();
   schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);
   schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);
   schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackTogether;
   schema.UseAssetBundleCache=true;schema.UseAssetBundleCrc=true;schema.UseUnityWebRequestForLocalBundles=true;schema.Timeout=30;schema.RetryCount=0;
   var provider=schema.AssetBundleProviderType;provider.Value=typeof(CacheOnlyAssetBundleProvider);schema.AssetBundleProviderType=provider;
   var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(EquipmentIconResources.AtlasAddress),group);
   entry.address=EquipmentIconResources.AtlasAddress;entry.SetLabel(name,true,true);entry.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
   EditorUtility.SetDirty(schema);EditorUtility.SetDirty(group);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
  }
 }
}
#endif
