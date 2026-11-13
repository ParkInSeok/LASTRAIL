#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
namespace LastRail.Diagnostics {
 // Keeps long local reviews bounded on disk; only actual captured frames enter the encoder.
 internal sealed class LocalReviewVideoRecorder:IDisposable {
  readonly Process process;readonly Task<string> errors;bool complete;
  internal LocalReviewVideoRecorder(string output){
   if(File.Exists(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Fresh D QA video required.");
   process=new Process{StartInfo=new ProcessStartInfo{
    FileName="D:/Project/LASTRAIL/Tools/FrameStudio/vendor/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",
    Arguments="-hide_banner -loglevel error -f image2pipe -vcodec mjpeg -framerate 10 -i pipe:0 -an -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p -movflags +faststart \""+output+"\"",
    UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true,
    WorkingDirectory="D:/Project/LASTRAIL"}};
   if(!process.Start())throw new InvalidOperationException("Local video encoder did not start.");errors=process.StandardError.ReadToEndAsync();
  }
  internal void Write(byte[] jpeg){if(complete||process.HasExited)throw new InvalidOperationException("Local video encoder stopped.");process.StandardInput.BaseStream.Write(jpeg,0,jpeg.Length);}
  internal void Finish(){
   if(complete)return;process.StandardInput.Close();
   if(!process.WaitForExit(30000))throw new TimeoutException("Local video encoder finalization timed out.");
   string error=errors.GetAwaiter().GetResult();if(process.ExitCode!=0)throw new InvalidOperationException("Local video encoding failed: "+error);complete=true;
  }
  public void Dispose(){if(!process.HasExited){process.Kill();process.WaitForExit(5000);}process.Dispose();}
 }
}
#endif
