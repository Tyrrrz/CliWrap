using System;
using System.Collections.Generic;
using System.Diagnostics;
using CliWrap.Formatters;

namespace CliWrap
{
    /// <summary>
    /// Wrapper for a Command Line Interface
    /// </summary>
    public class Cli
    {
        /// <summary>
        /// Target file path
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Argument formatter
        /// </summary>
        public IArgumentFormatter Formatter { get; }

        /// <summary>
        /// Initializes CLI wrapper on a target with a custom formatter
        /// </summary>
        public Cli(string filePath, IArgumentFormatter formatter)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            FilePath = filePath;
            Formatter = formatter;
        }

        /// <summary>
        /// Initializes CLI wrapper on a target
        /// </summary>
        public Cli(string filePath)
            : this(filePath, new SpacedArgumentFormatter())
        {
        }

        private string ExecuteInternal(string args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            var process = new Process
            {
                StartInfo =
                {
                    FileName = FilePath,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Arguments = args,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            process.Start();
            string stdOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return stdOut;
        }

        /// <summary>
        /// Executes with the given arguments
        /// </summary>
        public string Execute(IEnumerable<Argument> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            string args = Formatter.Format(arguments);
            return ExecuteInternal(args);
        }

        /// <summary>
        /// Executes with the given arguments
        /// </summary>
        public string Execute(params Argument[] arguments)
            => Execute((IEnumerable<Argument>) arguments);
    }
}