using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Buffered;
using CliWrap.Tests.Utils;
using Xunit;

namespace CliWrap.Tests;

public class CancellationSpecs
{
    [Fact(Timeout = 15000)]
    public async Task I_can_execute_a_command_bound_to_the_shutdown_token_and_it_gets_killed_when_the_current_process_exits()
    {
        // Arrange
        // The dummy spawns a child process bound to the shutdown token, prints its ID, and exits without waiting for it
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithArguments(["spawn", Dummy.Program.FilePath, "sleep", "00:00:20"]);

        // Act
        var result = await cmd.ExecuteBufferedAsync();

        // Assert
        var childProcessId = int.Parse(result.StandardOutput.Trim(), CultureInfo.InvariantCulture);
        await WaitForExitAsync(childProcessId);
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_bound_to_the_shutdown_token_and_it_gets_killed_when_the_current_process_receives_SIGTERM()
    {
        Skip.If(OperatingSystem.IsWindows(), "SIGTERM is only supported on Unix-like systems.");

        // Arrange
        var childProcessIdTcs = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        // The dummy spawns a child process bound to the shutdown token, prints its ID, and waits for it,
        // so that it's still running when the signal arrives
        var cmd =
            Cli.Wrap(Dummy.Program.FilePath)
                .WithArguments(["spawn", Dummy.Program.FilePath, "sleep", "00:00:20", "--wait"])
                .WithValidation(CommandResultValidation.None)
            | PipeTarget.ToDelegate(line =>
                childProcessIdTcs.TrySetResult(int.Parse(line, CultureInfo.InvariantCulture))
            );

        var task = cmd.ExecuteAsync();
        var childProcessId = await childProcessIdTcs.Task;

        // Act
        await Cli.Wrap("kill")
            .WithArguments(["-TERM", task.ProcessId.ToString(CultureInfo.InvariantCulture)])
            .ExecuteAsync();

        await task;

        // Assert
        await WaitForExitAsync(childProcessId);
    }

    // The kill signal is sent before the parent exits, but the OS may need a moment to tear the process down
    private static async Task WaitForExitAsync(int processId)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            // The process has already exited
            return;
        }

        using (process)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(timeoutCts.Token);
        }
    }
}
