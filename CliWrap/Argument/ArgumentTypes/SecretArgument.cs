
namespace CliWrap.Argument.ArgumentTypes {

    /// <summary> Represents a secret argument. </summary>
    public sealed class SecretArgument : IProcessArgument {

        private readonly IProcessArgument _argument;

        /// <summary> Initializes a new instance of the <see cref="SecretArgument"/> class. </summary>
        /// <param name="argument"> The argument. </param>
        public SecretArgument(IProcessArgument argument) {
            _argument = argument;
        }

        /// <summary>
        ///     Render the arguments as a <see cref="string" />. The secret text will be included.
        /// </summary>
        /// <returns> A string representation of the argument. </returns>
        public string Render() {
            return _argument.Render();
        }

        /// <summary>
        ///     Renders the argument as a <see cref="string" />.
        ///      The secret text will be redacted.
        /// </summary>
        /// <returns> A safe string representation of the argument. </returns>
        public string RenderSafe() {
            return "[REDACTED]";
        }

        /// <summary> Returns a <see cref="string" /> that represents the current object. </summary>
        /// <returns> A string that represents the current object. </returns>
        public override string ToString() {
            return RenderSafe();
        }
    }
}