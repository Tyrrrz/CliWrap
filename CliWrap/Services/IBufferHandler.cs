namespace CliWrap.Services
{
    /// <summary>
    /// Used to handle the standard output and standard error of CLI processes
    /// </summary>
    public interface IBufferHandler
    {
        /// <summary>
        /// Gets called when a line is written to the standard output
        /// </summary>
        /// <param name="line">The line written to the standard output</param>
        void HandleStandardOutput(string line);

        /// <summary>
        /// Gets called when a line is written to the standard error
        /// </summary>
        /// <param name="line">The line written to the standard error</param>
        void HandleStandardError(string line);
    }
}