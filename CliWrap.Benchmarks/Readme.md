# CliWrap.Benchmarks

**Basic**

```ini
|           Method |     Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |---------:|---------:|---------:|------:|--------:|------:|------:|------:|----------:|
| RunProcessAsTask | 78.86 ms | 0.563 ms | 0.527 ms |  0.80 |    0.01 |     - |     - |     - | 100.71 KB |
|          Sheller | 89.58 ms | 1.776 ms | 1.662 ms |  0.91 |    0.02 |     - |     - |     - | 166.83 KB |
|          CliWrap | 98.83 ms | 0.707 ms | 0.627 ms |  1.00 |    0.00 |     - |     - |     - | 143.72 KB |
|   MedallionShell | 99.23 ms | 0.645 ms | 0.572 ms |  1.00 |    0.01 |     - |     - |     - | 166.68 KB |
```

**Buffering**

```ini
|           Method |     Mean |   Error |   StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------------- |---------:|--------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|
| RunProcessAsTask | 415.2 ms | 6.67 ms |  5.92 ms |  0.94 |    0.03 |  6000.0000 | 3000.0000 | 1000.0000 |  40.78 MB |
|          Sheller | 432.1 ms | 8.33 ms |  8.55 ms |  0.98 |    0.03 | 10000.0000 | 3000.0000 | 1000.0000 |  60.09 MB |
|         ProcessX | 436.4 ms | 8.30 ms | 10.20 ms |  0.99 |    0.03 |  6000.0000 | 3000.0000 | 1000.0000 |   41.1 MB |
|          CliWrap | 440.2 ms | 8.78 ms | 10.78 ms |  1.00 |    0.00 |  6000.0000 | 3000.0000 | 1000.0000 |  44.83 MB |
|   MedallionShell | 473.4 ms | 9.24 ms | 10.64 ms |  1.07 |    0.04 |  4000.0000 | 1000.0000 |         - |  61.87 MB |
```

**Async event stream**

```ini
|   Method |     Mean |    Error |   StdDev | Ratio | RatioSD |      Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------- |---------:|---------:|---------:|------:|--------:|-----------:|------:|------:|----------:|
| ProcessX | 420.7 ms |  8.14 ms |  8.71 ms |  1.00 |    0.03 | 11000.0000 |     - |     - |  21.97 MB |
|  CliWrap | 420.8 ms | 10.87 ms | 10.68 ms |  1.00 |    0.00 | 11000.0000 |     - |     - |  26.29 MB |
```

**Observable event stream**

```ini
|  Method |     Mean |   Error |   StdDev | Ratio |     Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------- |---------:|--------:|---------:|------:|----------:|------:|------:|----------:|
| CliWrap | 420.6 ms | 8.21 ms | 10.97 ms |  1.00 | 9000.0000 |     - |     - |  25.74 MB |
```

**Pipe from stream**

```ini
|         Method |     Mean |   Error |  StdDev |   Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|--------:|--------:|---------:|------:|--------:|------:|------:|------:|----------:|
| MedallionShell | 109.4 ms | 2.06 ms | 2.21 ms | 109.3 ms |  0.95 |    0.06 |     - |     - |     - | 173.13 KB |
|        CliWrap | 112.3 ms | 2.36 ms | 5.14 ms | 110.1 ms |  1.00 |    0.00 |     - |     - |     - | 148.92 KB |
```

**Pipe to stream**

```ini
|         Method |     Mean |   Error |  StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|--------:|--------:|------:|--------:|------:|------:|------:|----------:|
| MedallionShell | 109.8 ms | 1.40 ms | 1.24 ms |  1.00 |    0.02 |     - |     - |     - |    170 KB |
|        CliWrap | 109.9 ms | 1.49 ms | 1.25 ms |  1.00 |    0.00 |     - |     - |     - | 144.94 KB |
```

**Pipe to multiple streams**

```ini
|  Method |     Mean |   Error |  StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------- |---------:|--------:|--------:|------:|------:|------:|------:|----------:|
| CliWrap | 112.0 ms | 2.80 ms | 2.62 ms |  1.00 |     - |     - |     - | 153.41 KB |
```