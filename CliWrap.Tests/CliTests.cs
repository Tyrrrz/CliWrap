using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;
using CliWrap.Tests.Internal;
using NUnit.Framework;

namespace CliWrap.Tests
{
    [TestFixture]
    [Timeout(5000)]
    public class CliTests
    {
        private const string TestString = "Hello world";
        private const string TestEnvVar = "TEST_ENV_VAR";

        private string TestDirPath => TestContext.CurrentContext.TestDirectory;

        private string EchoArgsToStdoutBat => Path.Combine(TestDirPath, "Bats", "EchoArgsToStdout.bat");
        private string EchoFirstArgEscapedToStdoutBat => Path.Combine(TestDirPath, "Bats", "EchoFirstArgEscapedToStdout.bat");
        private string EchoStdinToStdoutBat => Path.Combine(TestDirPath, "Bats", "EchoStdinToStdout.bat");
        private string EchoEnvVarToStdoutBat => Path.Combine(TestDirPath, "Bats", "EchoEnvVarToStdout.bat");
        private string EchoArgsToStderrBat => Path.Combine(TestDirPath, "Bats", "EchoArgsToStderr.bat");
        private string EchoSpamBat => Path.Combine(TestDirPath, "Bats", "EchoSpam.bat");
        private string SleepBat => Path.Combine(TestDirPath, "Bats", "Sleep.bat");
        private string StartChildProcessesBat => Path.Combine(TestDirPath, "Bats", "StartChildProcesses.bat");
        private string NonZeroExitCodeBat => Path.Combine(TestDirPath, "Bats", "NonZeroExitCode.bat");

        private void AssertExecutionResult(ExecutionResult result,
            int expectedExitCode, string expectedStandardOutput, string expectedStandardError)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode), "Exit code");
                Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(expectedStandardOutput), "Stdout");
                Assert.That(result.StandardError.TrimEnd(), Is.EqualTo(expectedStandardError), "Stderr");
                Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime), "Start time / exit time");
                Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime), "Run time");
            });
        }

        #region Execute

        [Test]
        public void Execute_EchoArgsToStdout_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .Execute();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public void Execute_StdoutClosedCallback_Test()
        {
            var stdoutClosed = false;
            var stdErrClosed = false;

            // Arrange & act
            Cli.Wrap(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .SetStandardOutputClosedCallback(() => stdoutClosed = true)
                .SetStandardErrorClosedCallback(() => stdErrClosed = true)
                .Execute();

            // Assert
            Assert.IsTrue(stdoutClosed);
            Assert.IsTrue(stdErrClosed);
        }

        [Test]
        public void Execute_EchoFirstArgEscapedToStdout_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoFirstArgEscapedToStdoutBat)
                .SetArguments(new[] {TestString})
                .Execute();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public void Execute_EchoStdinToStdout_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoStdinToStdoutBat)
                .SetStandardInput(TestString)
                .Execute();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public void Execute_EchoStdinToStdout_Empty_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoStdinToStdoutBat).Execute();

            // Assert
            AssertExecutionResult(result, 0, "ECHO is off.", "");
        }

        [Test]
        public void Execute_EchoEnvVarToStdout_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoEnvVarToStdoutBat)
                .SetEnvironmentVariable(TestEnvVar, TestString)
                .Execute();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public void Execute_EchoArgsToStderr_Test()
        {
            // Arrange & act
            var result = Cli.Wrap(EchoArgsToStderrBat)
                .SetArguments(TestString)
                .EnableStandardErrorValidation(false)
                .Execute();

            // Assert
            AssertExecutionResult(result, 0, "", TestString);
        }

        [Test]
        public void Execute_EchoSpam_Callback_Test()
        {
            // Arrange & act
            var standardOutputBuffer = new StringBuilder();
            var standardErrorBuffer = new StringBuilder();
            var result = Cli.Wrap(EchoSpamBat)
                .SetStandardOutputCallback(l => standardOutputBuffer.AppendLine(l))
                .SetStandardErrorCallback(l => standardErrorBuffer.AppendLine(l))
                .EnableStandardErrorValidation(false)
                .Execute();

            // Assert
            var expectedStandardOutput = standardOutputBuffer.ToString().TrimEnd();
            var expectedStandardError = standardErrorBuffer.ToString().TrimEnd();
            AssertExecutionResult(result, 0, expectedStandardOutput, expectedStandardError);
        }

        [Test]
        public void Execute_StartChildProcesses_Cancel_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                var cli = Cli.Wrap(StartChildProcessesBat).SetCancellationToken(cts.Token, true);
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                // Act & assert
                Assert.Throws<OperationCanceledException>(() => cli.Execute());
                Assert.That(cli.ProcessId, Is.Not.Null, "Process ID");
                Assert.That(ProcessEx.GetDescendantProcesses(cli.ProcessId.Value), Is.Empty, "Child processes");
            }
        }

        [Test]
        public void Execute_Sleep_CancelEarly_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Act & assert
                Assert.Throws<OperationCanceledException>(() =>
                {
                    Cli.Wrap(SleepBat)
                        .SetCancellationToken(cts.Token)
                        .Execute();
                });
            }
        }

        [Test]
        public void Execute_Sleep_CancelLate_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                // Act & assert
                Assert.Throws<OperationCanceledException>(() =>
                {
                    Cli.Wrap(SleepBat)
                        .SetCancellationToken(cts.Token)
                        .Execute();
                });
            }
        }

        [Test]
        public void Execute_EchoArgsToStderr_Validation_Test()
        {
            // Arrange & act
            var ex = Assert.Throws<StandardErrorValidationException>(() =>
            {
                Cli.Wrap(EchoArgsToStderrBat)
                    .SetArguments(TestString)
                    .EnableStandardErrorValidation()
                    .Execute();
            });

            // Assert
            AssertExecutionResult(ex.ExecutionResult, 0, "", TestString);
            Assert.That(ex.StandardError, Is.EqualTo(ex.ExecutionResult.StandardError), "Exception stderr");
        }

        [Test]
        public void Execute_NonZeroExitCode_Validation_Test()
        {
            var ex = Assert.Throws<ExitCodeValidationException>(() =>
            {
                Cli.Wrap(NonZeroExitCodeBat)
                    .EnableExitCodeValidation()
                    .Execute();
            });

            // Assert
            AssertExecutionResult(ex.ExecutionResult, 14, "", "");
            Assert.That(ex.ExitCode, Is.EqualTo(ex.ExecutionResult.ExitCode), "Exception exit code");
        }

        #endregion

        #region ExecuteAndForget

        [Test]
        public void ExecuteAndForget_EchoArgsToStdout_Test()
        {
            // Arrange & act & assert
            Cli.Wrap(EchoArgsToStdoutBat).ExecuteAndForget();
        }

        #endregion

        #region ExecuteAsync

        [Test]
        public async Task ExecuteAsync_ProcessId_Test()
        {
            // Arrange
            var cli = Cli.Wrap(EchoArgsToStdoutBat)
                .SetArguments(TestString);

            // Act
            var task = cli.ExecuteAsync();

            // Assert
            Assert.That(cli.ProcessId, Is.Not.Null);

            // Process ID is available before task completes
            await task;
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStdout_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public async Task ExecuteAsync_StdoutClosedCallback_Test()
        {
            var stdoutClosed = false;
            var stdErrClosed = false;

            // Arrange & act
            await Cli.Wrap(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .SetStandardOutputClosedCallback(() => stdoutClosed = true)
                .SetStandardErrorClosedCallback(() => stdErrClosed = true)
                .ExecuteAsync();

            // Assert
            Assert.IsTrue(stdoutClosed);
            Assert.IsTrue(stdErrClosed);
        }

        [Test]
        public async Task ExecuteAsync_EchoFirstArgEscapedToStdout_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoFirstArgEscapedToStdoutBat)
                .SetArguments(new[] {TestString})
                .ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoStdinToStdoutBat)
                .SetStandardInput(TestString)
                .ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Empty_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoStdinToStdoutBat).ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, "ECHO is off.", "");
        }

        [Test]
        public async Task ExecuteAsync_EchoEnvVarToStdout_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoEnvVarToStdoutBat)
                .SetEnvironmentVariable(TestEnvVar, TestString)
                .ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, TestString, "");
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStderr_Test()
        {
            // Arrange & act
            var result = await Cli.Wrap(EchoArgsToStderrBat)
                .SetArguments(TestString)
                .EnableStandardErrorValidation(false)
                .ExecuteAsync();

            // Assert
            AssertExecutionResult(result, 0, "", TestString);
        }

        [Test]
        public async Task ExecuteAsync_EchoSpam_Callback_Test()
        {
            // Arrange & act
            var standardOutputBuffer = new StringBuilder();
            var standardErrorBuffer = new StringBuilder();
            var result = await Cli.Wrap(EchoSpamBat)
                .SetStandardOutputCallback(l => standardOutputBuffer.AppendLine(l))
                .SetStandardErrorCallback(l => standardErrorBuffer.AppendLine(l))
                .EnableStandardErrorValidation(false)
                .ExecuteAsync();

            // Assert
            var expectedStandardOutput = standardOutputBuffer.ToString().TrimEnd();
            var expectedStandardError = standardErrorBuffer.ToString().TrimEnd();
            AssertExecutionResult(result, 0, expectedStandardOutput, expectedStandardError);
        }

        [Test]
        public async Task ExecuteAsync_StartChildProcesses_Cancel_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                var cli = Cli.Wrap(StartChildProcessesBat).SetCancellationToken(cts.Token, true);

                // Act
                var executeTask = cli.ExecuteAsync();

                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.That(cli.ProcessId, Is.Not.Null, "Process ID");
                Assert.That(ProcessEx.GetDescendantProcesses(cli.ProcessId.Value), Is.Not.Empty, "Child processes (before cancel)");

                cts.Cancel();

                // Assert
                Assert.ThrowsAsync<OperationCanceledException>(() => executeTask);
                Assert.That(ProcessEx.GetDescendantProcesses(cli.ProcessId.Value), Is.Empty, "Child processes (after cancel)");
            }
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelEarly_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Act & assert
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await Cli.Wrap(SleepBat)
                        .SetCancellationToken(cts.Token)
                        .ExecuteAsync();
                });
            }
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelLate_Test()
        {
            // Arrange
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                // Act & assert
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await Cli.Wrap(SleepBat)
                        .SetCancellationToken(cts.Token)
                        .ExecuteAsync();
                });
            }
        }

        [Test]
        public void ExecuteAsync_EchoArgsToStderr_Validation_Test()
        {
            // Arrange & act
            var ex = Assert.ThrowsAsync<StandardErrorValidationException>(() =>
                Cli.Wrap(EchoArgsToStderrBat)
                    .SetArguments(TestString)
                    .EnableStandardErrorValidation()
                    .ExecuteAsync());

            // Assert
            AssertExecutionResult(ex.ExecutionResult, 0, "", TestString);
            Assert.That(ex.StandardError, Is.EqualTo(ex.ExecutionResult.StandardError), "Exception stderr");
        }

        [Test]
        public void ExecuteAsync_NonZeroExitCode_Validation_Test()
        {
            // Arrange & act
            var ex = Assert.ThrowsAsync<ExitCodeValidationException>(() =>
                Cli.Wrap(NonZeroExitCodeBat)
                    .EnableExitCodeValidation()
                    .ExecuteAsync());

            // Assert
            AssertExecutionResult(ex.ExecutionResult, 14, "", "");
            Assert.That(ex.ExitCode, Is.EqualTo(ex.ExecutionResult.ExitCode), "Exception exit code");
        }

        #endregion
    }
}