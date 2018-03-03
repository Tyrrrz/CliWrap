using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using CliWrap.Services;

namespace CliWrap
{
    /// <summary>
    /// Interface for <see cref="Cli" />.
    /// </summary>
    public interface ICli : IDisposable
    {
        /// <summary>
        /// File path of the target executable.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Settings to use when executing the target executable.
        /// </summary>
        CliSettings Settings { get; }

        #region Execute

        /// <summary>
        /// Executes target process with given input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="input">Execution input.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        ExecutionOutput Execute(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        ExecutionOutput Execute(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        /// <summary>
        /// Executes target process without input, waits until completion synchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        ExecutionOutput Execute(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        #endregion

        #region ExecuteAndForget

        /// <summary>
        /// Executes target process with given input, without waiting for completion.
        /// </summary>
        /// <param name="input">Execution input.</param>
        void ExecuteAndForget(ExecutionInput input);

        /// <summary>
        /// Executes target process with given command line arguments, without waiting for completion.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        void ExecuteAndForget(string arguments);

        /// <summary>
        /// Executes target process without input, without waiting for completion.
        /// </summary>
        void ExecuteAndForget();

        #endregion

        #region ExecuteAsync

        /// <summary>
        /// Executes target process with given input, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="input">Execution input.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        Task<ExecutionOutput> ExecuteAsync(ExecutionInput input,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        /// <summary>
        /// Executes target process with given command line arguments, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="arguments">Command line arguments passed when executing the target process.</param>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        Task<ExecutionOutput> ExecuteAsync(string arguments,
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        /// <summary>
        /// Executes target process without input, waits until completion asynchronously and returns produced output.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to abort execution.</param>
        /// <param name="bufferHandler">Handler for real-time standard output and standard error data.</param>
        /// <remarks>The underlying process is killed if the execution is canceled.</remarks>
        Task<ExecutionOutput> ExecuteAsync(
            CancellationToken cancellationToken = default(CancellationToken),
            IBufferHandler bufferHandler = null);

        #endregion

        /// <summary>
        /// Cancels all currently running execution tasks.
        /// </summary>
        /// <remarks>Doesn't affect processes instantiated by <see cref="ExecuteAndForget()"/>.</remarks>
        void CancelAll();
    }
}