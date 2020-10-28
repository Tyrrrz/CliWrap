# CliWrap.Benchmarks

All benchmarks below were ran with the following configuration:

```ini
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i5-4460 CPU 3.20GHz (Haswell), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
```

**Basic**

```ini
|           Method |      Mean |    Error |    StdDev |   Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |----------:|---------:|----------:|---------:|------:|--------:|------:|------:|------:|----------:|
| RunProcessAsTask |  77.35 ms | 4.705 ms | 13.800 ms | 70.82 ms |  0.89 |    0.17 |     - |     - |     - | 100.59 KB |
|          CliWrap |  88.03 ms | 3.077 ms |  8.780 ms | 84.71 ms |  1.00 |    0.00 |     - |     - |     - | 143.29 KB |
|          Sheller |  99.55 ms | 6.893 ms | 19.441 ms | 93.34 ms |  1.14 |    0.25 |     - |     - |     - | 166.63 KB |
|   MedallionShell | 105.96 ms | 6.678 ms | 19.480 ms | 98.77 ms |  1.21 |    0.24 |     - |     - |     - | 165.02 KB |
```

**Buffering**

```ini
|           Method |     Mean |    Error |    StdDev |   Median | Ratio | RatioSD |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------------- |---------:|---------:|----------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|
|          Sheller | 540.2 ms | 10.69 ms |  25.82 ms | 535.5 ms |  0.88 |    0.10 | 10000.0000 | 3000.0000 | 1000.0000 |   60.1 MB |
| RunProcessAsTask | 567.2 ms | 18.92 ms |  51.79 ms | 547.6 ms |  0.93 |    0.14 |  6000.0000 | 3000.0000 | 1000.0000 |  40.79 MB |
|   MedallionShell | 585.7 ms | 11.70 ms |  28.71 ms | 580.7 ms |  0.96 |    0.11 |  4000.0000 | 1000.0000 |         - |  61.59 MB |
|          CliWrap | 620.1 ms | 26.80 ms |  74.72 ms | 585.8 ms |  1.00 |    0.00 |  6000.0000 | 3000.0000 | 1000.0000 |  45.47 MB |
|         ProcessX | 666.5 ms | 40.66 ms | 117.97 ms | 612.1 ms |  1.09 |    0.24 |  6000.0000 | 3000.0000 | 1000.0000 |  41.11 MB |
```

**Async event stream**

```ini
|   Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |      Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------- |---------:|---------:|---------:|---------:|------:|--------:|-----------:|------:|------:|----------:|
|  CliWrap | 399.8 ms |  8.26 ms | 22.75 ms | 395.2 ms |  1.00 |    0.00 | 11000.0000 |     - |     - |  26.29 MB |
| ProcessX | 451.1 ms | 19.53 ms | 56.34 ms | 432.2 ms |  1.12 |    0.15 | 11000.0000 |     - |     - |  22.01 MB |
```

**Observable event stream**

```ini
|  Method |     Mean |   Error |   StdDev | Ratio |     Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------- |---------:|--------:|---------:|------:|----------:|------:|------:|----------:|
| CliWrap | 420.6 ms | 8.21 ms | 10.97 ms |  1.00 | 9000.0000 |     - |     - |  25.74 MB |
```

**Pipe from stream**

```ini
|         Method |     Mean |   Error |   StdDev |   Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|--------:|---------:|---------:|------:|--------:|------:|------:|------:|----------:|
|        CliWrap | 111.1 ms | 4.59 ms | 13.24 ms | 106.2 ms |  1.00 |    0.00 |     - |     - |     - |  148.7 KB |
| MedallionShell | 112.9 ms | 3.30 ms |  9.08 ms | 111.0 ms |  1.03 |    0.15 |     - |     - |     - | 173.13 KB |
```

**Pipe to stream**

```ini
|         Method |     Mean |   Error |  StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|--------:|--------:|------:|--------:|------:|------:|------:|----------:|
|        CliWrap | 105.4 ms | 2.10 ms | 5.57 ms |  1.00 |    0.00 |     - |     - |     - | 144.76 KB |
| MedallionShell | 112.3 ms | 2.97 ms | 8.32 ms |  1.06 |    0.10 |     - |     - |     - | 169.85 KB |
```

**Pipe to multiple streams**

```ini
|  Method |     Mean |   Error |  StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------- |---------:|--------:|--------:|------:|------:|------:|------:|----------:|
| CliWrap | 113.7 ms | 2.41 ms | 7.10 ms |  1.00 |     - |     - |     - | 150.91 KB |
```