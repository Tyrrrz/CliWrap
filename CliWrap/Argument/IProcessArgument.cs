
namespace CliWrap.Argument {

    /// <summary> Represents a process argument. </summary>
    public interface IProcessArgument {

        /// <summary>
        ///     Render the argument as a <see cref="string"/>. Sensitive information will be included.
        /// </summary>
        /// <returns> A string representation of the argument. </returns>
        string Render();

        /// <summary>
        ///     Renders the argument as a <see cref="string"/>. Sensitive information will be redacted.
        /// </summary>
        /// <returns> A safe string representation of the argument. </returns>
        string RenderSafe();
    }
}