namespace CliWrap.Services
{
    /// <summary>
    /// Defines a provider for handling real-time standard output and standard error data.
    /// </summary>
    public interface IBufferHandler
    {
        /// <summary>
        /// Gets called when underlying process writes a line to standard output.
        /// </summary>
        void HandleStandardOutput(string line);

        /// <summary>
        /// Gets called when underlying process writes a line to standard error.
        /// </summary>
        void HandleStandardError(string line);
    }
}