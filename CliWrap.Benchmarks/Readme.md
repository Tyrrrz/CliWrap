# CliWrap.Benchmarks

All benchmarks below were ran with the following configuration:

```ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19041.1165 (2004/May2020Update/20H1)
Intel Core i5-4460 CPU 3.20GHz (Haswell), 1 CPU, 4 logical and 4 physical cores
.NET SDK=5.0.301
         [Host]     : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
         DefaultJob : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
```

**Basic**

```ini
|           Method |     Mean |   Error |  StdDev | Ratio | Allocated |
|----------------- |---------:|--------:|--------:|------:|----------:|
| RunProcessAsTask | 129.0 ms | 1.33 ms | 1.18 ms |  0.89 |    112 KB |
|          Sheller | 140.2 ms | 1.39 ms | 1.30 ms |  0.96 |    181 KB |
|          CliWrap | 145.5 ms | 1.12 ms | 0.94 ms |  1.00 |    152 KB |
|   MedallionShell | 146.0 ms | 1.53 ms | 1.43 ms |  1.00 |    174 KB |
```

**Buffering**

```ini
|           Method |     Mean |   Error |   StdDev |   Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------- |---------:|--------:|---------:|---------:|------:|--------:|----------:|---------:|---------:|----------:|
| RunProcessAsTask | 153.6 ms | 1.41 ms |  1.32 ms | 153.2 ms |  0.89 |    0.01 |  500.0000 | 250.0000 |        - |      4 MB |
|          Sheller | 164.7 ms | 1.45 ms |  1.28 ms | 164.6 ms |  0.95 |    0.01 | 1000.0000 | 333.3333 |        - |      6 MB |
|          CliWrap | 172.5 ms | 1.57 ms |  1.55 ms | 172.5 ms |  1.00 |    0.00 |  666.6667 | 333.3333 |        - |      5 MB |
|   MedallionShell | 176.0 ms | 3.31 ms |  3.25 ms | 176.0 ms |  1.02 |    0.02 | 1000.0000 | 666.6667 | 333.3333 |      6 MB |
|         ProcessX | 190.2 ms | 5.05 ms | 14.41 ms | 185.7 ms |  1.05 |    0.05 |  333.3333 |        - |        - |      4 MB |
```

**Async event stream**

```ini
|   Method |     Mean |   Error |  StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 | Allocated |
|--------- |---------:|--------:|--------:|------:|--------:|----------:|---------:|----------:|
| ProcessX | 179.6 ms | 2.22 ms | 1.97 ms |  0.97 |    0.02 |  666.6667 |        - |      2 MB |
|  CliWrap | 185.6 ms | 3.59 ms | 4.40 ms |  1.00 |    0.00 | 1000.0000 | 333.3333 |      3 MB |
```

**Observable event stream**

```ini
|  Method |     Mean |   Error |  StdDev | Ratio |     Gen 0 |    Gen 1 | Allocated |
|-------- |---------:|--------:|--------:|------:|----------:|---------:|----------:|
| CliWrap | 180.1 ms | 2.04 ms | 1.91 ms |  1.00 | 1000.0000 | 333.3333 |      3 MB |
```

**Pipe from stream**

```ini
|         Method |     Mean |   Error |  StdDev |   Median | Ratio | RatioSD | Allocated |
|--------------- |---------:|--------:|--------:|---------:|------:|--------:|----------:|
| MedallionShell | 151.7 ms | 2.76 ms | 2.16 ms | 151.3 ms |  0.93 |    0.04 |    172 KB |
|        CliWrap | 159.7 ms | 3.05 ms | 7.48 ms | 157.0 ms |  1.00 |    0.00 |    148 KB |
```

**Pipe to stream**

```ini
|         Method |     Mean |   Error |  StdDev | Ratio | RatioSD | Allocated |
|--------------- |---------:|--------:|--------:|------:|--------:|----------:|
|        CliWrap | 166.0 ms | 2.00 ms | 1.56 ms |  1.00 |    0.00 |      2 MB |
| MedallionShell | 176.3 ms | 3.40 ms | 6.80 ms |  1.05 |    0.03 |      3 MB |
```

**Pipe to multiple streams**

```ini
|  Method |     Mean |   Error |  StdDev | Ratio | Allocated |
|-------- |---------:|--------:|--------:|------:|----------:|
| CliWrap | 167.9 ms | 2.21 ms | 1.96 ms |  1.00 |      5 MB |
```