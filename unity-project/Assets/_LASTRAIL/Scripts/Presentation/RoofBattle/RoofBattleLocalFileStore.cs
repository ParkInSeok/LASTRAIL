using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.RoofBattle {
 // Atomic replacement and compare-and-swap for this explicit D LOCAL context. No global DB or operating persistence.
 public sealed class RoofBattleLocalFileStore {
  readonly string directory,checkpoint,lockPath,context,expectedVersion;
  public RoofBattleLocalFileStore(string directory,string context):this(directory,context,RoofBattleLocalOwner.Version){}
  public RoofBattleLocalFileStore(string directory,string context,string expectedVersion){
   if(string.IsNullOrWhiteSpace(expectedVersion)||expectedVersion.Length>128)throw new ArgumentException("Explicit checkpoint contract version required.");this.expectedVersion=expectedVersion;
   if(string.IsNullOrWhiteSpace(directory)||string.IsNullOrWhiteSpace(context))throw new ArgumentException("Explicit D LOCAL store and context required.");
   this.directory=Path.GetFullPath(directory);if(!this.directory.StartsWith(@"D:\Project\LASTRAIL\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Roof LOCAL output must remain inside D:/Project/LASTRAIL.");
   this.context=context;using var sha=SHA256.Create();string key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(context))).Replace("-","").ToLowerInvariant();
   Directory.CreateDirectory(this.directory);checkpoint=Path.Combine(this.directory,key+".json");lockPath=Path.Combine(this.directory,key+".lock");
  }
  JObject Read(){if(!File.Exists(checkpoint))return null;if(new FileInfo(checkpoint).Length>32L*1024*1024)throw new IOException("LOCAL checkpoint exceeds storage format bound.");return JObject.Parse(File.ReadAllText(checkpoint,Encoding.UTF8));}
  public JObject Load(){using var lease=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);return Read();}
  public void Save(JObject candidate,long expectedRevision){
   if(candidate==null||(string)candidate["version"]!=expectedVersion||(string)candidate["contextId"]!=context||candidate["revision"]?.Type!=JTokenType.Integer||(long)candidate["revision"]!=checked(expectedRevision+1))throw new ArgumentException("LOCAL checkpoint context/revision mismatch.");
   using var lease=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);var prior=Read();
   if(((long?)prior?["revision"]??0)!=expectedRevision)throw new InvalidOperationException("LOCAL checkpoint revision conflict; restore original saved context.");
   var bytes=new UTF8Encoding(false,true).GetBytes(candidate.ToString(Formatting.None));if(bytes.Length>32L*1024*1024)throw new IOException("LOCAL checkpoint exceeds storage format bound.");
   string temporary=Path.Combine(directory,Path.GetFileName(checkpoint)+"."+Guid.NewGuid().ToString("N")+".tmp");
   try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(checkpoint))File.Replace(temporary,checkpoint,null);else File.Move(temporary,checkpoint);}
   finally{if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
}
