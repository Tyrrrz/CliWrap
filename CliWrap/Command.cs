using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using CliWrap.Builders;

namespace CliWrap;

/// <summary>
/// Instructions for running a process.
/// </summary>
public partial class Command : ICommandConfiguration
{
    /// <inheritdoc />
    public string TargetFilePath { get; }

    /// <inheritdoc />
    public string Arguments { get; }

    /// <inheritdoc>
    public bool ShellExecute { get; }

    /// <inheritdoc />
    public string WorkingDirPath { get; }

    /// <inheritdoc />
    public Credentials Credentials { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <inheritdoc />
    public CommandResultValidation Validation { get; }

    /// <inheritdoc />
    public PipeSource StandardInputPipe { get; }

    /// <inheritdoc />
    public PipeTarget StandardOutputPipe { get; }

    /// <inheritdoc />
    public PipeTarget StandardErrorPipe { get; }

    /// <summary>
    /// Initializes an instance of <see cref="Command" />.
    /// </summary>
    public Command(
        string targetFilePath,
        string arguments,
        bool shellExecute,
        string workingDirPath,
        Credentials credentials,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CommandResultValidation validation,
        PipeSource standardInputPipe,
        PipeTarget standardOutputPipe,
        PipeTarget standardErrorPipe)
    {
        TargetFilePath = targetFilePath;
        Arguments = arguments;
        ShellExecute = shellExecute;
        WorkingDirPath = workingDirPath;
        Credentials = credentials;
        EnvironmentVariables = environmentVariables;
        Validation = validation;
        StandardInputPipe = standardInputPipe;
        StandardOutputPipe = standardOutputPipe;
        StandardErrorPipe = standardErrorPipe;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Command" />.
    /// </summary>
    public Command(string targetFilePath) : this(
        targetFilePath,
        string.Empty,
        false,
        Directory.GetCurrentDirectory(),
        Credentials.Default,
        new Dictionary<string, string?>(),
        CommandResultValidation.ZeroExitCode,
        PipeSource.Null,
        PipeTarget.Null,
        PipeTarget.Null)
    {
    }

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the specified value.
    /// </summary>
    [Pure]
    public Command WithArguments(string arguments) => new(
        TargetFilePath,
        arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value
    /// obtained by formatting the specified enumeration.
    /// </summary>
    [Pure]
    public Command WithArguments(IEnumerable<string> arguments, bool escape) =>
        WithArguments(args => args.Add(arguments, escape));

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value
    /// obtained by formatting the specified enumeration.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    [Pure]
    public Command WithArguments(IEnumerable<string> arguments) =>
        WithArguments(arguments, true);

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value
    /// configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithArguments(Action<ArgumentsBuilder> configure)
    {
        var builder = new ArgumentsBuilder();
        configure(builder);

        return WithArguments(builder.Build());
    }

    [Pure]
    public Command WithShellExecute() => new(
         TargetFilePath,
        Arguments,
        true,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
        );

    /// <summary>
    /// Creates a copy of this command, setting the working directory path to the specified value.
    /// </summary>
    [Pure]
    public Command WithWorkingDirectory(string workingDirPath) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        workingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the user credentials to the specified value.
    /// </summary>
    [Pure]
    public Command WithCredentials(Credentials credentials) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the user credentials to the value
    /// configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithCredentials(Action<CredentialsBuilder> configure)
    {
        var builder = new CredentialsBuilder();
        configure(builder);

        return WithCredentials(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the specified value.
    /// </summary>
    [Pure]
    public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string?> environmentVariables) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        environmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the value
    /// configured by the specified delegate.
    /// </summary>
    [Pure]
    public Command WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)
    {
        var builder = new EnvironmentVariablesBuilder();
        configure(builder);

        return WithEnvironmentVariables(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the validation options to the specified value.
    /// </summary>
    [Pure]
    public Command WithValidation(CommandResultValidation validation) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        validation,
        StandardInputPipe,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard input pipe to the specified source.
    /// </summary>
    [Pure]
    public Command WithStandardInputPipe(PipeSource source) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        source,
        StandardOutputPipe,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard output pipe to the specified target.
    /// </summary>
    [Pure]
    public Command WithStandardOutputPipe(PipeTarget target) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        target,
        StandardErrorPipe
    );

    /// <summary>
    /// Creates a copy of this command, setting the standard error pipe to the specified target.
    /// </summary>
    [Pure]
    public Command WithStandardErrorPipe(PipeTarget target) => new(
        TargetFilePath,
        Arguments,
        ShellExecute,
        WorkingDirPath,
        Credentials,
        EnvironmentVariables,
        Validation,
        StandardInputPipe,
        StandardOutputPipe,
        target
    );

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"{TargetFilePath} {Arguments}";
}