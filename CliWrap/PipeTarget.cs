using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap
{
    public abstract partial class PipeTarget
    {
        public abstract Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default);
    }

    public partial class PipeTarget
    {
        public static PipeTarget FromStream(Stream stream) => new StreamPipeTarget(stream);

        public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
        {
            var actualTargets = targets.Where(t => t != Null).ToArray();

            if (actualTargets.Length == 1)
                return actualTargets.Single();

            if (actualTargets.Length == 0)
                return Null;

            return new MergedPipeTarget(actualTargets);
        }

        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);

        public static PipeTarget Null { get; } = FromStream(Stream.Null);
    }

    internal class StreamPipeTarget : PipeTarget
    {
        private readonly Stream _stream;

        public StreamPipeTarget(Stream stream)
        {
            _stream = stream;
        }

        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            source.CopyToAsync(_stream, cancellationToken);
    }

    internal class MergedPipeTarget : PipeTarget
    {
        private readonly IReadOnlyList<PipeTarget> _targets;

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets)
        {
            _targets = targets;
        }

        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            Task.WhenAll(_targets.Select(t => t.CopyFromAsync(source, cancellationToken)));
    }
}