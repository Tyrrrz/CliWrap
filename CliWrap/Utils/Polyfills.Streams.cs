// ReSharper disable CheckNamespace

#if NET461 || NETSTANDARD2_0
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

internal static class StreamPolyfills
{
    public static async ValueTask<int> ReadAsync(
        this Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        return await stream.ReadAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> ReadAsync(
        this StreamReader reader,
        Memory<char> buffer,
        CancellationToken cancellationToken)
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
        CancellationToken cancellationToken)
    {
        if (!MemoryMarshal.TryGetArray(buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask CopyToAsync(
        this Stream stream,
        Stream destination,
        CancellationToken cancellationToken) =>
        await stream.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
}
#endif