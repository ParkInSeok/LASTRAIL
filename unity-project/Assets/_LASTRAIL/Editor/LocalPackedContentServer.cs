using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
namespace LastRail.Editor {
 // Editor owns this loopback host; only the selected build's bundle files are served.
 [InitializeOnLoad] public static class LocalPackedContentServer {
  static HttpListener listener;static string directory;
  static LocalPackedContentServer(){AssemblyReloadEvents.beforeAssemblyReload+=Stop;EditorApplication.quitting+=Stop;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode)Stop();};}
  public static void Start(string build){
   var root=Path.Combine(build,"remote");if(listener!=null){if(directory!=root)throw new InvalidOperationException("Restart Play mode to select another build.");return;}
   if(!Directory.Exists(root))throw new DirectoryNotFoundException("Local bundles missing: "+root);
   var next=new HttpListener();next.Prefixes.Add("http://127.0.0.1:18284/");next.Start();directory=root;listener=next;_ = Task.Run(()=>Serve(next,root));
  }
  public static void Stop(){var old=listener;listener=null;directory=null;old?.Close();}
  static async Task Serve(HttpListener host,string root){
   while(host.IsListening){HttpListenerContext context;try{context=await host.GetContextAsync().ConfigureAwait(false);}catch(HttpListenerException){break;}catch(ObjectDisposedException){break;}
    try{var path=context.Request.Url.AbsolutePath;const string prefix="/LOCAL_TEST/Windows/";var name=path.StartsWith(prefix,StringComparison.Ordinal)?path.Substring(prefix.Length):"";
     if(context.Request.HttpMethod!="GET"||name!=Path.GetFileName(name)||!name.EndsWith(".bundle",StringComparison.Ordinal)||name.Contains("%")||!File.Exists(Path.Combine(root,name))){context.Response.StatusCode=404;UnityEngine.Debug.LogWarning("LOCAL_PACKED_404 "+path);continue;}
     using(var input=File.OpenRead(Path.Combine(root,name))){context.Response.ContentLength64=input.Length;context.Response.ContentType="application/octet-stream";context.Response.KeepAlive=false;input.CopyTo(context.Response.OutputStream);}
    }catch(IOException error){UnityEngine.Debug.LogWarning("LOCAL_PACKED_IO "+error.Message);context.Response.Abort();}catch(HttpListenerException error){UnityEngine.Debug.LogWarning("LOCAL_PACKED_HTTP "+error.Message);context.Response.Abort();}finally{context.Response.Close();}
   }
  }
 }
}
