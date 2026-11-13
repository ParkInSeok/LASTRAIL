#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace LastRail.Diagnostics {
 // Captures this Player's listener mix, never the microphone or other applications.
 public sealed class ReinaReviewAudioCapture:MonoBehaviour {
  readonly object gate=new object();float[] samples;int count,channels,rate;bool recording;double firstDsp=-1,requestedDsp,requestedTime;
  public void Begin(){lock(gate){rate=AudioSettings.outputSampleRate;samples=new float[checked(rate*2*130)];requestedDsp=AudioSettings.dspTime;requestedTime=Time.realtimeSinceStartupAsDouble;recording=true;}}
  void OnAudioFilterRead(float[] data,int channelCount){lock(gate){
   if(!recording)return;
   if(channels==0){channels=channelCount;firstDsp=AudioSettings.dspTime;}
   if(channels!=channelCount||count+data.Length>samples.Length){recording=false;return;}
   Array.Copy(data,0,samples,count,data.Length);count+=data.Length;
  }}
  public JObject Save(string path){
   lock(gate){recording=false;
    if(count==0||channels==0)throw new InvalidOperationException("No native listener audio captured.");
    using var writer=new BinaryWriter(File.Create(path));int bytes=checked(count*2);
    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
    writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);double peak=0;
    for(int i=0;i<count;i++){peak=Math.Max(peak,Math.Abs(samples[i]));writer.Write((short)(Math.Max(-1,Math.Min(1,samples[i]))*32767));}
    samples=null;return new JObject{["sampleRate"]=rate,["channels"]=channels,["samples"]=count,["seconds"]=count/(double)(rate*channels),["firstDsp"]=firstDsp,["startRealtime"]=requestedTime+firstDsp-requestedDsp,["peak"]=peak,["source"]="NATIVE_AUDIO_LISTENER_MIX"};
   }
  }
  void OnDestroy(){lock(gate){recording=false;samples=null;}}
 }
}
#endif
