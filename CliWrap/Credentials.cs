using System.Diagnostics.CodeAnalysis;

namespace CliWrap;

/// <summary>
/// User credentials used for starting a process.
/// </summary>
public partial class Credentials
{
    /// <summary>
    /// Active Directory domain used when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public string? Domain { get; }

    /// <summary>
    /// Username used when starting the process.
    /// </summary>
    public string? UserName { get; }

    /// <summary>
    /// Password used when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public string? Password { get; }

    /// <summary>
    /// Whether to load the user profile when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public bool LoadUserProfile { get; }

    /// <summary>
    /// Initializes an instance of <see cref="Credentials" />.
    /// </summary>
    public Credentials(
        string? domain = null,
        string? userName = null,
        string? password = null,
        bool loadUserProfile = false)
    {
        Domain = domain;
        UserName = userName;
        Password = password;
        LoadUserProfile = loadUserProfile;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Credentials" />.
    /// </summary>
    // TODO: (breaking change) remove in favor of the other overload
    [ExcludeFromCodeCoverage]
    public Credentials(
        string? domain,
        string? username,
        string? password)
        : this(domain, username, password, false)
    {
    }
}

public partial class Credentials
{
    /// <summary>
    /// Empty credentials.
    /// </summary>
    public static Credentials Default { get; } = new();
}