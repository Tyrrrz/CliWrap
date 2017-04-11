using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliWrap.Tests
{
    [TestClass]
    public class CliTests
    {
        [TestMethod]
        public void ExecuteTest()
        {
            var cli = new Cli("ArgsEcho.bat");

            string output = cli.Execute();
            Assert.AreEqual(string.Empty, output);

            output = cli.Execute("Hello World");
            Assert.AreEqual("Hello World", output);
        }

        [TestMethod]
        public void ExecuteAndForgetTest()
        {
            var cli = new Cli("ArgsEcho.bat");

            cli.ExecuteAndForget();
            cli.ExecuteAndForget("Hello World");

            // Better than nothing
        }

        [TestMethod]
        public async Task ExecuteAsyncTest()
        {
            var cli = new Cli("ArgsEcho.bat");

            string output = await cli.ExecuteAsync();
            Assert.AreEqual(string.Empty, output);

            output = await cli.ExecuteAsync("Hello World");
            Assert.AreEqual("Hello World", output);
        }
    }
}