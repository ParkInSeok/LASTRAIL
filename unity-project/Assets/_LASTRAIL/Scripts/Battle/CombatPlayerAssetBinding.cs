using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle
{
    // Build-time file provenance and Player object references, not admission authority.
    // The scene/build owns these shared assets. A consumer owns only its lease.
    public sealed class CombatPlayerAssetBinding : ScriptableObject
    {
        [SerializeField] string manifestJson;
        [SerializeField] CombatPresentationAssets configuration;
        [SerializeField] UnityEngine.Object[] objects;
        public string ManifestHash => Ce03CommandContract.Sha(manifestJson??"");
        public string ManifestJson => manifestJson;

        public Lease Acquire(string expectedManifestHash,string[] formation)
        {
            if(string.IsNullOrEmpty(manifestJson)||expectedManifestHash!=ManifestHash)
                throw new InvalidOperationException("Battle asset manifest version differs.");
            var manifest=JObject.Parse(manifestJson);
            if((string)manifest["version"]!="combat-player-assets-v1"||(string)manifest["unityVersion"]!=Application.unityVersion)
                throw new InvalidOperationException("Battle asset build version differs.");
            var squad=((JArray)manifest["formation"]).Values<string>().ToArray();
            if(formation==null||formation.Length!=5||formation.Distinct(StringComparer.Ordinal).Count()!=5||
                !formation.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(squad.OrderBy(x=>x,StringComparer.Ordinal)))
                throw new InvalidOperationException("Battle asset formation is unsupported.");
            var rows=(JArray)manifest["runtimeObjects"];
            if(configuration==null||objects==null||rows.Count!=objects.Length||!objects.Contains(configuration))
                throw new InvalidOperationException("Battle asset references are incomplete.");
            for(int i=0;i<objects.Length;i++)
            {
                var value=objects[i];var row=rows[i];
                if(value==null||value.GetType().FullName!=(string)row["type"]||value.name!=(string)row["name"])
                    throw new InvalidOperationException("Battle asset object differs: "+(string)row["id"]);
                if(value is Texture2D texture&&(texture.width!=(int)row["width"]||texture.height!=(int)row["height"]))
                    throw new InvalidOperationException("Battle texture dimensions differ.");
            }
            foreach(var role in (JArray)manifest["roles"])
            {
                var field=typeof(CombatPresentationAssets).GetField((string)role["field"]);
                if(field==null)throw new InvalidOperationException("Unknown presentation role.");
                object actual=field.GetValue(configuration);int index=(int)role["index"];
                if(index>=0)
                {
                    if(actual is not Array array||index>=array.Length)throw new InvalidOperationException("Presentation role array differs.");
                    actual=array.GetValue(index);
                }
                int reference=(int)role["reference"];
                if(reference<0||reference>=objects.Length||!ReferenceEquals(actual,objects[reference]))
                    throw new InvalidOperationException("Presentation role reference differs: "+field.Name);
            }
            if(!JToken.DeepEquals(manifest["configurationSettings"],CaptureSettings(configuration)))
                throw new InvalidOperationException("Presentation lookup or layout settings differ.");
            if(!JToken.DeepEquals(manifest["nestedSettings"]??JValue.CreateNull(),CaptureNestedSettings(configuration,objects)))
                throw new InvalidOperationException("Nested presentation settings differ.");
            RequirePresentation(configuration);
            return new Lease(this);
        }

        public static JToken CaptureNestedSettings(CombatPresentationAssets config,UnityEngine.Object[] references)
        {
            if(config.OperatorFrames==null&&config.OrdinaryRoof==null)return JValue.CreateNull();
            var result=new JObject();
            foreach(var pair in new[]{("operators",(UnityEngine.Object)config.OperatorFrames),("roof",(UnityEngine.Object)config.OrdinaryRoof)})
            {
                if(pair.Item2==null){result[pair.Item1]=null;continue;}
                if(!references.Contains(pair.Item2))throw new InvalidOperationException("Nested art registry is outside the binding.");
                var data=JObject.Parse(JsonUtility.ToJson(pair.Item2));
                foreach(var row in data.Descendants().OfType<JObject>().Where(o=>o.Property("instanceID")!=null).ToArray())
                {
                    int id=(int)row["instanceID"],index=id==0?-1:Array.FindIndex(references,o=>o!=null&&o.GetInstanceID()==id);
                    if(id!=0&&index<0)throw new InvalidOperationException("Nested art reference is outside the binding.");
                    row.RemoveAll();row["reference"]=index;
                }
                result[pair.Item1]=data;
            }
            return result;
        }
        public static JObject CaptureSettings(CombatPresentationAssets config)
        {
            var result=JObject.Parse(JsonUtility.ToJson(config));
            foreach(var field in typeof(CombatPresentationAssets).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))
                if(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)||
                    field.FieldType.IsArray&&typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType.GetElementType()))result.Remove(field.Name);
            return result;
        }

        public static void RequirePresentation(CombatPresentationAssets config)
        {
            if(config==null||config.Font==null||config.ExteriorMap==null||config.ExteriorTrain==null||config.Interior==null||
                config.Frame==null||config.Primary==null||config.Secondary==null||config.Hit==null||config.Shield==null||
                config.Console==null||config.IconAtlas==null||config.Buggy==null||config.Bike==null||
                config.SecondFinalResources==null||config.SecondFinalResources.Length!=22||config.SecondFinalResources.Any(x=>x==null)||
                Ce03PresentationManifest.Actors.Any(id=>config.ActorAtlas(id)==null)||config.Keys==null||config.Frames==null||
                config.Keys.Length!=160||config.Frames.Length!=160||config.Frames.Any(x=>x==null))
                throw new InvalidOperationException("Approved combat presentation is incomplete.");
        }

#if UNITY_EDITOR
        public void InitializeForBuild(string manifest,CombatPresentationAssets config,UnityEngine.Object[] references)
        {
            if(manifestJson!=null)throw new InvalidOperationException("A build binding is immutable.");
            manifestJson=manifest;configuration=config;objects=(UnityEngine.Object[])references.Clone();
        }
#endif
        public sealed class Lease : IDisposable
        {
            CombatPlayerAssetBinding owner;
            internal Lease(CombatPlayerAssetBinding owner){this.owner=owner;}
            public CombatPresentationAssets ReadAssets()
            {
                if(owner==null)throw new ObjectDisposedException(nameof(Lease));
                return owner.configuration;
            }
            public void Dispose(){owner=null;}
        }
    }
}
