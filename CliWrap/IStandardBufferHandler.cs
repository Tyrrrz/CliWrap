namespace CliWrap
{
    public interface IStandardBufferHandler
    {
        void HandleStandardOutput(string line);
        void HandleStandardError(string line);
    }
}
