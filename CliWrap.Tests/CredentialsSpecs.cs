using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace CliWrap.Tests;

public class CredentialsSpecs
{
    [SkippableFact(Timeout = 15000)]
    public async Task Command_can_be_executed_as_another_user()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process under a different user is only supported on Windows."
        );

        // We can't really test the happy path, but at least can verify
        // that the credentials have been passed by getting an exception.

        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a.Add(Dummy.Program.FilePath))
            .WithCredentials(c => c
                .SetUserName("user123")
                .SetPassword("pass123")
                .LoadUserProfile()
            );

        // Act & assert
        await Assert.ThrowsAsync<Win32Exception>(() => cmd.ExecuteAsync());
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Command_can_be_executed_as_another_user_under_a_different_domain()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process under a different user is only supported on Windows."
        );

        // We can't really test the happy path, but at least can verify
        // that the credentials have been passed by getting an exception.

        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a.Add(Dummy.Program.FilePath))
            .WithCredentials(c => c
                .SetDomain("domain123")
                .SetUserName("user123")
                .SetPassword("pass123")
                .LoadUserProfile()
            );

        // Act & assert
        await Assert.ThrowsAsync<Win32Exception>(() => cmd.ExecuteAsync());
    }

    [SkippableFact(Timeout = 15000)]
    public async Task Command_execution_throws_if_executed_as_another_user_on_an_unsupported_operating_system()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process under a different user is only supported on Windows."
        );

        // Arrange
        var cmd = Cli.Wrap("dotnet")
            .WithArguments(a => a.Add(Dummy.Program.FilePath))
            .WithCredentials(c => c
                .SetUserName("user123")
                .SetPassword("pass123")
            );

        // Act & assert
        await Assert.ThrowsAsync<NotSupportedException>(() => cmd.ExecuteAsync());
    }
}