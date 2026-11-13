using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
namespace LastRail.Presentation.AugmentSelection
{
    public enum AugmentRecordCoverage { None, Complete, Partial }
    public sealed class AugmentResultMetric
    {
        public string Label {get;}
        public long Value {get;}
        public AugmentResultMetric(string label,long value){if(string.IsNullOrWhiteSpace(label)||value<0)throw new ArgumentException();Label=label;Value=value;}
    }
    // Immutable projection supplied by the existing completed-node/owned-effect
    // owner. This is not a new event ledger or proof that a candidate was owned.
    public sealed class AugmentNodeResultEntry
    {
        public string Id {get;}
        public AugmentCardPresentation Card {get;}
        public AugmentRecordCoverage Coverage {get;}
        public string ScopeLabel {get;}
        public IReadOnlyList<AugmentResultMetric> Metrics {get;}
        public AugmentNodeResultEntry(string id,AugmentCardPresentation card,AugmentRecordCoverage coverage,string scopeLabel,IEnumerable<AugmentResultMetric> metrics)
        {
            if(string.IsNullOrWhiteSpace(id)||card==null||!Enum.IsDefined(typeof(AugmentRecordCoverage),coverage))throw new ArgumentException();
            var values=(metrics??Array.Empty<AugmentResultMetric>()).ToArray();
            if(values.Any(x=>x==null)||values.Select(x=>x.Label).Distinct().Count()!=values.Length||coverage==AugmentRecordCoverage.None&&values.Length!=0||coverage!=AugmentRecordCoverage.None&&values.Length==0||coverage==AugmentRecordCoverage.Partial&&string.IsNullOrWhiteSpace(scopeLabel))throw new ArgumentException("Explicit measured scope and units required");
            Id=id;Card=card;Coverage=coverage;ScopeLabel=scopeLabel;Metrics=Array.AsReadOnly(values);
        }
        public string Summary=>Coverage==AugmentRecordCoverage.None?"기록 없음":(Coverage==AugmentRecordCoverage.Partial?ScopeLabel+"\n":Metrics.All(x=>x.Value==0)?"이번 전투 발동결과 없음\n":"")+string.Join("\n",Metrics.Take(2).Select(x=>x.Label+" "+x.Value.ToString("N0",CultureInfo.InvariantCulture)));
        public string Details=>Summary+(Metrics.Count>2?"\n"+string.Join("\n",Metrics.Skip(2).Select(x=>x.Label+" "+x.Value.ToString("N0",CultureInfo.InvariantCulture))):"")+"\n\n"+Card.Details;
    }
    public sealed class AugmentNodeResult
    {
        public string RunId {get;}
        public string NodeId {get;}
        public long FinalSequence {get;}
        public IReadOnlyList<AugmentNodeResultEntry> Entries {get;}
        public AugmentNodeResult(string runId,string nodeId,long finalSequence,IEnumerable<AugmentNodeResultEntry> entries)
        {
            if(string.IsNullOrWhiteSpace(runId)||string.IsNullOrWhiteSpace(nodeId)||finalSequence<0||entries==null)throw new ArgumentException();
            var rows=entries.ToArray();if(rows.Any(x=>x==null)||rows.Select(x=>x.Id).Distinct().Count()!=rows.Length)throw new ArgumentException("Unique original owned order required");
            RunId=runId;NodeId=nodeId;FinalSequence=finalSequence;Entries=Array.AsReadOnly(rows);
        }
    }
}
