
namespace CliWrap.Argument.ArgumentTypes {

    /// <summary> Represents a argument preceded by a switch. </summary>
    public class SwitchArgument : IProcessArgument {

        private readonly string _switch;
        private readonly IProcessArgument _argument;
        private readonly string _separator;

        /// <summary> Initializes a new instance of the <see cref="SwitchArgument"/> class. </summary>
        /// <param name="switch">    The switch. </param>
        /// <param name="argument">  The argument. </param>
        /// <param name="separator"> (Optional) The separator between the <paramref name="switch"/> and
        ///                          the <paramref name="argument"/>. </param>
        public SwitchArgument(string @switch, IProcessArgument argument, string separator = " ") {
            _switch = @switch;
            _argument = argument;
            _separator = separator;
        }

        /// <summary>
        ///     Render the arguments as a <see cref="string" />. Sensitive information will be
        ///     included.
        /// </summary>
        /// <returns> A string representation of the argument. </returns>
        public string Render() {
            return string.Concat(_switch, _separator, _argument.Render());
        }

        /// <summary>
        ///     Renders the argument as a <see cref="string" />.
        ///      The secret text will be redacted.
        /// </summary>
        /// <returns> A safe string representation of the argument. </returns>
        public string RenderSafe() {
            return string.Concat(_switch, _separator, _argument.RenderSafe());
        }

        /// <summary> Returns a string that represents the current object. </summary>
        /// <returns> A string that represents the current object. </returns>
        public override string ToString() {
            return RenderSafe();
        }
    }
}