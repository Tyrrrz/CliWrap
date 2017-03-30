using CliWrap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class CliTests
    {
        [TestMethod]
        public void ExecuteTest()
        {
            var cli = new Cli("ArgsEcho.bat");

            var args = new[] {new Argument("b"), new Argument("test"), new Argument("ke", "strel")};
            string output = cli.Execute(args);

            Assert.AreEqual(cli.Formatter.Format(args), output);
        }
    }
}