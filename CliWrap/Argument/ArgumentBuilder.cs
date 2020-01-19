using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CliWrap.Argument.ArgumentTypes;

namespace CliWrap.Argument {

    /// <summary> Utility for building process arguments. </summary>
    public sealed class ArgumentBuilder : IReadOnlyCollection<IProcessArgument> {

        private readonly List<IProcessArgument> _tokens;

        /// <summary>
        ///     Gets the number of arguments contained in the <see cref="ArgumentBuilder"/>.
        /// </summary>
        /// <value> The count. </value>
        public int Count => _tokens.Count;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArgumentBuilder"/> class.
        /// </summary>
        public ArgumentBuilder() {
            _tokens = new List<IProcessArgument>();
        }

        /// <summary> Clears all arguments from the builder. </summary>
        public void Clear() {
            _tokens.Clear();
        }

        /// <summary> Appends an argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void Append(IProcessArgument argument) {
            _tokens.Add(argument);
        }

        /// <summary> Prepends an argument. </summary>
        /// <param name="argument"> The argument. </param>
        public void Prepend(IProcessArgument argument) {
            _tokens.Insert(0, argument);
        }

        /// <summary>
        ///     Renders the arguments as a <see cref="string"/>. Sensitive information will be included.
        /// </summary>
        /// <returns> A string representation of the arguments. </returns>
        public string Render() {
            return string.Join(" ", _tokens.Select(t => t.Render()));
        }

        /// <summary>
        ///     Renders the arguments as a <see cref="string"/>. Sensitive information will be redacted.
        /// </summary>
        /// <returns> A safe string representation of the arguments. </returns>
        public string RenderSafe() {
            return string.Join(" ", _tokens.Select(t => t.RenderSafe()));
        }

        /// <summary> Tries to filer any unsafe arguments from string. </summary>
        /// <param name="source"> unsafe source string. </param>
        /// <returns> Filtered string. </returns>
        public string FilterUnsafe(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                return source;
            }

            return _tokens
                .Select(token => new {
                    Safe = token.RenderSafe().Trim('"').Trim(),
                    Unsafe = token.Render().Trim('"').Trim()
                })
                .Where(token => token.Safe != token.Unsafe)
                .Aggregate(
                    new StringBuilder(source),
                    (sb, token) => sb.Replace(token.Unsafe, token.Safe),
                    sb => sb.ToString());
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="string"/> to
        ///     <see cref="ArgumentBuilder"/>.
        /// </summary>
        /// <param name="value"> The text value to convert. </param>
        /// <returns> A builder representation of the string. </returns>
        public static implicit operator ArgumentBuilder(string value) {
            return FromString(value);
        }

        /// <summary>
        ///     Performs a conversion from <see cref="string"/> to
        ///     <see cref="ArgumentBuilder"/>.
        /// </summary>
        /// <param name="value"> The text value to convert. </param>
        /// <returns> A builder representation of the string. </returns>
        public static ArgumentBuilder FromString(string value) {
            var builder = new ArgumentBuilder();
            builder.Append(new TextArgument(value));
            return builder;
        }

        /// <summary> Returns an enumerator that iterates through the collection. </summary>
        /// <typeparam> Type of the process argument. </typeparam>
        /// <returns> An enumerator that can be used to iterate through the collection. </returns>
        IEnumerator<IProcessArgument> IEnumerable<IProcessArgument>.GetEnumerator() {
            return _tokens.GetEnumerator();
        }

        /// <summary> Returns an enumerator that iterates through a collection. </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through
        ///     the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)_tokens).GetEnumerator();
        }
    }
}