using System;
using System.Threading;
using System.Threading.Tasks;

namespace LastRail.Resources
{
    public sealed class AuthenticatedScreenEntry
    {
        private readonly Func<CancellationToken, Task> open;
        private readonly Action close;
        private CancellationTokenSource pending;
        private long generation;
        public bool Entering { get; private set; }
        public bool Entered { get; private set; }
        public bool Failed { get; private set; }
        public event Action Changed;

        public AuthenticatedScreenEntry(Func<CancellationToken, Task> open, Action close)
        {
            this.open = open ?? throw new ArgumentNullException(nameof(open));
            this.close = close ?? throw new ArgumentNullException(nameof(close));
        }

        public void Reset()
        {
            generation++;
            pending?.Cancel(); pending = null;
            Entering = false; Entered = false; Failed = false;
            close(); Changed?.Invoke();
        }

        public async Task<bool> EnterAsync(CancellationToken cancel)
        {
            if (Entering || Entered || cancel.IsCancellationRequested) return false;
            var owner = generation;
            var operation = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            pending = operation; Entering = true; Failed = false; Changed?.Invoke();
            try
            {
                await open(operation.Token);
                operation.Token.ThrowIfCancellationRequested();
                if (owner != generation) return false;
                Entered = true; return true;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception)
            {
                if (owner == generation && !operation.IsCancellationRequested) Failed = true;
                return false;
            }
            finally
            {
                if (owner == generation)
                {
                    Entering = false;
                    if (ReferenceEquals(pending, operation)) pending = null;
                    Changed?.Invoke();
                }
                operation.Dispose();
            }
        }
    }
}
