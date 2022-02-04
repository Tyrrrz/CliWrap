# CliWrap.Benchmarks

All benchmarks below were ran with the following configuration:

```ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1466 (21H1/May2021Update)
11th Gen Intel Core i5-11600K 3.90GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.100
         [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
         DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
```

## Basic benchmarks

**Description**: run a process, wait for completion, and return the exit code.

```ini
|           Method |     Mean |    Error |   StdDev | Ratio | Allocated |
|----------------- |---------:|---------:|---------:|------:|----------:|
| RunProcessAsTask | 53.71 ms | 0.221 ms | 0.196 ms |  0.84 |    112 KB |
|          Sheller | 60.51 ms | 0.244 ms | 0.229 ms |  0.95 |    125 KB |
|   MedallionShell | 63.53 ms | 0.236 ms | 0.209 ms |  0.99 |    118 KB |
|          CliWrap | 64.03 ms | 0.399 ms | 0.373 ms |  1.00 |     93 KB |
```

## Buffering benchmarks

**Description**: run a process, read standard output and error, wait for completion, and return buffered output and error data.
Target program writes a total of 1 million characters to each stream.

```ini
|           Method |     Mean |    Error |   StdDev | Ratio |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------- |---------:|---------:|---------:|------:|----------:|---------:|---------:|----------:|
| RunProcessAsTask | 73.43 ms | 0.439 ms | 0.389 ms |  0.88 |  714.2857 | 428.5714 | 285.7143 |      4 MB |
|          Sheller | 79.70 ms | 0.231 ms | 0.216 ms |  0.96 |  857.1429 | 428.5714 | 142.8571 |      6 MB |
|         ProcessX | 83.12 ms | 0.473 ms | 0.442 ms |  1.00 |  714.2857 | 428.5714 | 285.7143 |      5 MB |
|          CliWrap | 83.20 ms | 0.382 ms | 0.339 ms |  1.00 |  571.4286 | 428.5714 | 142.8571 |      5 MB |
|   MedallionShell | 84.75 ms | 0.325 ms | 0.288 ms |  1.02 | 1000.0000 | 833.3333 | 666.6667 |      6 MB |
```

## Async event stream benchmarks

**Description**: run a process as a pull-based event stream and return the number of lines written to each stream.
Target program writes a total of 1 million characters to each stream.

```ini
|   Method |     Mean |    Error |   StdDev | Ratio |    Gen 0 | Allocated |
|--------- |---------:|---------:|---------:|------:|---------:|----------:|
| ProcessX | 83.53 ms | 0.621 ms | 0.550 ms |  0.99 | 333.3333 |      3 MB |
|  CliWrap | 84.47 ms | 1.212 ms | 1.075 ms |  1.00 | 500.0000 |      3 MB |
```

## Observable event stream benchmarks

**Description**: run a process as a push-based event stream and return the number of lines written to each stream.
Target program writes a total of 1 million characters to each stream.

```ini
|  Method |     Mean |    Error |   StdDev | Ratio |    Gen 0 | Allocated |
|-------- |---------:|---------:|---------:|------:|---------:|----------:|
| CliWrap | 81.94 ms | 0.414 ms | 0.346 ms |  1.00 | 428.5714 |      3 MB |
```

## Pipe from stream benchmarks

**Description**: run a process and pipe a stream into standard input. 

```ini
|         Method |     Mean |    Error |   StdDev | Ratio | RatioSD | Allocated |
|--------------- |---------:|---------:|---------:|------:|--------:|----------:|
|        CliWrap | 64.70 ms | 0.470 ms | 0.440 ms |  1.00 |    0.00 |     93 KB |
| MedallionShell | 64.85 ms | 0.909 ms | 0.806 ms |  1.00 |    0.02 |    168 KB |
```

## Pipe to stream benchmarks

**Description**: run a process and pipe the standard output into a memory stream.
Target program writes a total of 1 million bytes to each stream.

```ini
|         Method |     Mean |    Error |   StdDev | Ratio |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------- |---------:|---------:|---------:|------:|---------:|---------:|---------:|----------:|
|        CliWrap | 75.52 ms | 0.541 ms | 0.480 ms |  1.00 | 142.8571 | 142.8571 | 142.8571 |      2 MB |
| MedallionShell | 76.56 ms | 0.396 ms | 0.351 ms |  1.01 | 142.8571 | 142.8571 | 142.8571 |      3 MB |
```

**Pipe to multiple streams**

**Description**: run a process and pipe the standard output into two memory streams.
Target program writes a total of 1 million bytes to each stream.

```ini
|  Method |     Mean |    Error |   StdDev | Ratio |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|-------- |---------:|---------:|---------:|------:|---------:|---------:|---------:|----------:|
| CliWrap | 77.29 ms | 0.515 ms | 0.456 ms |  1.00 | 714.2857 | 571.4286 | 571.4286 |      5 MB |
```