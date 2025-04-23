using System.Collections.Generic;
using System.IO;

namespace CliWrap;

record struct CommandConfiguration(
    string TargetFilePath,
    string Arguments,
    string WorkingDirPath,
    ResourcePolicy ResourcePolicy,
    Credentials Credentials,
    IReadOnlyDictionary<string, string?> EnvironmentVariables,
    CommandResultValidation Validation,
    PipeSource StandardInputPipe,
    PipeTarget StandardOutputPipe,
    PipeTarget StandardErrorPipe
) : ICommandConfiguration
{
    public CommandConfiguration(string targetFilePath)
        : this(
            targetFilePath,
            string.Empty,
            Directory.GetCurrentDirectory(),
            ResourcePolicy.Default,
            Credentials.Default,
            new Dictionary<string, string?>(),
            CommandResultValidation.ZeroExitCode,
            PipeSource.Null,
            PipeTarget.Null,
            PipeTarget.Null
        ) { }
}
