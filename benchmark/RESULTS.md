# Benchmark results

Full suite: 68 cases. Regenerate with `dotnet run -c Release --project benchmark/Benchmarks`.
See [README.md](README.md) for what each benchmark measures and how the libraries are configured.

## Environment

```
BenchmarkDotNet v0.15.8, Linux Fedora Linux 44 (COSMIC)
AMD Ryzen 5 5600H with Radeon Graphics 1.11GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 11.0.100-rc.3.26522.148
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-BJZBKM : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

IterationCount=10  LaunchCount=2  WarmupCount=10
```

Targets write to the system temp directory (tmpfs on Linux) unless `BENCHMARK_LOG_ROOT` is set, so the numbers isolate CPU and memory cost from the physical disk. The durable write benchmark is therefore only meaningful with `BENCHMARK_LOG_ROOT` on a real filesystem.

## Byte parity

1,000 messages, MessageOnly profile, measured after flush and close:

```
Parity report: 1000 messages per case, bytes measured after the provider is flushed and closed.

Format       Library           Bytes    B/msg  Preview
----------------------------------------------------------------------------------------------------
MessageOnly  Bleess           67,000     67.0  This is a test message with some parameters 100, some string, True
MessageOnly  Karambolo        67,000     67.0  This is a test message with some parameters 100, some string, True
MessageOnly  NReco            67,000     67.0  This is a test message with some parameters 100, some string, True
Realistic    Bleess          113,000    113.0  2026-11-25 05:42:22.058  fail: benchmark [0] This is a test message wi...
Realistic    Karambolo       128,000    128.0  fail: benchmark[0] @ 2026-11-24T23:42:22.0645825-06:00
Realistic    NReco           117,000    117.0  2026-11-25T05:42:22.0721857Z	FAIL	[benchmark]	[0]	This is a test messa...
Json         Bleess          376,000    376.0  {"Timestamp":"2026-11-25 05:42:22.084","EventId":0,"LogLevel":"Error",...
Json         Karambolo       360,000    360.0  {
```

Identical byte counts for the MessageOnly profile mean all three libraries did the same output work. `Realistic` and `Json` use each library's native layout, so their sizes differ.

Karambolo discards entries when its bounded queue is full, so its large-message numbers reflect doing less work (its allocation column is empty because the entry is dropped before formatting).

## Producer cost - text (MessageOnly profile)

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|------------------------|-----------------|------------|----------|-----------|--------|--------|-----------|
| Bleess_single_write    | Shape=Small     | 369.834 ns | 2.883 ns |  3.205 ns | 0.0496 | 0.0000 | 416 B     |
| NReco_single_write     | Shape=Small     | 899.653 ns | 3.269 ns |  3.634 ns | 0.0544 | 0.0267 | 458 B     |
| Karambolo_single_write | Shape=Small     |   2.004 μs | 0.016 μs |  0.018 μs | 0.1373 | 0.0076 | 1.14 KB   |
| Bleess_single_write    | Shape=Medium    |   1.273 μs | 0.022 μs |  0.024 μs | 0.5093 | 0.5054 | 4.16 KB   |
| NReco_single_write     | Shape=Medium    |   2.466 μs | 0.015 μs |  0.016 μs | 0.2747 | 0.2708 | 2.25 KB   |
| Karambolo_single_write | Shape=Medium    |   3.625 μs | 0.032 μs |  0.037 μs | 1.4000 | 0.5035 | 11.30 KB  |
| Karambolo_single_write | Shape=Large     |  10.148 μs | 2.697 μs |  2.105 μs | -      | -      | -         |
| NReco_single_write     | Shape=Large     |  26.700 μs | 0.493 μs |  0.568 μs | 5.2185 | 5.1880 | 32.27 KB  |
| Bleess_single_write    | Shape=Large     |  28.112 μs | 0.860 μs |  0.956 μs | 9.0942 | 5.1575 | 64.24 KB  |
| Bleess_single_write    | Shape=Exception | 363.779 ns | 1.145 ns |  1.176 ns | 0.0496 | 0.0005 | 416 B     |
| NReco_single_write     | Shape=Exception | 887.176 ns | 9.949 ns | 11.059 ns | 0.0544 | 0.0267 | 459 B     |
| Karambolo_single_write | Shape=Exception |   1.988 μs | 0.013 μs |  0.014 μs | 0.1373 | 0.0038 | 1.14 KB   |

## Producer cost - JSON

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|-----------------------------|-----------------|-----------|----------|----------|---------|---------|-----------|
| Bleess_single_write_json    | Shape=Small     |  2.995 μs | 0.012 μs | 0.013 μs | 0.1793  | 0.0000  | 1.47 KB   |
| Karambolo_single_write_json | Shape=Small     |  3.148 μs | 0.024 μs | 0.026 μs | 0.2670  | 0.0076  | 2.20 KB   |
| Bleess_single_write_json    | Shape=Medium    |  4.229 μs | 0.056 μs | 0.064 μs | 1.3199  | 0.4349  | 10.83 KB  |
| Karambolo_single_write_json | Shape=Medium    |  7.023 μs | 0.211 μs | 0.197 μs | -       | -       | -         |
| Karambolo_single_write_json | Shape=Large     | 42.061 μs | 1.624 μs | 1.870 μs | 37.1094 | 14.5264 | 289.38 KB |
| Bleess_single_write_json    | Shape=Large     | 55.854 μs | 1.729 μs | 1.922 μs | 16.2964 | 8.7891  | 160.97 KB |
| Bleess_single_write_json    | Shape=Exception |  6.150 μs | 0.074 μs | 0.082 μs | 0.6714  | 0.0000  | 5.58 KB   |
| Karambolo_single_write_json | Shape=Exception |  6.689 μs | 0.148 μs | 0.158 μs | 0.7019  | 0.0000  | 5.88 KB   |

## End to end - 500 messages, OS buffered flush

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|----------------------|-----------------|------------|-----------|-----------|------------|-----------|-----------|
| Bleess_end_to_end    | Shape=Small     | 547.378 μs | 24.867 μs | 26.607 μs | 0.0000     | 0.0000    | 211.75 KB |
| NReco_end_to_end     | Shape=Small     | 686.478 μs | 55.032 μs | 63.375 μs | 0.0000     | 0.0000    | 148.62 KB |
| Karambolo_end_to_end | Shape=Small     |   2.071 ms |  0.084 ms |  0.097 ms | 0.0000     | 0.0000    | 578.30 KB |
| NReco_end_to_end     | Shape=Medium    |   1.359 ms |  0.075 ms |  0.086 ms | 0.0000     | 0.0000    | 1.03 MB   |
| Bleess_end_to_end    | Shape=Medium    |   1.745 ms |  0.066 ms |  0.073 ms | 0.0000     | 0.0000    | 2.01 MB   |
| Karambolo_end_to_end | Shape=Medium    |   3.309 ms |  0.100 ms |  0.116 ms | 0.0000     | 0.0000    | 5.60 MB   |
| NReco_end_to_end     | Shape=Large     |   9.771 ms |  0.219 ms |  0.252 ms | 1000.0000  | 0.0000    | 15.68 MB  |
| Bleess_end_to_end    | Shape=Large     |  10.141 ms |  0.817 ms |  0.874 ms | 3000.0000  | 2000.0000 | 31.32 MB  |
| Karambolo_end_to_end | Shape=Large     |  17.687 ms |  1.036 ms |  1.152 ms | 10000.0000 | 5000.0000 | 73.95 MB  |
| Bleess_end_to_end    | Shape=Exception | 531.742 μs | 21.815 μs | 24.247 μs | 0.0000     | 0.0000    | 211.75 KB |
| NReco_end_to_end     | Shape=Exception | 694.828 μs | 40.434 μs | 46.564 μs | 0.0000     | 0.0000    | 140.37 KB |
| Karambolo_end_to_end | Shape=Exception |   2.136 ms |  0.120 ms |  0.138 ms | 0.0000     | 0.0000    | 578.30 KB |

## Rolling - 5,000 messages, 64 KB files, 4 files retained

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|-------------------|------------|-----------|----------|----------|--------|--------|-----------|
| Bleess_rolling    |            |  4.426 ms | 0.318 ms | 0.366 ms | 0.0000 | 0.0000 | 2.07 MB   |
| NReco_rolling     |            |  5.310 ms | 0.110 ms | 0.113 ms | 0.0000 | 0.0000 | 1.92 MB   |
| Karambolo_rolling |            | 18.762 ms | 1.667 ms | 1.919 ms | 0.0000 | 0.0000 | 4.65 MB   |

## Durable writes - Bleess only, 200 messages

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|----------------------|-------------------|------------|----------|-----------|--------|--------|-----------|
| Bleess_flush_to_disk | FlushToDisk=False | 267.228 μs | 9.430 μs | 10.090 μs | 0.0000 | 0.0000 | 89.88 KB  |
| Bleess_flush_to_disk | FlushToDisk=True  | 267.477 μs | 8.045 μs |  8.608 μs | 0.0000 | 0.0000 | 90.80 KB  |

## Contention - 2,000 messages per producer, MessageOnly

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|----------------------|-------------------------|------------|-----------|-----------|-------------|------------|-------------|
| Bleess_contention    | Producers=1&Shape=Small | 925.853 μs | 13.120 μs | 14.583 μs | 99.6094     | 0.9766     | 813.74 KB   |
| NReco_contention     | Producers=1&Shape=Small |   1.837 ms |  0.019 ms |  0.021 ms | 101.5625    | 48.8281    | 836.33 KB   |
| Karambolo_contention | Producers=1&Shape=Small |   4.271 ms |  0.015 ms |  0.016 ms | 281.2500    | 0.0000     | 2.23 MB     |
| Karambolo_contention | Producers=1&Shape=Large |  50.530 ms | 11.943 ms |  9.325 ms | -           | -          | -           |
| NReco_contention     | Producers=1&Shape=Large |  54.948 ms |  1.209 ms |  1.392 ms | 10444.4444  | 10333.3333 | 63.07 MB    |
| Bleess_contention    | Producers=1&Shape=Large |  55.525 ms |  1.035 ms |  1.192 ms | 18111.1111  | 10222.2222 | 125.59 MB   |
| Bleess_contention    | Producers=4&Shape=Small |   4.300 ms |  0.100 ms |  0.115 ms | 437.5000    | 171.8750   | 3.46 MB     |
| Karambolo_contention | Producers=4&Shape=Small |   5.201 ms |  0.096 ms |  0.106 ms | 625.0000    | 0.0000     | 4.95 MB     |
| NReco_contention     | Producers=4&Shape=Small |  10.729 ms |  0.139 ms |  0.161 ms | 421.8750    | 203.1250   | 3.36 MB     |
| Karambolo_contention | Producers=4&Shape=Large | 100.252 ms |  2.342 ms |  2.697 ms | 172000.0000 | 14833.3333 | 1,176.86 MB |
| NReco_contention     | Producers=4&Shape=Large | 234.357 ms |  3.265 ms |  3.760 ms | 43000.0000  | 42666.6667 | 252.55 MB   |
| Bleess_contention    | Producers=4&Shape=Large | 240.589 ms |  7.429 ms |  8.555 ms | 74000.0000  | 41500.0000 | 502.91 MB   |
| Karambolo_contention | Producers=8&Shape=Small |   6.727 ms |  0.074 ms |  0.085 ms | 1046.8750   | 0.0000     | 8.25 MB     |
| Bleess_contention    | Producers=8&Shape=Small |  12.460 ms |  0.146 ms |  0.168 ms | 921.8750    | 406.2500   | 7.25 MB     |
| NReco_contention     | Producers=8&Shape=Small |  21.874 ms |  0.285 ms |  0.328 ms | 843.7500    | 406.2500   | 6.70 MB     |
| Karambolo_contention | Producers=8&Shape=Large | 178.412 ms |  4.077 ms |  4.363 ms | 342666.6667 | 29000.0000 | 2,352.07 MB |
| NReco_contention     | Producers=8&Shape=Large | 463.875 ms | 12.778 ms | 14.715 ms | 85000.0000  | 84000.0000 | 505.09 MB   |
| Bleess_contention    | Producers=8&Shape=Large | 487.670 ms | 12.891 ms | 14.846 ms | 147000.0000 | 83000.0000 | 1,006.13 MB |

## Multi-file - one event per call, MessageOnly

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|----------------------------------|-------------|------------|-----------|-----------|--------|--------|-----------|
| Bleess_multifile_single_write    | FileCount=1 | 361.754 ns | 13.698 ns | 15.775 ns | 0.0496 | 0.0005 | 416 B     |
| NReco_multifile_single_write     | FileCount=1 | 885.882 ns | 15.451 ns | 17.174 ns | 0.0544 | 0.0267 | 458 B     |
| Karambolo_multifile_single_write | FileCount=1 |   1.977 μs |  0.009 μs |  0.010 μs | 0.1373 | 0.0076 | 1.13 KB   |
| Bleess_multifile_single_write    | FileCount=2 | 802.084 ns |  3.256 ns |  3.484 ns | 0.0877 | 0.0000 | 736 B     |
| NReco_multifile_single_write     | FileCount=2 | 981.460 ns | 19.856 ns | 22.070 ns | 0.0687 | 0.0343 | 581 B     |
| Karambolo_multifile_single_write | FileCount=2 |   2.229 μs |  0.059 μs |  0.066 μs | 0.2670 | 0.0305 | 2.18 KB   |
| NReco_multifile_single_write     | FileCount=4 |   1.324 μs |  0.016 μs |  0.018 μs | 0.0877 | 0.0057 | 736 B     |
| Bleess_multifile_single_write    | FileCount=4 |   1.922 μs |  0.111 μs |  0.119 μs | 0.1640 | 0.0019 | 1.34 KB   |
| Karambolo_multifile_single_write | FileCount=4 |   3.398 μs |  0.295 μs |  0.340 μs | 0.5722 | 0.0000 | 4.65 KB   |

## Startup - provider creation with existing files

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|-------------------------|--------------------|------------|----------|----------|---------|---------|-----------|
| Bleess_provider_startup | ExistingFiles=0    | 343.078 μs | 1.995 μs | 2.298 μs | 10.7422 | 1.9531  | 83.21 KB  |
| Bleess_provider_startup | ExistingFiles=100  | 555.919 μs | 4.780 μs | 5.115 μs | 15.6250 | 1.9531  | 121.16 KB |
| Bleess_provider_startup | ExistingFiles=1000 |   2.295 ms | 0.018 ms | 0.019 ms | 54.6875 | 27.3438 | 452.42 KB |

## No-op baseline

| Method | Parameters | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|-------------|------------|-----------|----------|----------|--------|--------|-----------|
| Null_logger |            | 37.062 ns | 0.436 ns | 0.502 ns | 0.0114 | 0.0000 | 96 B      |
