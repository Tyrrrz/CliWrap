namespace CliWrap.Builders
{
    /// <summary>
    /// Builder that helps configure user credentials.
    /// </summary>
    public class CredentialsBuilder
    {
        private string? _domain;
        private string? _userName;
        private string? _password;

        /// <summary>
        /// Sets the active directory domain to the specified value.
        /// </summary>
        /// <remarks>Supported only on Windows.</remarks>
        public CredentialsBuilder SetDomain(string? domain)
        {
            _domain = domain;
            return this;
        }

        /// <summary>
        /// Sets the user name to the specified value.
        /// </summary>
        public CredentialsBuilder SetUserName(string? userName)
        {
            _userName = userName;
            return this;
        }

        /// <summary>
        /// Sets the user password to the specified value.
        /// </summary>
        /// <remarks>Supported only on Windows.</remarks>
        public CredentialsBuilder SetPassword(string? password)
        {
            _password = password;
            return this;
        }

        /// <summary>
        /// Builds the resulting credentials.
        /// </summary>
        public Credentials Build() => new(
            _domain,
            _userName,
            _password
        );
    }
}