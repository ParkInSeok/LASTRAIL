using System;
using System.IO;
namespace LastRail.Diagnostics {
 // Explicit local development output only. Normal device/deployed storage retains its original path.
 public static class LocalDevelopmentDiagnosticsPath {
  public static string Resolve(string persistentDirectory,string explicitQaDirectory){
   if(explicitQaDirectory==null)return Path.Combine(persistentDirectory,"diagnostics","safe-client-errors-v1.json");
   if(string.IsNullOrWhiteSpace(explicitQaDirectory)||!Path.IsPathRooted(explicitQaDirectory))throw new ArgumentException("Absolute D project QA diagnostics directory required.");
   string full=Path.GetFullPath(explicitQaDirectory).Replace('\\','/').TrimEnd('/');
   if(!full.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Local development diagnostics must remain inside canonical D project QA.");
   return Path.Combine(full,"safe-client-errors-v1.json");
  }
 }
}