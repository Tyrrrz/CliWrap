using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using NUnit.Framework;

namespace CliWrap.Tests
{
    [TestFixture]
    [Timeout(5000)]
    public class IntegrationTests
    {
        private const string TestString = "Hello world";
        private const string TestEnvVar = "TEST_ENV_VAR";

        private string TestDirPath => TestContext.CurrentContext.TestDirectory;

        private string EchoArgsToStdoutBat => Path.Combine(TestDirPath, "Bats\\EchoArgsToStdout.bat");
        private string EchoStdinToStdoutBat => Path.Combine(TestDirPath, "Bats\\EchoStdinToStdout.bat");
        private string EchoEnvVarToStdoutBat => Path.Combine(TestDirPath, "Bats\\EchoEnvVarToStdout.bat");
        private string EchoArgsToStderrBat => Path.Combine(TestDirPath, "Bats\\EchoArgsToStderr.bat");
        private string EchoSpamBat => Path.Combine(TestDirPath, "Bats\\EchoSpam.bat");
        private string SleepBat => Path.Combine(TestDirPath, "Bats\\Sleep.bat");
        private string NonZeroExitCodeBat => Path.Combine(TestDirPath, "Bats\\NonZeroExitCode.bat");

        #region Execute

        [Test]
        public void Execute_EchoArgsToStdout_Test()
        {
            var result = new Cli(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Test()
        {
            var result = new Cli(EchoStdinToStdoutBat)
                .SetStandardInput(TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Empty_Test()
        {
            var result = new Cli(EchoStdinToStdoutBat).Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoEnvVarToStdout_Test()
        {
            var result = new Cli(EchoEnvVarToStdoutBat)
                .SetEnvironmentVariable(TestEnvVar, TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoArgsToStderr_Test()
        {
            var result = new Cli(EchoArgsToStderrBat)
                .SetArguments(TestString)
                .EnableStandardErrorValidation(false)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(result.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoSpam_Callback_Test()
        {
            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var result = new Cli(EchoSpamBat)
                .SetStandardOutputCallback(l => stdOutBuffer.AppendLine(l))
                .SetStandardErrorCallback(l => stdErrBuffer.AppendLine(l))
                .EnableStandardErrorValidation(false)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(result.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(SleepBat).SetCancellationToken(cts.Token);

                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => cli.Execute());
            }
        }

        [Test]
        public void Execute_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(SleepBat).SetCancellationToken(cts.Token);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.Throws<OperationCanceledException>(() => cli.Execute());
            }
        }

        [Test]
        public void Execute_EchoArgsToStderr_Validation_Test()
        {
            var ex = Assert.Throws<StandardErrorValidationException>(() =>
                new Cli(EchoArgsToStderrBat)
                    .SetArguments(TestString)
                    .EnableStandardErrorValidation()
                    .Execute());

            Assert.That(ex.ExecutionResult, Is.Not.Null);
            Assert.That(ex.ExecutionResult.ExitCode, Is.Zero);
            Assert.That(ex.ExecutionResult.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(ex.ExecutionResult.StartTime, Is.LessThanOrEqualTo(ex.ExecutionResult.ExitTime));
            Assert.That(ex.ExecutionResult.RunTime, Is.EqualTo(ex.ExecutionResult.ExitTime - ex.ExecutionResult.StartTime));
            Assert.That(ex.StandardError, Is.EqualTo(ex.ExecutionResult.StandardError));
        }

        [Test]
        public void Execute_NonZeroExitCode_Validation_Test()
        {
            var ex = Assert.Throws<ExitCodeValidationException>(() =>
                new Cli(NonZeroExitCodeBat)
                    .EnableExitCodeValidation()
                    .Execute());

            Assert.That(ex.ExecutionResult, Is.Not.Null);
            Assert.That(ex.ExecutionResult.ExitCode, Is.Not.Zero);
            Assert.That(ex.ExecutionResult.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StandardError.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StartTime, Is.LessThanOrEqualTo(ex.ExecutionResult.ExitTime));
            Assert.That(ex.ExecutionResult.RunTime, Is.EqualTo(ex.ExecutionResult.ExitTime - ex.ExecutionResult.StartTime));
            Assert.That(ex.ExitCode, Is.EqualTo(ex.ExecutionResult.ExitCode));
        }

        #endregion

        #region ExecuteAndForget

        [Test]
        public void ExecuteAndForget_EchoArgsToStdout_Test()
        {
            new Cli(EchoArgsToStdoutBat).ExecuteAndForget();
        }

        #endregion

        #region ExecuteAsync

        [Test]
        public async Task ExecuteAsync_EchoArgsToStdout_Test()
        {
            var result = await new Cli(EchoArgsToStdoutBat)
                .SetArguments(TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Test()
        {
            var result = await new Cli(EchoStdinToStdoutBat)
                .SetStandardInput(TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Empty_Test()
        {
            var result = await new Cli(EchoStdinToStdoutBat).ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoEnvVarToStdout_Test()
        {
            var result = await new Cli(EchoEnvVarToStdoutBat)
                .SetEnvironmentVariable(TestEnvVar, TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStderr_Test()
        {
            var result = await new Cli(EchoArgsToStderrBat)
                .SetArguments(TestString)
                .EnableStandardErrorValidation(false)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(result.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoSpam_Callback_Test()
        {
            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var result = await new Cli(EchoSpamBat)
                .SetStandardOutputCallback(l => stdOutBuffer.AppendLine(l))
                .SetStandardErrorCallback(l => stdErrBuffer.AppendLine(l))
                .EnableStandardErrorValidation(false)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(result.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(SleepBat).SetCancellationToken(cts.Token);

                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync());
            }
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(SleepBat).SetCancellationToken(cts.Token);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync());
            }
        }

        [Test]
        public void ExecuteAsync_EchoArgsToStderr_Validation_Test()
        {
            var ex = Assert.ThrowsAsync<StandardErrorValidationException>(() =>
                new Cli(EchoArgsToStderrBat)
                    .SetArguments(TestString)
                    .EnableStandardErrorValidation()
                    .ExecuteAsync());

            Assert.That(ex.ExecutionResult, Is.Not.Null);
            Assert.That(ex.ExecutionResult.ExitCode, Is.Zero);
            Assert.That(ex.ExecutionResult.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(ex.ExecutionResult.StartTime, Is.LessThanOrEqualTo(ex.ExecutionResult.ExitTime));
            Assert.That(ex.ExecutionResult.RunTime, Is.EqualTo(ex.ExecutionResult.ExitTime - ex.ExecutionResult.StartTime));
            Assert.That(ex.StandardError, Is.EqualTo(ex.ExecutionResult.StandardError));
        }

        [Test]
        public void ExecuteAsync_NonZeroExitCode_Validation_Test()
        {
            var ex = Assert.ThrowsAsync<ExitCodeValidationException>(() =>
                new Cli(NonZeroExitCodeBat)
                    .EnableExitCodeValidation()
                    .ExecuteAsync());

            Assert.That(ex.ExecutionResult, Is.Not.Null);
            Assert.That(ex.ExecutionResult.ExitCode, Is.Not.Zero);
            Assert.That(ex.ExecutionResult.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StandardError.TrimEnd(), Is.Empty);
            Assert.That(ex.ExecutionResult.StartTime, Is.LessThanOrEqualTo(ex.ExecutionResult.ExitTime));
            Assert.That(ex.ExecutionResult.RunTime, Is.EqualTo(ex.ExecutionResult.ExitTime - ex.ExecutionResult.StartTime));
            Assert.That(ex.ExitCode, Is.EqualTo(ex.ExecutionResult.ExitCode));
        }

        #endregion
    }
}