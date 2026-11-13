using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        sealed class PassClaimAllProgress
        {
            public readonly List<BulkClaimCommand> Commands = new List<BulkClaimCommand>();
            public readonly List<BulkClaimResult> Results = new List<BulkClaimResult>();
            public int Index;
            public bool Aborted;
        }

        PassClaimAllProgress passClaimAll;
        bool passClaimAllRunning;
        public bool HasPassClaimAllResult => passClaimAll != null;
        public bool CanContinuePassClaimAll => passClaimAll != null && !passClaimAll.Aborted &&
            passClaimAll.Index < passClaimAll.Commands.Count && !Busy;
        public JArray PassClaimAllItems => new JArray(passClaimAll?.Results.SelectMany(r => r.Items) ?? Enumerable.Empty<JToken>());
        public string PassClaimAllNotice
        {
            get
            {
                if (passClaimAll == null) return "";
                if (passClaimAll.Commands.Count == 0) return "지금 수령할 수 있는 패스 보상이 없습니다.";
                var results = passClaimAll.Results;
                var summary = "성공 " + results.Sum(r => r.Success) + " · 실패 " + results.Sum(r => r.Failed) + " · 미확정 " + results.Sum(r => r.Pending);
                return summary + "\n" + (passClaimAllRunning ? "무료·프리미엄 보상을 확인하고 있습니다."
                    : passClaimAll.Index == passClaimAll.Commands.Count ? "전체 요청의 처리가 끝났습니다."
                    : passClaimAll.Aborted ? "요청 조건을 확인하지 못했습니다. 최신 패스 상태를 확인해 주세요."
                    : "미완료 요청과 남은 트랙을 이어받을 수 있습니다.");
            }
        }

        public Task ClaimAllPassAsync(string policy)
        {
            SynchronizeSession();
            if (Busy || Pending != null || !RecurringReady || RecurringLoading) return Task.CompletedTask;
            var progress = new PassClaimAllProgress();
            foreach (var track in new[] { "FREE", "PREMIUM" })
            {
                var selection = PrepareBulkSelection("PASS_LEVEL", policy, track);
                if (selection == null) return Task.CompletedTask;
                var ids = selection.Rows.Select(row => (string)row["id"]).ToArray();
                if (ids.Length == 0) continue;
                var command = PrepareBulkCommand(selection, ids);
                if (command == null) return Task.CompletedTask;
                progress.Commands.Add(command);
            }
            passClaimAll = progress;
            return RunPassClaimAllAsync(progress);
        }

        public Task ContinuePassClaimAllAsync()
        {
            SynchronizeSession();
            return CanContinuePassClaimAll ? RunPassClaimAllAsync(passClaimAll) : Task.CompletedTask;
        }

        async Task RunPassClaimAllAsync(PassClaimAllProgress progress)
        {
            var original = owner;
            var stamp = ownerGeneration;
            if (!Current(original, stamp) || Busy) return;
            passClaimAllRunning = true;
            Changed?.Invoke();
            try
            {
                while (progress.Index < progress.Commands.Count && Current(original, stamp) && ReferenceEquals(passClaimAll, progress))
                {
                    var command = progress.Commands[progress.Index];
                    var previous = LastBulkResult;
                    await SubmitBulkStoredAsync(command.Payload, true);
                    if (!Current(original, stamp) || !ReferenceEquals(passClaimAll, progress)) return;
                    var result = LastBulkResult;
                    var matches = result != null && JToken.DeepEquals(JObject.Parse(result.RequestJson), JObject.Parse(command.Payload));
                    if (matches)
                    {
                        if (progress.Results.Count == progress.Index) progress.Results.Add(result);
                        else progress.Results[progress.Index] = result;
                    }
                    if (Pending != null)
                    {
                        // Continue only a newly confirmed partial response that reduced remaining work.
                        // Lost/malformed responses retain the original journal and require explicit retry.
                        if (!matches || ReferenceEquals(previous, result) || result.Complete ||
                            previous != null && JToken.DeepEquals(JObject.Parse(previous.RequestJson), JObject.Parse(result.RequestJson)) && result.Pending >= previous.Pending)
                            return;
                        continue;
                    }
                    if (!matches || !result.Complete || ActionErrorCode != null)
                    { progress.Aborted = true; return; }
                    progress.Index++;
                }
            }
            finally
            {
                if (ReferenceEquals(passClaimAll, progress))
                {
                    passClaimAllRunning = false;
                    if (!disposed) Changed?.Invoke();
                }
            }
        }
    }
}
