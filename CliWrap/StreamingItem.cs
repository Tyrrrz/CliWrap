namespace CliWrap
{
    public class StreamingItem
    {
        public StandardStream Source { get; }

        public string Data { get; }

        public StreamingItem(StandardStream source, string data)
        {
            Source = source;
            Data = data;
        }

        public void Deconstruct(out StandardStream source, out string data)
        {
            source = Source;
            data = Data;
        }
    }
}