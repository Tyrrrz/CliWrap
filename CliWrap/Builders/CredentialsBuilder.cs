namespace CliWrap.Builders;

/// <summary>
/// Builder that helps configure user credentials.
/// </summary>
public class CredentialsBuilder : ICredentialsConfigurator
{
    private string? _domain;
    private string? _userName;
    private string? _password;

    /// <inheritdoc />
    public CredentialsBuilder SetDomain(string? domain)
    {
        _domain = domain;
        return this;
    }

    /// <inheritdoc />
    public CredentialsBuilder SetUserName(string? userName)
    {
        _userName = userName;
        return this;
    }

    /// <inheritdoc />
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