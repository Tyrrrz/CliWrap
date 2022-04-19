using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace CliWrap
{
    /// <summary>
    /// Represents a command parameter that should be kept confidential.
    /// A masked value will appear in all sensitive output (such as logging).
    /// </summary>
    public sealed class SensitiveString : IDisposable
    {
        internal const string DefaultMask = "*****";
        private SecureString _value;
        private string _mask;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref='SensitiveString'/> class with value and default mask.
        /// </summary>
        /// <param name="value">The sensitive value.</param>
        public SensitiveString(string? value) : this(value, DefaultMask)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='SensitiveString'/> class with value and default mask.
        /// </summary>
        /// <param name="value">The sensitive value.</param>
        public SensitiveString(SecureString value) : this(value, DefaultMask)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='SensitiveString'/> class with value and mask.
        /// </summary>
        /// <param name="value">The sensitive value.</param>
        /// <param name="mask">The mask value.</param>
        public SensitiveString(string? value, string mask) : this(SecureStringHelper.MarshalToSecureString(value), mask)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='SensitiveString'/> class with value and mask.
        /// </summary>
        /// <param name="value">The sensitive value.</param>
        /// /// <param name="mask">The mask value.</param>
        public SensitiveString(SecureString value, string mask)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (mask == null)
            {
                throw new ArgumentNullException(nameof(mask));
            }
            _value = value;
            _mask = mask;
        }

        /// <summary>
        /// Returns the string representation of the object.
        /// </summary>
        /// <param name="value">The <see cref='SensitiveString'/> value.</param>
        /// <returns>The mask value.</returns>
        [return: NotNullIfNotNull("value")]
        public static implicit operator string?(SensitiveString? value)
        {
            return value?.ToString();
        }
        
        /// <summary>
        /// Returns the masked value of the current object.
        /// </summary>
        /// <returns>The mask value.</returns>
        public override string ToString()
        {
            GuardNotDisposed();
            return _mask;
        }
        
        internal string? UnsecureString
        {
            get
            {
                GuardNotDisposed();
                return SecureStringHelper.MarshalToString(_value);
            }
        }

        private void GuardNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SensitiveString));
            }
        }
        
        /// <summary>
        /// Releases all resources used by the current <see cref='SensitiveString'/> object.
        /// </summary>
        public void Dispose()
        {
            _value.Dispose();
            _disposed = true;
        }
    }
}
