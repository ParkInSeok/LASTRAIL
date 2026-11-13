using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Native float32 values are written with Newtonsoft's round-trip float formatter.
 // Parse that SAME wire representation, never widen float32 directly to double or
 // round to millimetres. Restoring to Unity float retains the original float bits.
 internal static class MiraJudgementWire {
  internal static JObject Capture(JObject value){
   if(value==null)throw new ArgumentNullException(nameof(value));
   using var text=new StringReader(value.ToString(Formatting.None));
   using var reader=new JsonTextReader(text){DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Double};
   return JObject.Load(reader);
  }
 }
}