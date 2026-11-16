using System;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace LastRail.Qa {
 // Candidate only. Not called by observed-v1, production damage or event persistence.
 public static class MapOverlapV2 {
  public readonly struct Result {
   public readonly bool DuringStep,AtStepEnd,NextConsumed;
   public readonly string Reason;
   public bool Candidate=>DuringStep||AtStepEnd;
   internal Result(bool during,bool endpoint,bool consumed,string reason){DuringStep=during;AtStepEnd=endpoint;NextConsumed=consumed||during||endpoint;Reason=reason;}
   public JObject Json()=>new JObject{["version"]="rockfall-overlap-candidate-v2",["candidate"]=Candidate,["duringStep"]=DuringStep,["atStepEnd"]=AtStepEnd,["nextConsumed"]=NextConsumed,["reason"]=Reason};
  }
  readonly struct Fraction {
   readonly BigInteger numerator,denominator;
   public Fraction(BigInteger n,BigInteger d){if(d<=0)throw new ArgumentOutOfRangeException(nameof(d));numerator=n;denominator=d;}
   public static bool Less(Fraction a,Fraction b)=>a.numerator*b.denominator<b.numerator*a.denominator;
   public static Fraction Max(Fraction a,Fraction b)=>Less(a,b)?b:a;
   public static Fraction Min(Fraction a,Fraction b)=>Less(a,b)?a:b;
  }
  public static Result Evaluate(BigInteger n,BigInteger h0,BigInteger h1,BigInteger length,BigInteger a,BigInteger b,BigInteger start,BigInteger end,bool routeMatches=true,bool stepAdvanced=true,bool eventConsumed=false){
   if(n<1||h0<0||h1<h0||length<=0||a<0||b<=a||start<0||end<=start)throw new ArgumentOutOfRangeException("Invalid monotone integer overlap domain");
   if(!routeMatches)return new Result(false,false,eventConsumed,"OTHER_ROUTE");
   if(!stepAdvanced)return new Result(false,false,eventConsumed,"NO_STEP");
   if(eventConsumed)return new Result(false,false,true,"ALREADY_CONSUMED");
   BigInteger delta=h1-h0;
   var lower=Fraction.Max(new Fraction(0,1),new Fraction(start-(n-1),1));
   var upper=Fraction.Min(new Fraction(1,1),new Fraction(end-(n-1),1));
   bool during;
   if(delta.IsZero)during=h0>a&&h0-length<b&&Fraction.Less(lower,upper);
   else{lower=Fraction.Max(lower,new Fraction(a-h0,delta));upper=Fraction.Min(upper,new Fraction(b+length-h0,delta));during=Fraction.Less(lower,upper);}
   bool endpoint=start<=n&&n<end&&h1>a&&h1-length<b;
   return new Result(during,endpoint,false,during?"ACTIVE_TIME_OVERLAP":endpoint?"ACTIVE_ENDPOINT_OCCUPANCY":"NO_OVERLAP");
  }
  static Result FromJson(JObject input,BigInteger scale,BigInteger timeOffset){
   BigInteger Read(string key){var v=input[key];if(v?.Type!=JTokenType.Integer)throw new FormatException("Integer fixture input required");return BigInteger.Parse(v.ToString(),System.Globalization.CultureInfo.InvariantCulture);}
   bool Flag(string key){var v=input[key];if(v?.Type!=JTokenType.Boolean)throw new FormatException("Boolean guard required");return (bool)v;}
   return Evaluate(Read("n")+timeOffset,Read("h0")*scale,Read("h1")*scale,Read("length")*scale,Read("A")*scale,Read("B")*scale,Read("S")+timeOffset,Read("E")+timeOffset,Flag("routeMatches"),Flag("stepAdvanced"),Flag("eventConsumed"));
  }
  public static JObject Verify(string output=null){
   string root=MapGrayboxFixtureGeometry.Root;
   var pins=new[]{("Server/overlap-v2.cjs","90305b567c6e9334d2689b54979b981f6cc9c5481f1e784862d18c74f30747a3"),("Server/overlap-v2-expectations.json","ccc26b7d16309f6da53052db59da2fc4bc3ea9d945e58c37dec597aee9aa6c08"),("../../docs/design/TRAIN-ROCKFALL-TIME-OVERLAP-20260914.md","579c2aa24abfdbc5d173a7bb53a5277580a0bf4d88ead5373a87d2cf97e19c94")};
   foreach(var pin in pins)if(MapGrayboxFixtureGeometry.Hash(Path.GetFullPath(Path.Combine(root,pin.Item1)))!=pin.Item2)throw new InvalidOperationException("v2 pin changed: "+pin.Item1);
   var fixture=JObject.Parse(File.ReadAllText(Path.Combine(root,"Server/overlap-v2-expectations.json")));var results=new JArray();var magnitude=BigInteger.Pow(10,30);
   foreach(var c in fixture["cases"]){var actual=FromJson((JObject)c["input"],1,0).Json();foreach(var field in ((JObject)c["expected"]).Properties())MapGrayboxFixtureReplay.Equal(actual[field.Name],field.Value,(string)c["id"]+"."+field.Name);MapGrayboxFixtureReplay.Equal(actual,c["actual"],(string)c["id"]+" Server");MapGrayboxFixtureReplay.Equal(FromJson((JObject)c["input"],magnitude,magnitude).Json(),actual,(string)c["id"]+" 1e30");results.Add(new JObject{["id"]=c["id"],["input"]=c["input"].DeepClone(),["result"]=actual});}
   var report=new JObject{["version"]="CSharp-isolated-overlap-v2",["scope"]="PURE_CANDIDATE_ONLY",["result"]="PASS",["cases"]=results.Count,["bigIntegerScaledAndTimeTranslatedCases"]=results.Count,["results"]=results,["v1Changed"]=false,["productionReady"]=false,["persistenceGuarantee"]="None: nextConsumed is a caller proposal, not an event commit"};
   if(output!=null){output=Path.GetFullPath(output);if(!output.StartsWith(Path.Combine(root,"Unity")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh Unity QA directory required");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"verification.json"),report.ToString());}return report;
  }
  public static void Export(){try{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-kyleOutput");if(i<0||i+1>=args.Length)throw new ArgumentException("Output required");Verify(args[i+1]);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
