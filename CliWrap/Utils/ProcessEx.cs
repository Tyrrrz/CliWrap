﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal class ProcessEx : IDisposable
{
    private readonly Process _nativeProcess;

    private readonly TaskCompletionSource<object?> _exitTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CancellationTokenSource? _waitTimeoutCts;
    private CancellationTokenRegistration? _waitTimeoutRegistration;

    public int Id { get; private set; }

    // We are purposely using Stream instead of StreamWriter/StreamReader to push the concerns of
    // writing and reading to PipeSource/PipeTarget at the higher level.

    public Stream StandardInput { get; private set; } = Stream.Null;

    public Stream StandardOutput { get; private set; } = Stream.Null;

    public Stream StandardError { get; private set; } = Stream.Null;

    public int ExitCode { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset ExitTime { get; private set; }

    public ProcessEx(ProcessStartInfo startInfo) =>
        _nativeProcess = new Process { StartInfo = startInfo };

    public void Start()
    {
        Debug.Assert(Id == default, "Attempt to launch a process more than once.");

        // Hook up events
        _nativeProcess.EnableRaisingEvents = true;
        _nativeProcess.Exited += (_, _) =>
        {
            ExitCode = _nativeProcess.ExitCode;

            // Calculate our own ExitTime to be consistent with StartTime
            ExitTime = DateTimeOffset.Now;

            // We don't handle cancellation here.
            // If necessary, proper exception will be thrown upstream.
            _exitTcs.TrySetResult(null);
        };

        // Start the process
        try
        {
            if (!_nativeProcess.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start a process with file path '{_nativeProcess.StartInfo.FileName}'. " +
                    "Target file is not an executable or lacks execute permissions."
                );
            }
        }
        catch (Win32Exception ex)
        {
            throw new Win32Exception(
                $"Failed to start a process with file path '{_nativeProcess.StartInfo.FileName}'. " +
                "Target file or working directory doesn't exist, or the provided credentials are invalid.",
                ex
            );
        }

        // We can't access Process.StartTime if the process has already exited.
        // It's entirely possible that the process exits so fast that by the time
        // we try to get the start time, it won't be accessible anymore.
        // Calculating time ourselves is slightly inaccurate, but at least we can
        // guarantee it won't fail.
        // https://github.com/Tyrrrz/CliWrap/issues/93
        StartTime = DateTimeOffset.Now;

        // Copy metadata and stream references
        Id = _nativeProcess.Id;
        StandardInput = _nativeProcess.StandardInput.BaseStream;
        StandardOutput = _nativeProcess.StandardOutput.BaseStream;
        StandardError = _nativeProcess.StandardError.BaseStream;
    }

    // Sends SIGINT
    public void Interrupt()
    {
        bool TryInterrupt()
        {
            // Sending an interrupt signal to a specific process is only possible on Unix.
            // On Windows, we can only send the signal to an entire process group, which
            // has the risk of bringing down unrelated processes. There are some workarounds,
            // but they are brittle and not worth the effort.
            // https://github.com/Tyrrrz/CliWrap/issues/47

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return NativeMethods.Unix.Kill(_nativeProcess.Id, 2) == 0;
            }

            return false;
        }

        if (!TryInterrupt())
        {
            // In case of failure, just do nothing and assume the user prepared a fallback
            Debug.Fail("Failed to send an interrupt signal.");
        }
    }

    // Sends SIGKILL
    public void Kill()
    {
        try
        {
            _nativeProcess.Kill(true);
        }
        catch when (_nativeProcess.HasExited)
        {
            // The process has exited before we could kill it. This is fine.
        }
        catch
        {
            // Either we actually failed to kill the process, or the system hasn't finished processing
            // the kill request yet. In either case, we can't really do anything about it.
            // Set up a timeout that will resolve the task after a while, in case the process hasn't exited by then.
            _waitTimeoutCts = new CancellationTokenSource();
            _waitTimeoutRegistration = _waitTimeoutCts.Token.Register(() =>
            {
                // We don't cancel the task here. Proper exception will be thrown upstream.
                if (_exitTcs.TrySetResult(null))
                    Debug.Fail("Process termination timed out.");
            });
            _waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
        }
    }

    public async Task WaitUntilExitAsync() => await _exitTcs.Task.ConfigureAwait(false);

    public void Dispose()
    {
        _waitTimeoutRegistration?.Dispose();
        _waitTimeoutCts?.Dispose();
        _nativeProcess.Dispose();
    }
}