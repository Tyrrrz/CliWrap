using System;

namespace CliWrap
{
    /// <summary>
    /// Command line argument
    /// </summary>
    public class Argument
    {
        /// <summary>
        /// Argument name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Argument value
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Initializes a command line argument with given name and value
        /// </summary>
        public Argument(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Initializes a boolean command line argument with value set to true
        /// </summary>
        public Argument(string name)
            : this(name, true)
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}