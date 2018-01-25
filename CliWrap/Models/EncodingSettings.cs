using CliWrap.Internal;
using System;
using System.Text;
using JetBrains.Annotations;

namespace CliWrap.Models
{
    /// <summary>
    /// Specifies the encodings to use for each input/output stream.
    /// </summary>
    public class EncodingSettings
    {
        private Encoding _standardInput;
        private Encoding _standardOutput;
        private Encoding _standardError;

        /// <summary>
        /// Encoding to use for standard input.
        /// </summary>
        [NotNull]
        public Encoding StandardInput
        {
            get => _standardInput;
            set => _standardInput = value.GuardNotNull(nameof(value));
        }

        /// <summary>
        /// Encoding to use for standard output.
        /// </summary>
        [NotNull]
        public Encoding StandardOutput
        {
            get => _standardOutput;
            set => _standardOutput = value.GuardNotNull(nameof(value));
        }

        /// <summary>
        /// Encoding to use for standard error.
        /// </summary>
        [NotNull]
        public Encoding StandardError
        {
            get => _standardError;
            set => _standardError = value.GuardNotNull(nameof(value));
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

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with separate encoding for input/output streams.
        /// </summary>
        public EncodingSettings(Encoding input, Encoding output)
            : this(input, output, output)
        {
        }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with the same encoding for all streams.
        /// </summary>
        public EncodingSettings(Encoding inputOutput)
            : this(inputOutput, inputOutput, inputOutput)
        {
        }

        /// <summary>
        /// Initializes <see cref="EncodingSettings" /> with default encodings
        /// (<see cref="Console.InputEncoding"/> and <see cref="Console.OutputEncoding"/>)
        /// </summary>
        public EncodingSettings()
            : this(Console.InputEncoding, Console.OutputEncoding)
        {
        }
    }
}