using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.EventStream;

internal partial class CommandEventAsyncEnumerable : IAsyncEnumerable<CommandEvent>
{
    public IAsyncEnumerator<CommandEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new Enumerator(cancellationToken);
}

internal partial class CommandEventAsyncEnumerable
{
    private class Enumerator : IAsyncEnumerator<CommandEvent>
    {
        private readonly CancellationToken _cancellationToken;

        public CommandEvent Current { get; } = default!;

        public Enumerator(CancellationToken cancellationToken) =>
            _cancellationToken = cancellationToken;

        public ValueTask<bool> MoveNextAsync()
        {
            throw new System.NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}