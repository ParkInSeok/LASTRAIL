using System;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        internal Func<bool> CaptureScreenSession()
        {
            var original=session();var stamp=generation();
            return ()=>Current(original,stamp);
        }
    }
}
