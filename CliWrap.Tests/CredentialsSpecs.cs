using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace CliWrap.Tests;

public class CredentialsSpecs
{
    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_different_user()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process as another user is only supported on Windows."
        );

        // We can't really test the happy path, but we can at least verify
        // that the credentials have been passed by getting an exception.

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithCredentials(c =>
                c.SetUserName("user123").SetPassword("pass123").LoadUserProfile()
            );

        // Act & assert
        await Assert.ThrowsAsync<Win32Exception>(() => cmd.ExecuteAsync());
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_execute_a_command_as_a_different_user_under_the_specified_domain()
    {
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process as another user is only supported on Windows."
        );

        // We can't really test the happy path, but we can at least verify
        // that the credentials have been passed by getting an exception.

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithCredentials(c =>
                c.SetDomain("domain123")
                    .SetUserName("user123")
                    .SetPassword("pass123")
                    .LoadUserProfile()
            );

        // Act & assert
        await Assert.ThrowsAsync<Win32Exception>(() => cmd.ExecuteAsync());
    }

    [SkippableFact(Timeout = 15000)]
    public async Task I_can_try_to_execute_a_command_as_a_different_user_and_get_an_error_if_the_operating_system_does_not_support_it()
    {
        Skip.If(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Starting a process as another user is fully supported on Windows."
        );

        // Arrange
        var cmd = Cli.Wrap(Dummy.Program.FilePath)
            .WithCredentials(c => c.SetUserName("user123").SetPassword("pass123"));

        // Act & assert
        await Assert.ThrowsAsync<NotSupportedException>(() => cmd.ExecuteAsync());
    }
}
