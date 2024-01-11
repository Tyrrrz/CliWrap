using System.Diagnostics.CodeAnalysis;

namespace CliWrap;

/// <summary>
/// User credentials used for starting a process.
/// </summary>
public partial class Credentials(
    string? domain = null,
    string? userName = null,
    string? password = null,
    bool loadUserProfile = false
)
{
    /// <summary>
    /// Initializes an instance of <see cref="Credentials" />.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overload
    [ExcludeFromCodeCoverage]
    public Credentials(string? domain, string? username, string? password)
        : this(domain, username, password, false) { }

    /// <summary>
    /// Active Directory domain used for starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public string? Domain { get; } = domain;

    /// <summary>
    /// Username used for starting the process.
    /// </summary>
    public string? UserName { get; } = userName;

    /// <summary>
    /// Password used for starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public string? Password { get; } = password;

    /// <summary>
    /// Whether to load the user profile when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public bool LoadUserProfile { get; } = loadUserProfile;
}

public partial class Credentials
{
    /// <summary>
    /// Empty credentials.
    /// </summary>
    public static Credentials Default { get; } = new();
}
