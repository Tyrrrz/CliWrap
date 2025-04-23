using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CliWrap.Builders;

namespace CliWrap;

/// <summary>
/// Instructions for running a process.
/// </summary>
public partial class Command : ICommandConfiguration
{
    private CommandConfiguration _configuration;

    /// <summary>
    /// Initializes an instance of <see cref="Command" /> using the specified configuration values.
    /// </summary>
    public Command(
        string targetFilePath,
        string arguments,
        string workingDirPath,
        ResourcePolicy resourcePolicy,
        Credentials credentials,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CommandResultValidation validation,
        PipeSource standardInputPipe,
        PipeTarget standardOutputPipe,
        PipeTarget standardErrorPipe
    )
        : this(
            new CommandConfiguration(
                targetFilePath,
                arguments,
                workingDirPath,
                resourcePolicy,
                credentials,
                environmentVariables,
                validation,
                standardInputPipe,
                standardOutputPipe,
                standardErrorPipe
            )
        ) { }

    /// <summary>
    /// Initializes an instance of <see cref="Command" /> using the default configuration.
    /// </summary>
    public Command(string targetFilePath)
        : this(new CommandConfiguration(targetFilePath)) { }

    /// <summary>
    /// Initializes an instance of <see cref="Command" /> using the configuration from
    /// the specified <see cref="ICommandConfiguration"/> instance.
    /// </summary>
    public Command(ICommandConfiguration configuration)
    {
        _configuration = new(
            configuration.TargetFilePath,
            configuration.Arguments,
            configuration.WorkingDirPath,
            configuration.ResourcePolicy,
            configuration.Credentials,
            configuration.EnvironmentVariables,
            configuration.Validation,
            configuration.StandardInputPipe,
            configuration.StandardOutputPipe,
            configuration.StandardErrorPipe
        );
    }

    /// <summary>
    /// Gets the <see cref="ICommandConfiguration"/>  instance that contains the configuration values for this command.
    /// </summary>
    public ICommandConfiguration Configuration => _configuration;

    // TODO: (breaking change) remove below delegating implementation of ICommandConfiguration

    /// <inheritdoc/>
    public string TargetFilePath => _configuration.TargetFilePath;

    /// <inheritdoc/>
    public string Arguments => _configuration.Arguments;

    /// <inheritdoc/>
    public string WorkingDirPath => _configuration.WorkingDirPath;

    /// <inheritdoc/>
    public ResourcePolicy ResourcePolicy => _configuration.ResourcePolicy;

    /// <inheritdoc/>
    public Credentials Credentials => _configuration.Credentials;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string?> EnvironmentVariables =>
        _configuration.EnvironmentVariables;

    /// <inheritdoc/>
    public CommandResultValidation Validation => _configuration.Validation;

    /// <inheritdoc/>
    public PipeSource StandardInputPipe => _configuration.StandardInputPipe;

    /// <inheritdoc/>
    public PipeTarget StandardOutputPipe => _configuration.StandardOutputPipe;

    /// <inheritdoc/>
    public PipeTarget StandardErrorPipe => _configuration.StandardErrorPipe;

    /// <summary>
    /// Sets the target file path to the specified value.
    /// </summary>
    public Command WithTargetFile(string targetFilePath)
    {
        _configuration = _configuration with { TargetFilePath = targetFilePath };
        return this;
    }

    /// <summary>
    /// Sets the arguments to the specified value.
    /// </summary>
    /// <remarks>
    /// Avoid using this overload, as it requires the arguments to be escaped manually.
    /// Formatting errors may lead to unexpected bugs and security vulnerabilities.
    /// </remarks>
    public Command WithArguments(string arguments)
    {
        _configuration = _configuration with { Arguments = arguments };
        return this;
    }

    /// <summary>
    /// Sets the arguments to the value obtained by formatting the specified enumeration.
    /// </summary>
    public Command WithArguments(IEnumerable<string> arguments, bool escape) =>
        WithArguments(args => args.Add(arguments, escape));

    /// <summary>
    /// Sets the arguments to the value obtained by formatting the specified enumeration.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public Command WithArguments(IEnumerable<string> arguments) => WithArguments(arguments, true);

    /// <summary>
    /// Sets the arguments to the value configured by the specified delegate.
    /// </summary>
    public Command WithArguments(Action<ArgumentsBuilder> configure)
    {
        var builder = new ArgumentsBuilder();
        configure(builder);

        return WithArguments(builder.Build());
    }

    /// <summary>
    /// Sets the working directory path to the specified value.
    /// </summary>
    public Command WithWorkingDirectory(string workingDirPath)
    {
        _configuration = _configuration with { WorkingDirPath = workingDirPath };
        return this;
    }

    /// <summary>
    /// Sets the resource policy to the specified value.
    /// </summary>
    public Command WithResourcePolicy(ResourcePolicy resourcePolicy)
    {
        _configuration = _configuration with { ResourcePolicy = resourcePolicy };
        return this;
    }

    /// <summary>
    /// Sets the resource policy to the value configured by the specified delegate.
    /// </summary>
    public Command WithResourcePolicy(Action<ResourcePolicyBuilder> configure)
    {
        var builder = new ResourcePolicyBuilder();
        configure(builder);

        return WithResourcePolicy(builder.Build());
    }

    /// <summary>
    /// Sets the user credentials to the specified value.
    /// </summary>
    public Command WithCredentials(Credentials credentials)
    {
        _configuration = _configuration with { Credentials = credentials };
        return this;
    }

    /// <summary>
    /// Sets the user credentials to the value configured by the specified delegate.
    /// </summary>
    public Command WithCredentials(Action<CredentialsBuilder> configure)
    {
        var builder = new CredentialsBuilder();
        configure(builder);

        return WithCredentials(builder.Build());
    }

    /// <summary>
    /// Sets the environment variables to the specified value.
    /// </summary>
    public Command WithEnvironmentVariables(
        IReadOnlyDictionary<string, string?> environmentVariables
    )
    {
        _configuration = _configuration with { EnvironmentVariables = environmentVariables };
        return this;
    }

    /// <summary>
    /// Sets the environment variables to the value configured by the specified delegate.
    /// </summary>
    public Command WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)
    {
        var builder = new EnvironmentVariablesBuilder();
        configure(builder);

        return WithEnvironmentVariables(builder.Build());
    }

    /// <summary>
    /// Sets the validation options to the specified value.
    /// </summary>
    public Command WithValidation(CommandResultValidation validation)
    {
        _configuration = _configuration with { Validation = validation };
        return this;
    }

    /// <summary>
    /// Sets the standard input pipe to the specified source.
    /// </summary>
    public Command WithStandardInputPipe(PipeSource source)
    {
        _configuration = _configuration with { StandardInputPipe = source };
        return this;
    }

    /// <summary>
    /// Sets the standard output pipe to the specified target.
    /// </summary>
    public Command WithStandardOutputPipe(PipeTarget target)
    {
        _configuration = _configuration with { StandardOutputPipe = target };
        return this;
    }

    /// <summary>
    /// Sets the standard error pipe to the specified target.
    /// </summary>
    public Command WithStandardErrorPipe(PipeTarget target)
    {
        _configuration = _configuration with { StandardErrorPipe = target };
        return this;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var configuration = _configuration; // Snapshot to avoid race conditions
        return $"{configuration.TargetFilePath} {configuration.Arguments}";
    }
}
