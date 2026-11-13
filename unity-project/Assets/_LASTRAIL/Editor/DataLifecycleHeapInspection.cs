using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
namespace LastRail.Editor {
 public static class DataLifecycleHeapInspection {
  // Diagnostic only. Preserve the snapshot; expose schemas/counts, never credential-bearing heap bytes in logs.
  public static void Inspect(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_HEAP_INPUT");var output=Environment.GetEnvironmentVariable("LASTRAIL_HEAP_OUTPUT");
   if(!Path.GetFullPath(path).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("D QA paths required");
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot",false)).First(t=>t!=null);
   object snapshot=null;
   try {
    snapshot=type.GetMethod("Load",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{path});
    var result=new JObject{["snapshotType"]=type.FullName};
    foreach(var name in new[]{"managedHeapSections","managedStacks","typeDescriptions","fieldDescriptions","gcHandles","virtualMachineInformation"}){
     var value=type.GetProperty(name).GetValue(snapshot);var row=new JObject{["type"]=value.GetType().FullName};
     var count=value.GetType().GetMethod("GetNumEntries");if(count!=null)row["count"]=Convert.ToInt64(count.Invoke(value,null));
     row["properties"]=new JArray(value.GetType().GetProperties().Select(p=>new JObject{["name"]=p.Name,["type"]=p.PropertyType.FullName}));
     row["fields"]=new JArray(value.GetType().GetFields().Select(p=>new JObject{["name"]=p.Name,["type"]=p.FieldType.FullName}));result[name]=row;
    }
    var vm=type.GetProperty("virtualMachineInformation").GetValue(snapshot);
    result["vm"]=new JObject(vm.GetType().GetProperties().Select(p=>new JProperty(p.Name,JToken.FromObject(p.GetValue(vm)))));
    foreach(var name in new[]{"typeDescriptions","fieldDescriptions","gcHandles"}){
     var entries=type.GetProperty(name).GetValue(snapshot);var data=new JObject();
     foreach(var property in entries.GetType().GetProperties())data[property.Name]=JToken.FromObject(ReadEntries(property.GetValue(entries)));
     result[name+"Data"]=data;
    }
    var sections=type.GetProperty("managedHeapSections").GetValue(snapshot);var addresses=(ulong[])ReadEntries(sections.GetType().GetProperty("startAddress").GetValue(sections));var bytes=(byte[][])ReadEntries(sections.GetType().GetProperty("bytes").GetValue(sections));
    var ranges=new JArray();using(var file=File.Create(Path.ChangeExtension(output,"heap.bin"))){for(int i=0;i<addresses.Length;i++){ranges.Add(new JObject{["start"]=addresses[i],["offset"]=file.Position,["length"]=bytes[i].Length});file.Write(bytes[i],0,bytes[i].Length);}}
    result["heapRanges"]=ranges;File.WriteAllText(output,result.ToString());
   }finally{(snapshot as IDisposable)?.Dispose();}
  }
  static Array ReadEntries(object entries){var count=Convert.ToUInt32(entries.GetType().GetMethod("GetNumEntries").Invoke(entries,null));var method=entries.GetType().GetMethod("GetEntries");var element=method.GetParameters()[2].ParameterType.GetElementType().GetElementType();var args=new object[]{0u,count,Array.CreateInstance(element,count)};method.Invoke(entries,args);return (Array)args[2];}
 }
}
