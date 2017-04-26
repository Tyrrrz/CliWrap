using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliWrap.Tests
{
    [TestClass]
    public class CliTests
    {
        private const string ArgsEchoFilePath = "Bats\\ArgsEcho.bat";
        private const string LongRunningFilePath = "Bats\\LongRunning.bat";
        private const string StdErrFilePath = "Bats\\StdErr.bat";

        [TestMethod]
        public void Execute_Normal_Test()
        {
            var cli = new Cli(ArgsEchoFilePath);

            string output = cli.Execute("Hello World");

            Assert.AreEqual("Hello World", output.TrimEnd());
        }

        [TestMethod]
        public void Execute_StdErr_Test()
        {
            var cli = new Cli(StdErrFilePath);

            var ex = Assert.ThrowsException<StdErrException>(() => cli.Execute());

            Assert.AreEqual("Hello from standard error", ex.StdErr.TrimEnd());
        }

        [TestMethod]
        public void ExecuteAndForget_Normal_Test()
        {
            var cli = new Cli(ArgsEchoFilePath);

            cli.ExecuteAndForget();
        }

        [TestMethod]
        public void ExecuteAndForget_StdErr_Test()
        {
            var cli = new Cli(StdErrFilePath);

            cli.ExecuteAndForget();

            // No exception should be thrown regardless
        }

        [TestMethod]
        public async Task ExecuteAsync_Normal_Test()
        {
            var cli = new Cli(ArgsEchoFilePath);

            string output = await cli.ExecuteAsync("Hello World");

            Assert.AreEqual("Hello World", output.TrimEnd());
        }

        [TestMethod, Timeout(5000)]
        public async Task ExecuteAsync_Cancel_Test()
        {
            var cli = new Cli(LongRunningFilePath);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
        }

        [TestMethod]
        public async Task ExecuteAsync_StdErr_Test()
        {
            var cli = new Cli(StdErrFilePath);

            var ex = await Assert.ThrowsExceptionAsync<StdErrException>(() => cli.ExecuteAsync());

            Assert.AreEqual("Hello from standard error", ex.StdErr.TrimEnd());
        }
    }
}