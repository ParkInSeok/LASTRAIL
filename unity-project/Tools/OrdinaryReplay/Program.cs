using System;
using System.Diagnostics;
using System.IO;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;

internal static class Program {
 static int Main(string[] args){
  try{
   if(args.Length!=3&&(args.Length!=4||args[3]!="--input-projection"))throw new ArgumentException("Two fixture files and one fresh output file required; optional --input-projection selects input QA.");
   foreach(var path in new[]{args[0],args[1]})if(new FileInfo(path).Length>2097152)throw new ArgumentException("Fixture exceeds QA limit.");
   string output=Path.GetFullPath(args[2]);
   if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||File.Exists(output))throw new ArgumentException("Fresh D QA output required.");
   using(var process=Process.GetCurrentProcess()){
    var clock=Stopwatch.StartNew();var cpu=process.TotalProcessorTime;
    var result=args.Length==4?OrdinarySharedCoreProbe.RunInputs(File.ReadAllText(args[0])):OrdinarySharedCoreProbe.Run(File.ReadAllText(args[0]),File.ReadAllText(args[1]));
    clock.Stop();process.Refresh();
    var report=new JObject{["result"]=result,["measurements"]=new JObject{["elapsedMs"]=clock.Elapsed.TotalMilliseconds,["cpuMs"]=(process.TotalProcessorTime-cpu).TotalMilliseconds,["peakWorkingSetBytes"]=process.PeakWorkingSet64>0?(JToken)new JValue(process.PeakWorkingSet64):JValue.CreateNull(),["managedBytes"]=GC.GetTotalMemory(false),["processBits"]=IntPtr.Size*8,["forcedGc"]=false,["scope"]="ONE_COLD_QA_CALL_NOT_PRODUCTION_CAPACITY"}};
    File.WriteAllText(output,report.ToString());Console.WriteLine("SHARED_CORE_QA_PASSED");return 0;
   }
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
