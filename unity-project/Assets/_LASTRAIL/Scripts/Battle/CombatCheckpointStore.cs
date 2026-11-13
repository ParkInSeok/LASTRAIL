using System;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public sealed class RestoredCombatCheckpoint
    {
        public BasicCombatSimulation Simulation { get; }
        public long Revision { get; }
        internal RestoredCombatCheckpoint(BasicCombatSimulation simulation,long revision){Simulation=simulation;Revision=revision;}
    }
    // Storage slice for the current local model. Not the full r2 settlement/admission
    // envelope. Callers supply the retained, verified START and trusted fresh config.
    public sealed class CombatCheckpointStore
    {
        readonly AndroidEncryptedReceiptFilePort file;
        readonly string lockPath;
        readonly Func<string> editorRead;
        readonly Action<string> editorWrite;
        public CombatCheckpointStore(IAndroidKeystoreNativeStoragePort crypto,string path)
        {file=new AndroidEncryptedReceiptFilePort(crypto,path,"lastrail_combat_checkpoint_v1:");lockPath=Path.GetFullPath(path)+".lock";}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
        // Explicit disposable Editor file port; not Android Keystore or shipping
        // account storage. Reuses the same lock/CAS/identity/checkpoint validator.
        public CombatCheckpointStore(string localPath,Func<string> read,Action<string> write)
        {lockPath=Path.GetFullPath(localPath)+".lock";editorRead=read??throw new ArgumentNullException(nameof(read));editorWrite=write??throw new ArgumentNullException(nameof(write));}
        // Plaintext disposable LOCAL QA only. Never a shipping secure-storage fallback.
        // Replace atomically while SaveIdentity holds the existing interprocess lock.
        public static CombatCheckpointStore CreateExplicitLocalQaFile(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("Absolute LOCAL QA checkpoint path required.",nameof(path));
            path=Path.GetFullPath(path);
            return new CombatCheckpointStore(path,
                ()=>File.Exists(path)?File.ReadAllText(path):null,
                raw=>{
                    string pending=path+"."+Guid.NewGuid().ToString("N")+".pending";
                    try{
                        var bytes=new System.Text.UTF8Encoding(false,true).GetBytes(raw);
                        using(var stream=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                        {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                        if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);
                    }finally{if(File.Exists(pending))File.Delete(pending);}
                });
        }
#endif
        IDisposable Acquire()
        {
            if(editorRead==null&&(!file.IsAndroidRuntime||!file.IsKeystoreAvailable||file.UsedPlayerPrefsFallback))throw new InvalidOperationException("Secure checkpoint storage unavailable.");
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath));
            // Shared across store instances/processes; failure to acquire is recoverable.
            return new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        static JObject Identity(PendingRunOperation start)
        {
            if(start==null||start.Kind!=PendingRunKind.Start||start.AcceptedResponseJson==null)throw new InvalidOperationException("A retained confirmed START is required.");
            return new JObject{["account"]=start.AccountId,["run"]=(string)JObject.Parse(start.AcceptedResponseJson)["runId"],["preview"]=start.TargetId,["key"]=start.IdempotencyKey,["body"]=start.BodyJson,["response"]=start.AcceptedResponseJson,["version"]=start.Build.AppVersion,["versionCode"]=start.Build.AppVersionCode,["composedAt"]=start.ComposedAt.ToString("O")};
        }
        JObject Load()
        {
            string raw;if(editorRead!=null){raw=editorRead();if(raw==null)return null;}else{var encrypted=file.Load();if(encrypted==null)return null;raw=file.Decrypt(encrypted);}
            if(raw==null||raw.Length>24*1024*1024)throw new FormatException("Checkpoint size invalid.");
            JObject r;
            using(var reader=new JsonTextReader(new StringReader(raw)){DateParseHandling=DateParseHandling.None,MaxDepth=48})
            {r=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});if(reader.Read()||r.Descendants().Any(t=>t.Type==JTokenType.Comment))throw new FormatException("Checkpoint JSON invalid.");}
            if(r.Count!=5||r.Properties().Any(p=>!new[]{"schema","revision","originalStart","state","tick"}.Contains(p.Name))||(string)r["schema"]!="ce03-local-store-v1"||r["revision"]?.Type!=JTokenType.Integer||(long)r["revision"]<1||!(r["originalStart"] is JObject)||r["state"]?.Type!=JTokenType.String||r["tick"]?.Type!=JTokenType.Integer||(long)r["tick"]<0)throw new FormatException("Checkpoint envelope invalid.");
            return r;
        }
        static void Match(JObject stored,JObject identity)
        {if(!JToken.DeepEquals(stored["originalStart"],identity))throw new InvalidOperationException("Original account/run/START differs; preserve checkpoint for recovery.");}
        public long Save(PendingRunOperation start,BasicCombatSimulation simulation,long expectedRevision)
            =>SaveIdentity(Identity(start),start.AccountId,simulation,expectedRevision);
        public long SaveLocalStart(Ce03OriginalStart start,BasicCombatSimulation simulation,long expectedRevision)
        {if(start==null)throw new ArgumentNullException(nameof(start));start.ValidateModel(simulation);return SaveIdentity(start.Copy(),start.Owner,simulation,expectedRevision);}
        public long SaveRailSession(Ce03RailSession session,BasicCombatSimulation simulation,long expectedRevision)
        {if(session==null)throw new ArgumentNullException(nameof(session));session.ValidateModel(simulation);return SaveIdentity(session.Copy(),session.Owner,simulation,expectedRevision);}
        public RestoredCombatCheckpoint RestoreRailSession(Ce03RailSession session)
        {if(session==null)throw new ArgumentNullException(nameof(session));var restored=RestoreIdentity(session.Copy(),session.Owner,session.CreateModel);if(restored!=null)session.ValidateModel(restored.Simulation);return restored;}
        internal long SaveIdentity(JObject identity,string owner,BasicCombatSimulation simulation,long expectedRevision)
        {
            if(simulation==null||simulation.LocalOwnerAccount!=owner||expectedRevision<0)throw new InvalidOperationException("Checkpoint owner or revision invalid.");
            using(Acquire())
            {
                var previous=Load();if(previous!=null)Match(previous,identity);
                var current=previous==null?0:(long)previous["revision"];
                if(current!=expectedRevision||previous!=null&&simulation.Tick<(long)previous["tick"])throw new InvalidOperationException("Stale checkpoint revision or tick.");
                var revision=checked(current+1);var state=simulation.CaptureLocalState();
                if(previous!=null&&!JToken.DeepEquals(JObject.Parse((string)previous["state"])["configurationSha256"],JObject.Parse(state)["configurationSha256"]))throw new InvalidOperationException("Original combat configuration changed.");
                var next=new JObject{["schema"]="ce03-local-store-v1",["revision"]=revision,["originalStart"]=identity.DeepClone(),["state"]=state,["tick"]=simulation.Tick};
                var raw=next.ToString(Formatting.None);if(raw.Length>24*1024*1024)throw new InvalidOperationException("Checkpoint capacity exceeded.");
                if(editorWrite!=null){editorWrite(raw);if(editorRead()!=raw)throw new IOException("LOCAL QA checkpoint readback failed.");}
                else{var encrypted=file.Encrypt(raw);if(encrypted==null||!file.Save(encrypted))throw new IOException("Checkpoint save did not succeed.");}
                return revision;
            }
        }
        public BasicCombatSimulation Restore(PendingRunOperation start,Func<BasicCombatSimulation> freshFactory)
            =>RestoreWithRevision(start,freshFactory)?.Simulation;
        public RestoredCombatCheckpoint RestoreWithRevision(PendingRunOperation start,Func<BasicCombatSimulation> freshFactory)
            =>RestoreIdentity(Identity(start),start.AccountId,freshFactory);
        public RestoredCombatCheckpoint RestoreLocalStartWithRevision(Ce03OriginalStart start,Func<BasicCombatSimulation> freshFactory)
        {if(start==null)throw new ArgumentNullException(nameof(start));var restored=RestoreIdentity(start.Copy(),start.Owner,freshFactory);if(restored!=null)start.ValidateModel(restored.Simulation);return restored;}
        public void RequireDurableLocalStart(Ce03OriginalStart start)
        {
            if(start==null)throw new ArgumentNullException(nameof(start));
            if(RestoreLocalStartWithRevision(start,start.CreateModel)==null)throw new IOException("Original START has no durable checkpoint.");
        }
        internal RestoredCombatCheckpoint RestoreIdentity(JObject identity,string owner,Func<BasicCombatSimulation> freshFactory)
        {
            using(Acquire())
            {
                var stored=Load();if(stored==null)return null;Match(stored,identity);
                var simulation=BasicCombatSimulation.RestoreLocalState((string)stored["state"],freshFactory);
                if(simulation.LocalOwnerAccount!=owner||simulation.Tick!=(long)stored["tick"])throw new InvalidOperationException("Checkpoint state identity differs.");
                return new RestoredCombatCheckpoint(simulation,(long)stored["revision"]);
            }
        }
    }
}
