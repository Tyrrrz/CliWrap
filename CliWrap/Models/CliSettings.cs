using System.IO;
using CliWrap.Internal;
using JetBrains.Annotations;

namespace CliWrap.Models
{
    /// <summary>
    /// Specifies settings for <see cref="Cli"/>.
    /// </summary>
    public class CliSettings
    {
        private string _workingDirectory;
        private EncodingSettings _encoding;

        /// <summary>
        /// Working directory.
        /// </summary>
        [NotNull]
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set => _workingDirectory = value.GuardNotNull(nameof(value));
        }

        /// <summary>
        /// Encodings to use for standard input, output and error.
        /// </summary>
        [NotNull]
        public EncodingSettings Encoding
        {
            get => _encoding;
            set => _encoding = value.GuardNotNull(nameof(value));
        }

        /// <summary>
        /// Initializes <see cref="CliSettings"/> with default settings.
        /// </summary>
        public CliSettings()
        {
            WorkingDirectory = Directory.GetCurrentDirectory();
            Encoding = new EncodingSettings();
        }
    }
}