using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CliWrap.Tests.Dummy
{
    // Shared metadata
    public static partial class Program
    {
        public static string Location { get; } = typeof(Program).Assembly.Location;

        public const string Echo = nameof(Echo);

        public const string EchoStdIn = nameof(EchoStdIn);

        public const string EchoStdOut = nameof(EchoStdOut);

        public const string EchoStdErr = nameof(EchoStdErr);

        public const string SetExitCode = nameof(SetExitCode);

        public const string LoopStdOut = nameof(LoopStdOut);

        public const string LoopStdErr = nameof(LoopStdErr);

        public const string LoopBoth = nameof(LoopBoth);

        public const string Binary = nameof(Binary);

        public const string GetStdInSize = nameof(GetStdInSize);
    }

    // Implementation
    public static partial class Program
    {
        private static readonly Random Random = new Random(1234567);

        private static readonly IReadOnlyDictionary<string, Func<string[], int>> Commands =
            new Dictionary<string, Func<string[], int>>(StringComparer.OrdinalIgnoreCase)
            {
                [Echo] = args =>
                {
                    Console.WriteLine(string.Join(" ", args));
                    return 0;
                },

                [EchoStdIn] = args =>
                {
                    using var input = Console.OpenStandardInput();
                    using var output = Console.OpenStandardOutput();

                    input.CopyTo(output);

                    return 0;
                },

                [EchoStdOut] = args =>
                {
                    Console.WriteLine(string.Join(" ", args));
                    return 0;
                },

                [EchoStdErr] = args =>
                {
                    Console.Error.WriteLine(string.Join(" ", args));
                    return 0;
                },

                [SetExitCode] = args => int.Parse(args.Single(), CultureInfo.InvariantCulture),

                [LoopStdOut] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    for (var i = 0; i < count; i++)
                        Console.WriteLine(i.ToString(CultureInfo.InvariantCulture));

                    return 0;
                },

                [LoopStdErr] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    for (var i = 0; i < count; i++)
                        Console.Error.WriteLine(i.ToString(CultureInfo.InvariantCulture));

                    return 0;
                },

                [LoopBoth] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    for (var i = 0; i < count; i++)
                    {
                        Console.WriteLine(i.ToString(CultureInfo.InvariantCulture));
                        Console.Error.WriteLine(i.ToString(CultureInfo.InvariantCulture));
                    }

                    return 0;
                },

                [Binary] = args =>
                {
                    var count = int.Parse(args.Single(), CultureInfo.InvariantCulture);

                    var buffer = new byte[count];
                    Random.NextBytes(buffer);

                    Console.OpenStandardOutput().Write(buffer, 0, buffer.Length);

                    return 0;
                },

                [GetStdInSize] = args =>
                {
                    using var input = Console.OpenStandardInput();

                    var i = 0L;
                    while (input.ReadByte() >= 0)
                        i++;

                    Console.WriteLine(i.ToString(CultureInfo.InvariantCulture));

                    return 0;
                }
            };

        public static int Main(string[] args)
        {
            if (args.Length <= 0)
            {
                return -1;
            }

            var command = args.ElementAtOrDefault(0);
            var commandArgs = args.Skip(1).ToArray();

            return Commands[command](commandArgs);
        }
    }
}