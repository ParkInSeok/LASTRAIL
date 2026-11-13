#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Audio {
 // Bounded DEV evidence from Unity's actual audio callback, not synthesized test output.
 public sealed class LocalAudioOutputCapture:MonoBehaviour {
  readonly object sync=new object();float[] pcm;int written,channels,rate,maxFrames;long samples,clipped;double peak;bool recording;
  public void Begin(int seconds){
   lock(sync){rate=AudioSettings.outputSampleRate;maxFrames=checked(rate*seconds);pcm=seconds>0?new float[checked(maxFrames*8)]:null;written=0;samples=clipped=0;peak=0;channels=0;recording=true;}
  }
  void OnAudioFilterRead(float[] data,int count){
   lock(sync){if(!recording)return;channels=count;samples+=data.Length;
    foreach(float v in data){double a=Math.Abs(v);if(a>peak)peak=a;if(a>1)clipped++;}
    if(pcm!=null){int n=Math.Max(0,Math.Min(data.Length,Math.Min(pcm.Length,maxFrames*count)-written));Array.Copy(data,0,pcm,written,n);written+=n;}
   }
  }
  public JObject Finish(string wav=null){
   lock(sync){recording=false;
    bool fileCreated=false;
    if(wav!=null&&channels>0&&written>0){
     using(var w=new BinaryWriter(new FileStream(wav,FileMode.CreateNew,FileAccess.Write))){
      w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+written*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
      w.Write(16);w.Write((short)1);w.Write((short)channels);w.Write(rate);w.Write(rate*channels*2);w.Write((short)(channels*2));w.Write((short)16);
      w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(written*2);
      for(int i=0;i<written;i++)w.Write((short)Math.Round(Math.Max(-1,Math.Min(1,pcm[i]))*32767));
     }
     fileCreated=true;
    }
    var result=new JObject{["samples"]=samples,["peak"]=peak,["clippedSamples"]=clipped,["channels"]=channels,["sampleRate"]=rate,["writtenSamples"]=written,
     ["fileCreated"]=fileCreated,["path"]=fileCreated?wav:null,["humanListening"]=false};
    pcm=null;return result;
   }
  }
  void OnDisable(){lock(sync){recording=false;pcm=null;}}
 }
}
#endif
