using System;
using System.Collections.Generic;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.MissionShell
{
    // Display identity only. Never a source of ownership, rarity, pickup or combat data.
    public static class OperatorVisualCatalog
    {
        static Dictionary<string,Tuple<string,string>> entries;
        static readonly Dictionary<string,string> koreanLabels=new Dictionary<string,string>();
        static void Load(){if(entries!=null)return;entries=new Dictionary<string,Tuple<string,string>>();var asset=LastRail.Resources.PreparedContent.Load<TextAsset>("OperatorIdentity/catalog");if(asset==null)return;
            try{foreach(JObject row in (JArray)GameApiJson.ReadObject(asset.text)["operators"]){var id=GameApiJson.String(row,"operatorId");entries.Add(id,Tuple.Create(GameApiJson.String(row,"displayLabel"),GameApiJson.String(row,"portraitResource")));var ko=(string)row["displayLabelKo"];if(!string.IsNullOrEmpty(ko))koreanLabels[id]=ko;}}catch{entries.Clear();koreanLabels.Clear();}}
        public static string KoreanLabel(string id){if(id==null)return "미배치";Load();return koreanLabels.TryGetValue(id,out var label)?label:Label(id);}
        public static IReadOnlyList<string> Ids {get{Load();return new List<string>(entries.Keys).AsReadOnly();}}
        public static string Label(string id){if(id==null)return "미배치";Load();return entries.TryGetValue(id,out var item)?item.Item1:id;}
        public static Texture2D Portrait(string id){if(id==null)return null;Load();return entries.TryGetValue(id,out var item)?LastRail.Resources.PreparedContent.Load<Texture2D>(item.Item2):null;}
        public static Rect Crop(Texture2D texture,float width,float height)
        {if(texture==null)return new Rect(0,0,1,1);float ratio=(width/height)/((float)texture.width/texture.height);return ratio<1?new Rect((1-ratio)*.5f,0,ratio,1):new Rect(0,(1-1/ratio)*.5f,1,1/ratio);}
    }
}
