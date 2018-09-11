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
    public class Tests
    {
        private const string TestString = "Hello world";
        private const int TestExitCode = 0;
        private const string TestEnvVar = "TEST_ENV_VAR";

        private readonly string _echoArgsToStdoutBat;
        private readonly string _echoStdinToStdoutBat;
        private readonly string _echoEnvVarToStdoutBat;
        private readonly string _echoArgsToStderrBat;
        private readonly string _sleepBat;
        private readonly string _echoSpamBat;

        public Tests()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _echoArgsToStdoutBat = Path.Combine(testDir, "Bats\\EchoArgsToStdout.bat");
            _echoStdinToStdoutBat = Path.Combine(testDir, "Bats\\EchoStdinToStdout.bat");
            _echoEnvVarToStdoutBat = Path.Combine(testDir, "Bats\\EchoEnvVarToStdout.bat");
            _echoArgsToStderrBat = Path.Combine(testDir, "Bats\\EchoArgsToStderr.bat");
            _sleepBat = Path.Combine(testDir, "Bats\\Sleep.bat");
            _echoSpamBat = Path.Combine(testDir, "Bats\\EchoSpam.bat");
        }

        #region Execute

        [Test]
        public void Execute_EchoArgsToStdout_Test()
        {
            var result = new Cli(_echoArgsToStdoutBat)
                .WithArguments(TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Test()
        {
            var result = new Cli(_echoStdinToStdoutBat)
                .WithStandardInput(TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Empty_Test()
        {
            var result = new Cli(_echoStdinToStdoutBat).Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoEnvVarToStdout_Test()
        {
            var result = new Cli(_echoEnvVarToStdoutBat)
                .WithEnvironmentVariable(TestEnvVar, TestString)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void Execute_EchoArgsToStderr_Test()
        {
            var ex = Assert.Throws<StandardErrorValidationException>(() => new Cli(_echoArgsToStderrBat)
                .WithArguments(TestString)
                .Execute());

            Assert.That(ex.StandardError.TrimEnd(), Is.EqualTo(TestString));
        }

        [Test]
        public void Execute_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat).WithCancellationToken(cts.Token);

                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => cli.Execute());
            }
        }

        [Test]
        public void Execute_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat).WithCancellationToken(cts.Token);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.Throws<OperationCanceledException>(() => cli.Execute());
            }
        }

        [Test]
        public void Execute_EchoSpam_BufferHandler_Test()
        {
            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var result = new Cli(_echoSpamBat)
                .WithStandardOutputObserver(l => stdOutBuffer.AppendLine(l))
                .WithStandardErrorObserver(l => stdErrBuffer.AppendLine(l))
                .WithStandardErrorValidation(false)
                .Execute();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(result.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        #endregion

        #region ExecuteAndForget

        [Test]
        public void ExecuteAndForget_EchoArgsToStdout_Test()
        {
            new Cli(_echoArgsToStdoutBat).ExecuteAndForget();
        }

        #endregion

        #region ExecuteAsync

        [Test]
        public async Task ExecuteAsync_EchoArgsToStdout_Test()
        {
            var result = await new Cli(_echoArgsToStdoutBat)
                .WithArguments(TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Test()
        {
            var result = await new Cli(_echoStdinToStdoutBat)
                .WithStandardInput(TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Empty_Test()
        {
            var result = await new Cli(_echoStdinToStdoutBat).ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoEnvVarToStdout_Test()
        {
            var result = await new Cli(_echoEnvVarToStdoutBat)
                .WithEnvironmentVariable(TestEnvVar, TestString)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(result.StandardError.TrimEnd(), Is.Empty);
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        [Test]
        public void ExecuteAsync_EchoArgsToStderr_Test()
        {
            var ex = Assert.ThrowsAsync<StandardErrorValidationException>(() => new Cli(_echoArgsToStderrBat)
                .WithArguments(TestString)
                .ExecuteAsync());

            Assert.That(ex.StandardError.TrimEnd(), Is.EqualTo(TestString));
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat).WithCancellationToken(cts.Token);

                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync());
            }
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat).WithCancellationToken(cts.Token);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync());
            }
        }

        [Test]
        public async Task ExecuteAsync_EchoSpam_BufferHandler_Test()
        {
            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var result = await new Cli(_echoSpamBat)
                .WithStandardOutputObserver(l => stdOutBuffer.AppendLine(l))
                .WithStandardErrorObserver(l => stdErrBuffer.AppendLine(l))
                .WithStandardErrorValidation(false)
                .ExecuteAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(result.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(result.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(result.StartTime, Is.LessThanOrEqualTo(result.ExitTime));
            Assert.That(result.RunTime, Is.EqualTo(result.ExitTime - result.StartTime));
        }

        #endregion
    }
}