using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CliWrap.Tests.Dummy.Internal;

namespace CliWrap.Tests.Dummy
{
    // Shared metadata
    public static partial class Program
    {
        public static string FilePath { get; } = typeof(Program).Assembly.Location;

        public const string SetExitCode = nameof(SetExitCode);

        public const string EchoArgsToStdOut = nameof(EchoArgsToStdOut);

        public const string EchoArgsToStdErr = nameof(EchoArgsToStdErr);

        public const string EchoArgsToStdOutAndStdErr = nameof(EchoArgsToStdOutAndStdErr);

        public const string EchoFirstArgToStdOut = nameof(EchoFirstArgToStdOut);

        public const string EchoStdInToStdOut = nameof(EchoStdInToStdOut);

        public const string EchoPartStdInToStdOut = nameof(EchoPartStdInToStdOut);

        public const string PrintStdInLength = nameof(PrintStdInLength);

        public const string PrintWorkingDir = nameof(PrintWorkingDir);

        public const string PrintEnvVars = nameof(PrintEnvVars);

        public const string PrintRandomText = nameof(PrintRandomText);

        public const string PrintRandomLines = nameof(PrintRandomLines);

        public const string PrintRandomBinary = nameof(PrintRandomBinary);

        public const string Sleep = nameof(Sleep);
    }

    // Implementation
    public static partial class Program
    {
        private static readonly Random Random = new Random(1234567);

        private static readonly IReadOnlyDictionary<string, Func<string[], int>> Commands =
            new Dictionary<string, Func<string[], int>>(StringComparer.OrdinalIgnoreCase)
            {
                [SetExitCode] = args =>
                {
                    var exitCode = int.Parse(args.Single(), CultureInfo.InvariantCulture);
                    Console.Error.WriteLine($"Exit code set to {exitCode}");

                    return exitCode;
                },

                [EchoArgsToStdOut] = args =>
                {
                    var text = string.Join(" ", args);
                    Console.WriteLine(text);

                    return 0;
                },

                [EchoArgsToStdErr] = args =>
                {
                    var text = string.Join(" ", args);
                    Console.Error.WriteLine(text);

                    return 0;
                },

                [EchoArgsToStdOutAndStdErr] = args =>
                {
                    var text = string.Join(" ", args);
                    Console.WriteLine(text);
                    Console.Error.WriteLine(text);

                    return 0;
                },

                [EchoFirstArgToStdOut] = args =>
                {
                    Console.WriteLine(args.FirstOrDefault());
                    return 0;
                },

                [EchoStdInToStdOut] = args =>
                {
                    using var input = Console.OpenStandardInput();
                    using var output = Console.OpenStandardOutput();

                    input.CopyTo(output);

                    return 0;
                },

                [EchoPartStdInToStdOut] = args =>
                {
                    var size = long.Parse(args.Single());

                    using var input = Console.OpenStandardInput();
                    using var output = Console.OpenStandardOutput();

                    var buffer = new byte[81920];

                    var bytesCopied = 0L;

                    while (bytesCopied < size)
                    {
                        var bytesToRead = (int) Math.Min(buffer.Length, size - bytesCopied);

                        var bytesRead = input.Read(buffer, 0, bytesToRead);
                        if (bytesRead == 0)
                            break;

                        output.Write(buffer, 0, bytesRead);
                        bytesCopied += bytesRead;
                    }

                    return 0;
                },

                [PrintStdInLength] = args =>
                {
                    using var input = Console.OpenStandardInput();

                    var i = 0L;
                    while (input.ReadByte() >= 0)
                        i++;

                    Console.WriteLine(i.ToString(CultureInfo.InvariantCulture));

                    return 0;
                },

                [PrintWorkingDir] = args =>
                {
                    Console.WriteLine(Directory.GetCurrentDirectory());

                    return 0;
                },

                [PrintEnvVars] = args =>
                {
                    foreach (var (name, value) in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
                        Console.WriteLine($"[{name}] = {value}");

                    return 0;
                },

                [PrintRandomText] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    for (var i = 0; i < count; i++)
                    {
                        Console.WriteLine(Random.NextChar());
                        Console.Error.WriteLine(Random.NextChar());
                    }

                    return 0;
                },

                [PrintRandomLines] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    for (var i = 0; i < count; i++)
                    {
                        Console.WriteLine(Random.NextString(5000));
                        Console.Error.WriteLine(Random.NextString(5000));
                    }

                    return 0;
                },

                [PrintRandomBinary] = args =>
                {
                    var count = long.Parse(args.Single(), CultureInfo.InvariantCulture);

                    using var output = Console.OpenStandardOutput();

                    var buffer = new byte[1024];
                    var bytesRemaining = count;

                    while (bytesRemaining > 0)
                    {
                        Random.NextBytes(buffer);

                        var bytesToCopy = Math.Min((int) bytesRemaining, buffer.Length);
                        output.Write(buffer, 0, bytesToCopy);

                        bytesRemaining -= bytesToCopy;
                    }

                    return 0;
                },

                [Sleep] = args =>
                {
                    var duration = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    Thread.Sleep(duration);

                    return 0;
                }
            };

        public static int Main(string[] args)
        {
            if (args.Length <= 0)
                return 0;

            var command = args.ElementAtOrDefault(0);
            var commandArgs = args.Skip(1).ToArray();

            return Commands[command](commandArgs);
        }
    }
}