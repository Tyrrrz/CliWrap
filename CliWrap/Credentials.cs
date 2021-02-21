namespace CliWrap
{
    /// <summary>
    /// User credentials used for running a process.
    /// </summary>
    public partial class Credentials
    {
        /// <summary>
        /// Active directory domain.
        /// </summary>
        /// <remarks>Supported only on Windows.</remarks>
        public string? Domain { get; }

        /// <summary>
        /// User name.
        /// </summary>
        public string? UserName { get; }

        /// <summary>
        /// User password.
        /// </summary>
        /// <remarks>Supported only on Windows.</remarks>
        public string? Password { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Credentials"/>.
        /// </summary>
        public Credentials(string? domain = null, string? userName = null, string? password = null)
        {
            Domain = domain;
            UserName = userName;
            Password = password;
        }
    }

    public partial class Credentials
    {
        /// <summary>
        /// Empty credentials.
        /// </summary>
        public static Credentials Default { get; } = new();
    }
}