namespace CliWrap.Builders;

/// <summary>
/// Methods to configure credentials.
/// </summary>
public interface ICredentialsConfigurator
{
    /// <summary>
    /// Sets the active directory domain to the specified value.
    /// </summary>
    /// <remarks>Supported only on Windows.</remarks>
    CredentialsBuilder SetDomain(string? domain);

    /// <summary>
    /// Sets the user name to the specified value.
    /// </summary>
    CredentialsBuilder SetUserName(string? userName);

    /// <summary>
    /// Sets the user password to the specified value.
    /// </summary>
    /// <remarks>Supported only on Windows.</remarks>
    CredentialsBuilder SetPassword(string? password);
}