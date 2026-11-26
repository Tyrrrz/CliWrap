using System.Threading;

namespace CliWrap.Tests.Utils;

// Mimics a UI thread that is blocked on async work: callbacks posted to it never run
internal sealed class NonPumpingSynchronizationContext : SynchronizationContext
{
    private int _postCount;

    public int PostCount => _postCount;

    public override void Post(SendOrPostCallback d, object? state) =>
        Interlocked.Increment(ref _postCount);
}
