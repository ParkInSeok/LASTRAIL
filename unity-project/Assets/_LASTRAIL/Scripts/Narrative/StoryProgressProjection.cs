using System;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Narrative
{
    // A validated, account-independent display definition. No network or command ownership.
    internal sealed class StoryProgressProjection
    {
        public const string ObservationVersion="story.progress-observation.v1.20261022";
        readonly string[] order,operations;
        readonly string initial,narrativeVersion;
        readonly JObject metadata;
        readonly JObject definition;
        internal StoryProgressProjection(JObject value)
        {
            if(Text(value,"version")!="story.progress-projection.v1.20261022"||
                !JToken.DeepEquals(value["coverage"],new JArray("STORY_PROGRESS","OPERATION_UNLOCKS")))throw new FormatException("Progress projection version/coverage");
            narrativeVersion=Text(value,"narrativeVersion");order=Ids(value["storyProgressOrder"]);operations=Ids(value["operationIds"]);initial=Text(value,"initialOperationId");
            if(order.Length<2||order[0]!="none"||!order.Skip(1).SequenceEqual(operations)||initial!=operations[0])throw new FormatException("Progress projection order");
            metadata=value["metadata"] as JObject??throw new FormatException("Progress metadata");
            foreach(var key in new[]{"narrativeVersion","contentStatus","localizationStatus","fixtureStatus","missingContentStatus"})Text(metadata,key);
            if(Text(metadata,"narrativeVersion")!=narrativeVersion)throw new FormatException("Progress metadata version");
            metadata=(JObject)metadata.DeepClone();definition=(JObject)value.DeepClone();
        }
        internal bool SameScope(StoryProgressProjection other)=>narrativeVersion==other.narrativeVersion;
        internal bool SameDefinition(StoryProgressProjection other)=>JToken.DeepEquals(definition,other.definition);
        internal JObject Project(JObject progress)
        {
            RunCompleteClaimReceipt.ValidateProgress(progress);
            var raw=(JObject)progress["state"];var story=raw["story"] as JObject;
            string highest="none",clearedHighest=null,lastRun=null;JToken updated=JValue.CreateNull();string[] cleared=Array.Empty<string>(),flags=Array.Empty<string>();
            if(story!=null)
            {
                if(Text(story,"actId")!="act_01_wasteland_railway"||NullableText(story,"narrativeVersion") is string version&&version!=narrativeVersion)throw new FormatException("Progress projection scope");
                highest=Text(story,"highestStoryProgressId");clearedHighest=Text(story,"highestClearedOperationId");
                if(!order.Contains(highest)||!operations.Contains(clearedHighest))throw new FormatException("Unknown progress operation");
                cleared=SortKnown(Ids(JArray.Parse(Text(story,"clearedOperationsJson",131072)),false));flags=Ids(JArray.Parse(Text(story,"storyFlagsJson",131072)),false);
                lastRun=NullableText(story,"lastProgressRunId");
                if(story["updatedAt"]?.Type!=JTokenType.Integer||(long)story["updatedAt"]<0)throw new FormatException("Progress timestamp");
                updated=DateTimeOffset.FromUnixTimeMilliseconds((long)story["updatedAt"]).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",System.Globalization.CultureInfo.InvariantCulture);
            }
            var unlocks=((JArray)raw["unlocks"]).Select(x=>(string)x["unlockId"]).Where(x=>x.StartsWith("operation:",StringComparison.Ordinal)).Select(x=>x.Substring(10)).Prepend(initial);
            return new JObject{["accepted"]=true,["actId"]="act_01_wasteland_railway",["storyProgress"]=new JObject{["highestStoryProgressId"]=highest,["highestClearedOperationId"]=clearedHighest==null?JValue.CreateNull():new JValue(clearedHighest),["order"]=Array.IndexOf(order,highest)},
                ["clearedOperationIds"]=new JArray(cleared),["unlockedOperations"]=new JArray(SortKnown(unlocks.ToArray())),["narrativeVersion"]=narrativeVersion,["narrative"]=metadata.DeepClone(),
                ["storyFlags"]=new JArray(flags),["narrativeFlags"]=new JArray(flags),["lastProgressRunId"]=lastRun==null?JValue.CreateNull():new JValue(lastRun),["updatedAt"]=updated};
        }
        internal void ValidateDisplay(JObject response,JObject projected)
        {
            foreach(var key in new[]{"actId","storyProgress","clearedOperationIds","unlockedOperations","narrativeVersion","narrative","storyFlags","narrativeFlags","lastProgressRunId","updatedAt"})
                if(!JToken.DeepEquals(response[key],projected[key]))throw new FormatException("Progress display mismatch: "+key);
        }
        string[] SortKnown(string[] ids){if(ids.Any(id=>!operations.Contains(id)))throw new FormatException("Unknown operation unlock/clear");return operations.Where(ids.Contains).ToArray();}
        static string[] Ids(JToken value,bool unique=true){if(!(value is JArray a)||a.Count>128||a.Any(x=>x.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)x)||((string)x).Length>200))throw new FormatException("Progress string array");var ids=a.Values<string>().ToArray();if(unique&&ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)throw new FormatException("Duplicate projection ID");return ids;}
        static string NullableText(JObject row,string key){if(row[key]?.Type==JTokenType.Null)return null;return Text(row,key);}
        static string Text(JObject row,string key,int limit=200){if(row[key]?.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)row[key])||((string)row[key]).Length>limit)throw new FormatException("Progress text: "+key);return (string)row[key];}
    }
}
