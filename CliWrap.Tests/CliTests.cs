using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliWrap.Tests
{
    [TestClass]
    public class CliTests
    {
        private const string EchoArgsBat = "Bats\\EchoArgs.bat";
        private const string EchoEnvVarBat = "Bats\\EchoEnvVar.bat";
        private const string EchoStdinBat = "Bats\\EchoStdin.bat";
        private const string NeverEndingBat = "Bats\\NeverEnding.bat";
        private const string ThrowErrorBat = "Bats\\ThrowError.bat";

        [TestMethod, Timeout(5000)]
        public void Execute_EchoArgs_Test()
        {
            using (var cli = new Cli(EchoArgsBat))
            {
                var output = cli.Execute("Hello world");
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_EchoStdin_Test()
        {
            using (var cli = new Cli(EchoStdinBat))
            {
                var input = new ExecutionInput(standardInput: "Hello world");
                var output = cli.Execute(input);
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_EchoStdin_Empty_Test()
        {
            using (var cli = new Cli(EchoStdinBat))
            {
                var output = cli.Execute();
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("ECHO is off.", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_EchoEnvVar_Test()
        {
            using (var cli = new Cli(EchoEnvVarBat))
            {
                var input = new ExecutionInput();
                input.EnvironmentVariables.Add("TEST_ENV_VAR", "Hello world");

                var output = cli.Execute(input);
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_ThrowError_Test()
        {
            using (var cli = new Cli(ThrowErrorBat))
            {
                var output = cli.Execute();
                var ex = Assert.ThrowsException<StandardErrorException>(() => output.ThrowIfError());

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("", output.StandardOutput.TrimEnd());
                Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
                Assert.AreEqual(output.StandardError, ex.StandardError);
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_NeverEnding_CancelEarly_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.ThrowsException<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [TestMethod, Timeout(5000)]
        public void Execute_NeverEnding_CancelLate_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(1));
                Assert.ThrowsException<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [TestMethod, Timeout(5000)]
        public void ExecuteAndForget_EchoArgs_Test()
        {
            using (var cli = new Cli(EchoArgsBat))
            {
                cli.ExecuteAndForget("Hello world");
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_EchoArgs_Test()
        {
            using (var cli = new Cli(EchoArgsBat))
            {
                var output = await cli.ExecuteAsync("Hello world");
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_EchoStdin_Test()
        {
            using (var cli = new Cli(EchoStdinBat))
            {
                var input = new ExecutionInput(standardInput: "Hello world");
                var output = await cli.ExecuteAsync(input);
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_EchoStdin_Empty_Test()
        {
            using (var cli = new Cli(EchoStdinBat))
            {
                var output = await cli.ExecuteAsync();
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("ECHO is off.", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_EchoEnvVar_Test()
        {
            using (var cli = new Cli(EchoEnvVarBat))
            {
                var input = new ExecutionInput();
                input.EnvironmentVariables.Add("TEST_ENV_VAR", "Hello world");

                var output = await cli.ExecuteAsync(input);
                output.ThrowIfError();

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
                Assert.AreEqual("", output.StandardError.TrimEnd());
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_ThrowError_Test()
        {
            using (var cli = new Cli(ThrowErrorBat))
            {
                var output = await cli.ExecuteAsync();
                var ex = Assert.ThrowsException<StandardErrorException>(() => output.ThrowIfError());

                Assert.IsNotNull(output);
                Assert.AreEqual(14, output.ExitCode);
                Assert.AreEqual("", output.StandardOutput.TrimEnd());
                Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
                Assert.AreEqual(output.StandardError, ex.StandardError);
                Assert.IsTrue(output.StartTime < output.ExitTime);
                Assert.IsTrue(TimeSpan.Zero < output.RunTime);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_NeverEnding_CancelEarly_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_NeverEnding_CancelLate_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(1));
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [TestMethod, Timeout(5000)]
        public void KillAllProcesses_Execute_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            {
                // Kill after some time
                Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        cli.KillAllProcesses();
                    })
                    .Forget();

                var output = cli.Execute();

                Assert.IsNotNull(output);
                Assert.AreNotEqual(14, output.ExitCode);
            }
        }

        [TestMethod, Timeout(5000)]
        public async Task KillAllProcesses_ExecuteAsync_Test()
        {
            using (var cli = new Cli(NeverEndingBat))
            {
                // Kill after some time
                Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        cli.KillAllProcesses();
                    })
                    .Forget();

                var output = await cli.ExecuteAsync();

                Assert.IsNotNull(output);
                Assert.AreNotEqual(14, output.ExitCode);
            }
        }
    }
}