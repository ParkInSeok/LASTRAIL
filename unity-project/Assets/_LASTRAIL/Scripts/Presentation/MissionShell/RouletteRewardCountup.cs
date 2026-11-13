using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    // Display-only snapshots. Interpolated values never leave the view or enter a reward journal.
    public sealed class RouletteRewardCountup
    {
        sealed class Line { public long? Start, Final; }
        JArray preview;
        readonly List<Line> lines=new List<Line>();
        public int Count=>lines.Count;
        public void CapturePreview(JArray rows){preview=rows==null?null:(JArray)rows.DeepClone();lines.Clear();}
        public void Clear(){preview=null;lines.Clear();}
        static string Key(JToken row)
        {
            var id=row?["id"]?.Type==JTokenType.String?(string)row["id"]:null;
            var kind=row?["kind"]?.Type==JTokenType.String?(string)row["kind"]:null;
            return string.IsNullOrEmpty(id)||string.IsNullOrEmpty(kind)?null:kind+"\u001f"+id;
        }
        static long? Quantity(JToken row)
        {
            return row?["quantity"]?.Type==JTokenType.Integer&&long.TryParse(row["quantity"].ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out long value)&&value>=0?value:(long?)null;
        }
        public void Begin(JArray receiptRows)
        {
            lines.Clear();if(receiptRows==null)return;
            var bases=(preview??new JArray()).Where(r=>Key(r)!=null).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.ToArray());
            var counts=receiptRows.Where(r=>Key(r)!=null).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.Count());
            foreach(var row in receiptRows)
            {
                var line=new Line{Final=Quantity(row)};string key=Key(row);
                if(key!=null&&counts[key]==1&&bases.TryGetValue(key,out var candidates)&&candidates.Length==1)
                {
                    var basis=candidates[0];string source=(string)row["source"],baseSource=(string)basis["source"];
                    // Same currency in an excluded source is not an eligible-base match.
                    if(source==null||baseSource==null||source==baseSource)line.Start=Quantity(basis);
                    if(line.Start.HasValue&&line.Final.HasValue&&line.Start.Value>line.Final.Value)line.Start=null;
                }
                lines.Add(line);
            }
        }
        public string Text(int index,double progress)
        {
            var row=lines[index];if(!row.Final.HasValue)return "—";
            if(progress>=1)return row.Final.Value.ToString("N0",CultureInfo.InvariantCulture);
            if(!row.Start.HasValue)return "—";
            decimal t=(decimal)Math.Max(0,Math.Min(1,progress));
            long value=(long)decimal.Truncate(row.Start.Value+((decimal)row.Final.Value-row.Start.Value)*t);
            return value.ToString("N0",CultureInfo.InvariantCulture);
        }
    }
}
