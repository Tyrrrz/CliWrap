using CliWrap.Internal;
using System;
using System.Text;

namespace CliWrap.Models
{
    /// <summary>
    /// Specifies the encodings to use for each input/output stream.
    /// </summary>
    public partial class EncodingSettings
    {
        /// <summary>
        /// Encoding to use for standard input.
        /// </summary>
        public Encoding StandardInput { get; }

        /// <summary>
        /// Encoding to use for standard output.
        /// </summary>
        public Encoding StandardOutput { get; }

        /// <summary>
        /// Encoding to use for standard error.
        /// </summary>
        public Encoding StandardError { get; }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with default encodings
        /// (<see cref="Console.InputEncoding"/> and <see cref="Console.OutputEncoding"/>)
        /// </summary>
        public EncodingSettings()
        {
            StandardInput = Console.InputEncoding;
            StandardOutput = Console.OutputEncoding;
            StandardError = Console.OutputEncoding;
        }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with the same encoding for all streams.
        /// </summary>
        public EncodingSettings(Encoding inputOutput)
        {
            StandardInput = inputOutput.GuardNotNull(nameof(inputOutput));
            StandardOutput = inputOutput;
            StandardError = inputOutput;
        }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with separate encoding for input/output streams.
        /// </summary>
        public EncodingSettings(Encoding input, Encoding output)
        {
            StandardInput = input.GuardNotNull(nameof(input));
            StandardOutput = output.GuardNotNull(nameof(output));
            StandardError = output;
        }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with separate encodings for all streams.
        /// </summary>
        public EncodingSettings(Encoding standardInput, Encoding standardOutput, Encoding standardError)
        {
            StandardInput = standardInput.GuardNotNull(nameof(standardInput));
            StandardOutput = standardOutput.GuardNotNull(nameof(standardOutput));
            StandardError = standardError.GuardNotNull(nameof(standardError));
        }
    }

    public partial class EncodingSettings
    {
        /// <summary>
        /// Default settings.
        /// </summary>
        public static EncodingSettings Default { get; } = new EncodingSettings();
    }
}