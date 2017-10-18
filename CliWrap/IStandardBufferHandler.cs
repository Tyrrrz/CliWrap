namespace CliWrap
{
    /// <summary>
    /// Used to handle the standard output and standard error of the cli processes
    /// </summary>
    public interface IStandardBufferHandler
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
