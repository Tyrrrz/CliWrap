using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CliWrap.Tests
{
    [TestClass]
    public class CliTests
    {
        private const string EchoArgsBat = "Bats\\EchoArgs.bat";
        private const string EchoStdinBat = "Bats\\EchoStdin.bat";
        private const string NeverEndingBat = "Bats\\NeverEnding.bat";
        private const string ThrowErrorBat = "Bats\\ThrowError.bat";

        private const string Ffmpeg = "Ffmpeg\\ffmpeg.exe";
        private const string FfmpegImageInput = "Ffmpeg\\picture.jpg";
        private const string FfmpegVideoOutput = "Ffmpeg\\video.mp4";

        ~CliTests()
        {
            if (File.Exists(FfmpegVideoOutput))
                File.Delete(FfmpegVideoOutput);
        }

        [TestMethod, Timeout(5000)]
        public void Execute_Ffmpeg_Test()
        {
            if (File.Exists(FfmpegVideoOutput))
                File.Delete(FfmpegVideoOutput);

            var cli = new Cli(Ffmpeg);
            var output = cli.Execute($"-loop 1 -framerate 2 -i \"{FfmpegImageInput}\" -t 0:01 {FfmpegVideoOutput}");

            Assert.IsNotNull(output);
            Assert.AreEqual(0, output.ExitCode);
            Assert.AreEqual(String.Empty, output.StandardOutput.TrimEnd());
            Assert.AreNotEqual(String.Empty, output.StandardError.TrimEnd());
        }

        [TestMethod]
        public void Execute_EchoArgs_Test()
        {
            var cli = new Cli(EchoArgsBat);

            var output = cli.Execute("Hello world");
            output.ThrowIfError();

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
            output.ThrowIfError();

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
            var ex = Assert.ThrowsException<StandardErrorException>(() => output.ThrowIfError());

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("", output.StandardOutput.TrimEnd());
            Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
            Assert.AreEqual(output.StandardError, ex.StandardError);
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
            output.ThrowIfError();

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
            output.ThrowIfError();

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
            var ex = Assert.ThrowsException<StandardErrorException>(() => output.ThrowIfError());

            Assert.IsNotNull(output);
            Assert.AreEqual(14, output.ExitCode);
            Assert.AreEqual("", output.StandardOutput.TrimEnd());
            Assert.AreEqual("Hello world", output.StandardError.TrimEnd());
            Assert.AreEqual(output.StandardError, ex.StandardError);
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