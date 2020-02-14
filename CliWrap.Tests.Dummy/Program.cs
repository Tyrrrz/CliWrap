using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CliWrap.Tests.Dummy
{
    public static class Program
    {
        private static readonly Random Random = new Random(1234567);

        public static string FilePath { get; } = typeof(Program).Assembly.Location;

        public const string Echo = "echo";

        public const string EchoStdIn = "echo-stdin";

        public const string EchoStdOut = "echo-stdout";

        public const string EchoStdErr = "echo-stderr";

        public const string SetExitCode = "set-exit-code";

        public const string LoopStdOut = "loop-stdout";

        public const string LoopStdErr = "loop-stderr";

        public const string LoopBoth = "loop-both";

        public const string Binary = "binary";

        public const string GetSize = "get-size";

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

                [GetSize] = args =>
                {
                    using var input = Console.OpenStandardInput();

                    var i = 0L;
                    while (input.ReadByte() >= 0)
                        i++;

                    Console.WriteLine(i.ToString(CultureInfo.InvariantCulture));

                    return 0;
                }
            };

        public static int Main(string[] args) => Commands[args.First()](args.Skip(1).ToArray());
    }
}