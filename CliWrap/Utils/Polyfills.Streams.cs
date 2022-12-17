// ReSharper disable CheckNamespace
// ReSharper disable RedundantUsingDirective
// ReSharper disable PartialTypeWithSinglePart

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NET462 || NETSTANDARD2_0
internal static partial class StreamPolyfills
{
    public static async ValueTask<int> ReadAsync(
        this Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        return await stream.ReadAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> ReadAsync(
        this StreamReader reader,
        Memory<char> buffer,
        CancellationToken cancellationToken = default)
    {
        // StreamReader doesn't accept cancellation token anywhere (pre-netstd2.1)
        cancellationToken.ThrowIfCancellationRequested();

        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<char>)buffer, out var segment))
            segment = new ArraySegment<char>(buffer.ToArray());

        return await reader.ReadAsync(segment.Array!, segment.Offset, segment.Count)
            .ConfigureAwait(false);
    }

    public static async ValueTask WriteAsync(
        this Stream stream,
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (!MemoryMarshal.TryGetArray(buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask CopyToAsync(
        this Stream stream,
        Stream destination,
        CancellationToken cancellationToken = default) =>
        await stream.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
}
#endif