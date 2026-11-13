#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Presentation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class ApprovedUiConnectionImport {
 const string Qa="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/";
 const string PathName="Assets/_LASTRAIL/Content/RemoteResources/ApprovedUi/Connections20261110.asset";
 static JArray entries;
 static string Source(string family,string id){var row=entries.Single(e=>(string)e["family"]==family&&(string)e["id"]==id);string path=((string)row["asset"]).Substring("unity-project/".Length);using var sha=SHA256.Create();if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant()!=(string)row["assetSha256"])throw new InvalidDataException("Approved UI bytes differ: "+id);return path;}
 static Texture2D Texture(string family,string id)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Source(family,id));
 static Sprite Frame(string family,string id,Vector4 border){
  string path=Source(family,id);string backup=Qa+"before/"+family+"-"+id+".png.meta";if(!File.Exists(backup))File.Copy(path+".meta",backup);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=border;importer.spritePixelsPerUnit=100;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 public static void ApplyAndCapture(){
  Apply();
  const string bindingPath="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryBinding.asset";
  string backup=Qa+"before/OrdinaryBinding.asset";if(!File.Exists(backup))File.Copy(bindingPath,backup);
  if(!File.Exists(Qa+"before/dynamic-resource-closure.json"))File.Copy(Ce03DynamicResourceClosure.ManifestPath,Qa+"before/dynamic-resource-closure.json");
  Ce03DynamicResourceClosure.Export();var config=AssetDatabase.LoadAssetAtPath<LastRail.Battle.CombatPresentationAssets>("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/OrdinaryPresentation.asset");
  var baked=CombatPlayerAssetBindingBuilder.BakeConfiguration(config);var existing=AssetDatabase.LoadAssetAtPath<LastRail.Battle.CombatPlayerAssetBinding>(bindingPath);EditorUtility.CopySerialized(baked,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(baked);AssetDatabase.SaveAssets();
  using(var lease=existing.Acquire(existing.ManifestHash,new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}))File.WriteAllText(Qa+"player-binding.json",existing.ManifestJson);
  ApprovedUiConnectionCapture.Run();ApprovedEquipmentFuelCapture.Run();
 }
 public static void Apply(){
  Directory.CreateDirectory(Qa+"before");entries=(JArray)JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/ApprovedUi20261110/catalog.json"))["entries"];
  var art=AssetDatabase.LoadAssetAtPath<ApprovedUiConnections>(PathName);bool fresh=art==null;if(fresh)art=ScriptableObject.CreateInstance<ApprovedUiConnections>();
  art.DialogFrame=Frame("common-dialog-independent-v02","dialog-frame",Vector4.one*160);
  art.ButtonFrame=Frame("common-dialog-independent-v02","button-frame",new Vector4(190,240,190,240));
  art.PanelFrame=Frame("panel-frame-composition-v01","panel-frame",Vector4.one*150);
  art.ItemFrame=Frame("equipment-composition-v60-review","item-frame",Vector4.one*70);
  art.CurrentLocation=Texture("skill-map-icons-v01","map-current");art.Fuel=Texture("skill-map-icons-v01","currency");
  art.DepartureCabin=Texture("d01-remaining-art-v01","departure-cabin");art.Pilot=Texture("d01-remaining-art-v01","pilot");
  art.Medal=Texture("common-dialog-independent-v02","medal");art.ButtonPlate=Texture("common-dialog-independent-v02","blue-plate");art.PanelTexture=Texture("d01-remaining-art-v01","panel-texture");art.MapBackground=Texture("d01-remaining-art-v01","map-landscape");
  art.Battle=Texture("skill-map-icons-v01","map-battle");art.Equipment=Texture("skill-map-icons-v01","map-equipment");art.Enchant=Texture("skill-map-icons-v01","map-enchant");art.Elite=Texture("operation-map-polish-v02","map-elite");art.Boss=Texture("operation-map-polish-v02","map-boss");
  if(art.DepartureCabin==null||art.Pilot==null||art.ItemFrame==null||art.CurrentLocation==null||art.Fuel==null||art.DialogFrame==null||art.ButtonFrame==null||art.PanelFrame==null||art.Medal==null||art.ButtonPlate==null||art.PanelTexture==null||art.MapBackground==null||new[]{"COMBAT","ELITE","BOSS","EQUIPMENT","ENCHANT"}.Any(k=>art.MapIcon(k)==null))throw new InvalidDataException("UI resource binding incomplete");
  Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));if(fresh)AssetDatabase.CreateAsset(art,PathName);else EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
  File.WriteAllText(Qa+"ui-binding.json",new JObject{["asset"]=PathName,["connectedUniqueAssets"]=17,["consumers"]=new JArray("LobbyDestinationPages.Notice","OrdinaryTravelPanel.ShowChoices","LobbyEquipmentMultishot","DeparturePreparationView"),["newNetworkCalls"]=0,["voicePlaybackAdded"]=false}.ToString());
 }
}
#endif
