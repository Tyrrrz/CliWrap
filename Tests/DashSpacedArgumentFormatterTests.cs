using CliWrap.Formatters;
using CliWrap.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliWrap.Tests
{
    [TestClass]
    public class DashSpacedArgumentFormatterTests
    {
        [TestMethod]
        public void FormatTests()
        {
            var formatter = new DashSpacedArgumentFormatter();

            var a = new Argument[0];
            var b = new[] { new Argument("single") };
            var c = new[] { new Argument("b"), new Argument("bool"), new Argument("bool2", true), new Argument("bool3", false) };
            var d = new[] { new Argument("s", "Hello"), new Argument("str", "Hello World") };
            var e = new[] { new Argument("q", new MockClass(1, 2)), new Argument("qqq", new MockClass(3, 4)) };

            string af = formatter.Format(a);
            string bf = formatter.Format(b);
            string cf = formatter.Format(c);
            string df = formatter.Format(d);
            string ef = formatter.Format(e);

            Assert.AreEqual("", af);
            Assert.AreEqual("-single", bf);
            Assert.AreEqual("-b -bool -bool2", cf);
            Assert.AreEqual("-s Hello -str Hello World", df);
            Assert.AreEqual("-q 1x2 -qqq 3x4", ef);
        }
    }
}