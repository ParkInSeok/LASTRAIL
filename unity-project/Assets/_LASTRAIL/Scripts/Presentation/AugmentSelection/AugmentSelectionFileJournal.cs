using System;
using System.IO;
using LastRail.Auth;
using LastRail.Submissions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.AugmentSelection
{
    // One original grant per file. Host owns the account/run/grant path and lifetime.
    // A pending intent is recovery evidence, never an applied effect or HP ledger.
    public sealed class AugmentSelectionFileJournal:IAugmentSelectionJournal
    {
        readonly AugmentSelectionScope scope;
        readonly AndroidEncryptedReceiptFilePort file;
        readonly string lockPath;
        readonly Func<string> editorRead;
        readonly Action<string> editorWrite;
        public AugmentSelectionFileJournal(AugmentSelectionScope scope,IAndroidKeystoreNativeStoragePort crypto,string absolutePath)
        {this.scope=scope??throw new ArgumentNullException(nameof(scope));file=new AndroidEncryptedReceiptFilePort(crypto,absolutePath,"lastrail_augment_selection_v1:");lockPath=Path.GetFullPath(absolutePath)+".lock";}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
        public AugmentSelectionFileJournal(AugmentSelectionScope scope,string absolutePath,Func<string> read,Action<string> write)
        {this.scope=scope??throw new ArgumentNullException(nameof(scope));if(!Path.IsPathRooted(absolutePath))throw new ArgumentException("Absolute path required");lockPath=Path.GetFullPath(absolutePath)+".lock";editorRead=read??throw new ArgumentNullException(nameof(read));editorWrite=write??throw new ArgumentNullException(nameof(write));}
#endif
        IDisposable Acquire(string key)
        {
            if(key!=scope.StorageKey)throw new ArgumentException("Selection journal scope changed");
            if(editorRead==null&&(!file.IsAndroidRuntime||!file.IsKeystoreAvailable||file.UsedPlayerPrefsFallback))throw new InvalidOperationException("Secure selection storage unavailable");
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath));return new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        string Read()
        {
            string raw;if(editorRead!=null)raw=editorRead();else{var payload=file.Load();raw=payload==null?null:file.Decrypt(payload);}
            if(raw==null)return null;if(raw.Length>4*1024*1024)throw new FormatException("Selection journal size");
            var root=SelectionJson.Object(raw);SelectionJson.Keys(root,"version","scope","pending");
            if((string)root["version"]!="unity.augment-selection-journal.v1"||(string)root["scope"]!=scope.StorageKey)throw new FormatException("Selection journal binding");
            if(root["pending"]?.Type==JTokenType.Null)return null;
            if(root["pending"]?.Type!=JTokenType.String)throw new FormatException("Selection journal command");
            string command=(string)root["pending"];AugmentSelectionCommand.Restore(scope,command);return command;
        }
        void Write(string command)
        {
            string raw=new JObject{["version"]="unity.augment-selection-journal.v1",["scope"]=scope.StorageKey,["pending"]=command==null?JValue.CreateNull():new JValue(command)}.ToString(Formatting.None);
            if(raw.Length>4*1024*1024)throw new FormatException("Selection journal size");
            if(editorWrite!=null)editorWrite(raw);else if(!file.Save(file.Encrypt(raw)))throw new IOException("Selection journal save failed");
            if(Read()!=command)throw new IOException("Selection journal readback failed");
        }
        public string Load(string scopeKey){using(Acquire(scopeKey))return Read();}
        public void SaveNew(string scopeKey,string serializedCommand)
        {
            AugmentSelectionCommand.Restore(scope,serializedCommand);
            using(Acquire(scopeKey)){var prior=Read();if(prior==serializedCommand)return;if(prior!=null)throw new InvalidOperationException("Selection pending original conflict");Write(serializedCommand);}
        }
        public void Complete(string scopeKey,string expectedSerializedCommand)
        {
            AugmentSelectionCommand.Restore(scope,expectedSerializedCommand);
            using(Acquire(scopeKey)){var prior=Read();if(prior==null)return;if(prior!=expectedSerializedCommand)throw new InvalidOperationException("Selection completion CAS conflict");Write(null);}
        }
    }
}
