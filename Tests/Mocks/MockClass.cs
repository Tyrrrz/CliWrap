namespace CliWrap.Tests.Mocks
{
    public class MockClass
    {
        public int X { get; }
        public int Y { get; }

        public MockClass(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{X}x{Y}";
        }
    }
}