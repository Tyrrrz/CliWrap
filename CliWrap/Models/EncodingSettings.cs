using CliWrap.Internal;
using System;
using System.Text;

namespace CliWrap.Models
{
    /// <summary>
    /// Specifies the encodings to use for each input/output stream.
    /// </summary>
    public class EncodingSettings
    {
        /// <summary>
        /// Encoding to use for stdin.
        /// </summary>
        public Encoding StandardInput { get; }

        /// <summary>
        /// Encoding to use for stdout.
        /// </summary>
        public Encoding StandardOutput { get; }

        /// <summary>
        /// Encoding to use for stderr.
        /// </summary>
        public Encoding StandardError { get; }

        /// <summary>
        /// Used default encodings:
        /// <see cref="Console.InputEncoding"/> for <see cref="StandardInput"/> and
        /// <see cref="Console.OutputEncoding"/> for both <see cref="StandardOutput"/>
        /// and <see cref="StandardError"/>.
        /// </summary>
        public EncodingSettings()
        {
            StandardInput = Console.InputEncoding;
            StandardOutput = Console.OutputEncoding;
            StandardError = Console.OutputEncoding;
        }

        /// <summary>
        /// Use one encoding for all input/output streams.
        /// </summary>
        /// <param name="encoding">Encoding to use.</param>
        public EncodingSettings(Encoding encoding)
        {
            StandardInput = encoding.GuardNotNull(nameof(encoding));
            StandardOutput = encoding;
            StandardError = encoding;
        }

        /// <summary>
        /// Use separate encodings for input and output streams.
        /// </summary>
        /// <param name="inputs">Encoding to use for <see cref="StandardInput"/>.</param>
        /// <param name="outputs">Encoding to use for both <see cref="StandardOutput"/> and <see cref="StandardError"/>.</param>
        public EncodingSettings(Encoding inputs, Encoding outputs)
        {
            StandardInput = inputs.GuardNotNull(nameof(inputs));
            StandardOutput = outputs.GuardNotNull(nameof(outputs));
            StandardError = outputs;
        }

        /// <summary>
        /// Use separate encodings for each input/output stream.
        /// </summary>
        /// <param name="standardInput">Encoding to use for <see cref="StandardInput"/>.</param>
        /// <param name="standardOutput">Encoding to use for <see cref="StandardOutput"/>.</param>
        /// <param name="standardError">Encoding to use for <see cref="StandardError"/>.</param>
        public EncodingSettings(Encoding standardInput, Encoding standardOutput, Encoding standardError)
        {
            StandardInput = standardInput.GuardNotNull(nameof(standardInput));
            StandardOutput = standardOutput.GuardNotNull(nameof(standardOutput));
            StandardError = standardError.GuardNotNull(nameof(standardError));
        }
    }
}