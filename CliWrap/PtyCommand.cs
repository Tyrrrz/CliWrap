using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using CliWrap.Builders;

namespace CliWrap;

/// <summary>
/// Instructions for running a process under a pseudo-terminal (PTY).
/// </summary>
/// <remarks>
/// <para>
/// Pseudo-terminal mode makes CLI applications behave as if running in an interactive terminal,
/// enabling colored output, progress indicators, and other TTY-dependent features.
/// </para>
/// <para>
/// Platform support:
/// <list type="bullet">
///   <item>Windows 10 version 1809 (build 17763) or later via ConPTY</item>
///   <item>Linux via openpty/posix_spawn with full PTY I/O</item>
///   <item>macOS via openpty/posix_spawn with full PTY I/O</item>
/// </list>
/// </para>
/// <para>
/// stderr is merged into stdout on all platforms. Configuration that does not apply to PTY
/// execution (resource policy, credentials, separate stderr pipe) is intentionally not exposed.
/// </para>
/// </remarks>
public partial class PtyCommand(
    string targetFilePath,
    string arguments,
    string workingDirPath,
    IReadOnlyDictionary<string, string?> environmentVariables,
    CommandResultValidation validation,
    PipeSource standardInputPipe,
    PipeTarget standardOutputPipe,
    PipeTarget standardErrorPipe,
    int columns,
    int rows
) : ICommand
{
    /// <summary>
    /// Initializes an instance of <see cref="PtyCommand" />.
    /// </summary>
    public PtyCommand(string targetFilePath)
        : this(
            targetFilePath,
            string.Empty,
            Directory.GetCurrentDirectory(),
            new Dictionary<string, string?>(),
            CommandResultValidation.ZeroExitCode,
            PipeSource.Null,
            PipeTarget.Null,
            PipeTarget.Null,
            80,
            24
        ) { }

    /// <inheritdoc />
    public string TargetFilePath { get; } = targetFilePath;

    /// <inheritdoc />
    public string Arguments { get; } = arguments;

    /// <inheritdoc />
    public string WorkingDirPath { get; } = workingDirPath;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; } =
        environmentVariables;

    /// <inheritdoc />
    public CommandResultValidation Validation { get; } = validation;

    /// <inheritdoc />
    public PipeSource StandardInputPipe { get; } = standardInputPipe;

    /// <inheritdoc />
    public PipeTarget StandardOutputPipe { get; } = standardOutputPipe;

    // Hidden from the concrete API but available through the interface.
    PipeTarget ICommand.StandardErrorPipe => _standardErrorPipe;

    private readonly PipeTarget _standardErrorPipe = standardErrorPipe;

    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    public int Columns { get; } =
        columns > 0
            ? columns
            : throw new ArgumentOutOfRangeException(
                nameof(columns),
                columns,
                "Terminal columns must be positive."
            );

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    public int Rows { get; } =
        rows > 0
            ? rows
            : throw new ArgumentOutOfRangeException(
                nameof(rows),
                rows,
                "Terminal rows must be positive."
            );

    /// <summary>
    /// Creates a copy of this command, setting the target file path to the specified value.
    /// </summary>
    [Pure]
    public PtyCommand WithTargetFile(string targetFilePath) =>
        new(
            targetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the specified value.
    /// </summary>
    /// <remarks>
    /// Avoid using this overload, as it requires the arguments to be escaped manually.
    /// Formatting errors may lead to unexpected bugs and security vulnerabilities.
    /// </remarks>
    [Pure]
    public PtyCommand WithArguments(string arguments) =>
        new(
            TargetFilePath,
            arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value
    /// obtained by formatting the specified enumeration.
    /// </summary>
    [Pure]
    public PtyCommand WithArguments(IEnumerable<string> arguments, bool escape = true) =>
        WithArguments(args => args.Add(arguments, escape));

    /// <summary>
    /// Creates a copy of this command, setting the arguments to the value
    /// configured by the specified delegate.
    /// </summary>
    [Pure]
    public PtyCommand WithArguments(Action<ArgumentsBuilder> configure)
    {
        var builder = new ArgumentsBuilder();
        configure(builder);

        return WithArguments(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the working directory path to the specified value.
    /// </summary>
    [Pure]
    public PtyCommand WithWorkingDirectory(string workingDirPath) =>
        new(
            TargetFilePath,
            Arguments,
            workingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the specified value.
    /// </summary>
    [Pure]
    public PtyCommand WithEnvironmentVariables(
        IReadOnlyDictionary<string, string?> environmentVariables
    ) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            environmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the environment variables to the value
    /// configured by the specified delegate.
    /// </summary>
    [Pure]
    public PtyCommand WithEnvironmentVariables(Action<EnvironmentVariablesBuilder> configure)
    {
        var builder = new EnvironmentVariablesBuilder();
        configure(builder);

        return WithEnvironmentVariables(builder.Build());
    }

    /// <summary>
    /// Creates a copy of this command, setting the validation options to the specified value.
    /// </summary>
    [Pure]
    public PtyCommand WithValidation(CommandResultValidation validation) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the standard input pipe to the specified source.
    /// </summary>
    [Pure]
    public PtyCommand WithStandardInputPipe(PipeSource source) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            source,
            StandardOutputPipe,
            _standardErrorPipe,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the standard output pipe to the specified target.
    /// </summary>
    [Pure]
    public PtyCommand WithStandardOutputPipe(PipeTarget target) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            target,
            _standardErrorPipe,
            Columns,
            Rows
        );

    [Pure]
    private PtyCommand WithStandardErrorPipe(PipeTarget target) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            target,
            Columns,
            Rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the terminal dimensions.
    /// </summary>
    [Pure]
    public PtyCommand WithSize(int columns, int rows) =>
        new(
            TargetFilePath,
            Arguments,
            WorkingDirPath,
            EnvironmentVariables,
            Validation,
            StandardInputPipe,
            StandardOutputPipe,
            _standardErrorPipe,
            columns,
            rows
        );

    /// <summary>
    /// Creates a copy of this command, setting the terminal width in columns.
    /// </summary>
    [Pure]
    public PtyCommand WithColumns(int columns) => WithSize(columns, Rows);

    /// <summary>
    /// Creates a copy of this command, setting the terminal height in rows.
    /// </summary>
    [Pure]
    public PtyCommand WithRows(int rows) => WithSize(Columns, rows);

    ICommand ICommand.WithStandardOutputPipe(PipeTarget target) => WithStandardOutputPipe(target);

    ICommand ICommand.WithStandardErrorPipe(PipeTarget target) => WithStandardErrorPipe(target);

    CommandTask<CommandResult> ICommand.ExecuteAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(cancellationToken);

    CommandTask<CommandResult> ICommand.ExecuteAsync(
        CancellationToken forcefulCancellationToken,
        CancellationToken gracefulCancellationToken
    ) => ExecuteAsync(forcefulCancellationToken, gracefulCancellationToken);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"{TargetFilePath} {Arguments}";
}
