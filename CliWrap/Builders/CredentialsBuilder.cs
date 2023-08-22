namespace CliWrap.Builders;

/// <summary>
/// Builder that helps configure user credentials.
/// </summary>
public class CredentialsBuilder
{
    private string? _domain;
    private string? _userName;
    private string? _password;
    private bool _loadUserProfile;

    /// <summary>
    /// Sets the Active Directory domain used when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public CredentialsBuilder SetDomain(string? domain)
    {
        _domain = domain;
        return this;
    }

    /// <summary>
    /// Sets the username used when starting the process.
    /// </summary>
    public CredentialsBuilder SetUserName(string? userName)
    {
        _userName = userName;
        return this;
    }

    /// <summary>
    /// Sets the password used when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public CredentialsBuilder SetPassword(string? password)
    {
        _password = password;
        return this;
    }

    /// <summary>
    /// Instructs whether to load the user profile when starting the process.
    /// </summary>
    /// <remarks>
    /// Only supported on Windows.
    /// </remarks>
    public CredentialsBuilder LoadUserProfile(bool loadUserProfile = true)
    {
        _loadUserProfile = loadUserProfile;
        return this;
    }

    /// <summary>
    /// Builds the resulting credentials.
    /// </summary>
    public Credentials Build() => new(_domain, _userName, _password, _loadUserProfile);
}
