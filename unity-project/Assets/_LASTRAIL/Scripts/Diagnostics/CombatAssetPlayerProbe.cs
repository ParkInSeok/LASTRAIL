#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Diagnostics
{
    // Asset binding trial only. No auth, START, simulation or economic transport.
    public sealed class CombatAssetPlayerProbe : MonoBehaviour
    {
        [SerializeField] CombatPlayerAssetBinding binding;
        [SerializeField] string expectedHash;
        SceneMemoryRecorder memory;
        static readonly string[] Squad={"op_kyle","op_reina","op_eve","op_sera","op_mira"};
        public void Initialize(CombatPlayerAssetBinding value){binding=value;expectedHash=value.ManifestHash;}
        async void Start()
        {
            string output=null;bool ownsOutput=false;
            try
            {
                output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_COMBAT_ASSET_TRIAL")??throw new InvalidOperationException("Explicit output required"));
                if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");
                Directory.CreateDirectory(output);ownsOutput=true;memory=SceneMemoryRecorder.CreateExternal(output,true);
                memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident-build-assets");
                for(int cycle=0;cycle<5;cycle++)
                {
                    memory.BeginWindow();
                    using(var one=binding.Acquire(expectedHash,Squad))
                    {
                        var two=binding.Acquire(expectedHash,Squad);var config=two.ReadAssets();two.Dispose();
                        if(one.ReadAssets()!=config||config.ExteriorMap==null)throw new InvalidOperationException("Shared resource invalidated");
                        await Task.Delay(250);
                    }
                    await Task.Delay(250);memory.Checkpoint("cycle-"+cycle+"-leases-released");
                }
                memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("06-natural-shared-resident");
                File.WriteAllText(Path.Combine(output,"result.json"),new JObject{["status"]="Passed",["scope"]="STANDALONE_ASSET_BINDING_ONLY_NOT_BATTLE_ENTRY",
                    ["manifestHash"]=expectedHash,["unityVersion"]=Application.unityVersion,["cycles"]=5,["transportComposed"]=false,
                    ["startSent"]=false,["combatTested"]=false,["sharedAssetOwner"]="SERIALIZED_SCENE_BINDING",
                    ["forcedGc"]=false,["memoryScope"]="Resident build references, validation and lease allocation; not load/unload or whole-game memory"}.ToString());
                Destroy(memory.gameObject);memory=null;binding=null;Application.Quit(0);
            }
            catch(Exception error)
            {
                if(ownsOutput)File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());
                else Debug.LogError("Combat asset trial output rejected: "+error.GetType().Name);
                if(memory!=null)Destroy(memory.gameObject);Application.Quit(1);
            }
        }
    }
}
#endif
