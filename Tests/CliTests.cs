using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliWrap.Tests
{
    [TestClass]
    public class CliTests
    {
        private const string EchoArgsBat = "Bats\\EchoArgs.bat";
        private const string EchoStdinBat = "Bats\\EchoStdin.bat";
        private const string NeverEndingBat = "Bats\\NeverEnding.bat";
        private const string ThrowErrorBat = "Bats\\ThrowError.bat";

        [TestMethod]
        public void Execute_EchoArgs_Test()
        {
            var cli = new Cli(EchoArgsBat);

            var output = cli.Execute("Hello world");

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
            Assert.AreEqual("", output.StandardError.TrimEnd());
        }

        [TestMethod]
        public void Execute_EchoStdin_Test()
        {
            var cli = new Cli(EchoStdinBat);

            var input = new ExecutionInput(standardInput: "Hello world");
            var output = cli.Execute(input);

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
            Assert.AreEqual("", output.StandardError.TrimEnd());
        }

        [TestMethod]
        public void Execute_ThrowError_Test()
        {
            var cli = new Cli(ThrowErrorBat);

            var output = cli.Execute();

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("", output.StandardOutput.TrimEnd());
            Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
        }

        [TestMethod]
        public void ExecuteAndForget_EchoArgs_Test()
        {
            var cli = new Cli(EchoArgsBat);

            cli.ExecuteAndForget("Hello world");
        }

        [TestMethod]
        public void ExecuteAndForget_EchoStdin_Test()
        {
            var cli = new Cli(EchoStdinBat);

            var input = new ExecutionInput(standardInput: "Hello world");
            cli.ExecuteAndForget(input);
        }

        [TestMethod]
        public void ExecuteAndForget_ThrowError_Test()
        {
            var cli = new Cli(ThrowErrorBat);

            cli.ExecuteAndForget();
        }

        [TestMethod]
        public async Task ExecuteAsync_EchoArgs_Test()
        {
            var cli = new Cli(EchoArgsBat);

            var output = await cli.ExecuteAsync("Hello world");

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
            Assert.AreEqual("", output.StandardError.TrimEnd());
        }

        [TestMethod]
        public async Task ExecuteAsync_EchoStdin_Test()
        {
            var cli = new Cli(EchoStdinBat);

            var input = new ExecutionInput(standardInput: "Hello world");
            var output = await cli.ExecuteAsync(input);

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("Hello world", output.StandardOutput.TrimEnd());
            Assert.AreEqual("", output.StandardError.TrimEnd());
        }

        [TestMethod]
        public async Task ExecuteAsync_ThrowError_Test()
        {
            var cli = new Cli(ThrowErrorBat);

            var output = await cli.ExecuteAsync();

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("", output.StandardOutput.TrimEnd());
            Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_CancelEarly_Test()
        {
            var cli = new Cli(NeverEndingBat);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_CancelLate_Test()
        {
            var cli = new Cli(NeverEndingBat);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
        }
    }
}