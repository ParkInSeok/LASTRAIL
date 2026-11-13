using System;
using System.IO;
using LastRail.Auth;
using LastRail.Submissions;

namespace LastRail.Run
{
    // One immutable acquisition per original account/run. Never an HP/effect ledger.
    // Host owns the path and lifetime; another account/run must use another store.
    public sealed class CombatO22ApplicationStore
    {
        readonly AndroidEncryptedReceiptFilePort file;
        readonly string lockPath;
        readonly Func<string> editorRead;
        readonly Action<string> editorWrite;
        public CombatO22ApplicationStore(IAndroidKeystoreNativeStoragePort crypto,string absolutePath)
        {file=new AndroidEncryptedReceiptFilePort(crypto,absolutePath,"lastrail_o22_application_original_v1:");lockPath=Path.GetFullPath(absolutePath)+".lock";}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
        // Explicit injected LOCAL QA port, not Android encryption evidence.
        public CombatO22ApplicationStore(string absolutePath,Func<string> read,Action<string> write)
        {if(!Path.IsPathRooted(absolutePath))throw new ArgumentException("Absolute path required");lockPath=Path.GetFullPath(absolutePath)+".lock";editorRead=read??throw new ArgumentNullException(nameof(read));editorWrite=write??throw new ArgumentNullException(nameof(write));}
#endif
        IDisposable Acquire()
        {
            if(editorRead==null&&(!file.IsAndroidRuntime||!file.IsKeystoreAvailable||file.UsedPlayerPrefsFallback))throw new InvalidOperationException("Secure original storage unavailable");
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath));return new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        string Read()
        {
            if(editorRead!=null)return editorRead();
            var encrypted=file.Load();return encrypted==null?null:file.Decrypt(encrypted);
        }
        public void Commit(CombatO22ApplicationOriginal original)
        {
            if(original==null)throw new ArgumentNullException(nameof(original));
            using(Acquire()){
                var prior=Read();
                if(prior!=null){var saved=CombatO22ApplicationOriginal.Read(prior,original.AccountId,original.RunId,original.GrantEventId,original.Environment,original.StartSnapshotHash);
                    if(saved.Capture()!=original.Capture())throw new InvalidOperationException("Original acquisition changed; preserve for recovery");return;}
                string raw=original.Capture();
                if(editorWrite!=null){editorWrite(raw);if(editorRead()!=raw)throw new IOException("Original readback failed");}
                else {var encrypted=file.Encrypt(raw);if(encrypted==null||!file.Save(encrypted))throw new IOException("Original save failed");}
            }
        }
        public CombatO22ApplicationOriginal Load(string account,string run,string grant,string environment,string startHash)
        {using(Acquire()){string raw=Read();return raw==null?null:CombatO22ApplicationOriginal.Read(raw,account,run,grant,environment,startHash);}}
        public CombatO22ApplicationOriginal LoadForRun(string account,string run,string environment,string startHash)
        {using(Acquire()){string raw=Read();return raw==null?null:CombatO22ApplicationOriginal.Read(raw,account,run,(string)Newtonsoft.Json.Linq.JObject.Parse(raw)["grantEventId"],environment,startHash);}}
    }
}
