# Bleess.Extensions.Logging.File
High performance rolling file logger for Microsoft.Extensions.Logging with no other 3rd party dependencies. Modeled after the standard console logger.

- Rolling files, max file size, max number of files, rolling by time period
- Text or Json output as well as custom formatters
- Standard idiomatic configuration (similar to other MS logging providers) using IConfiguration or configuration callbacks
- Abitity to update settings such as log level, filter rules, or log file path while application is running
- Logging scopes and activity tracking support
- High performance using dedicated write thread
- Ability to specify multiple log files with independent settings

## Usage

Add the nuget package Bleess.Extensions.Logging.File

The log provider is configured just like any other Microsoft.Extensions.Logging providers.  There are extensions methods on the ILogBuilder to add the provider.

When using Host.CreateDefaultBuilder you only need to call `AddFile()`, and the logger will be configured using configuration providers.  There are also other overloads to configure the logger using options callbacks etc.
 
 ```csharp
 logBuilder.AddFile();
 ```
 
## Configuration

Below is a sample configuration for the file provider.  The values shown are the defaults.

```json
{
  "Logging": {

    "File": {    
      "IncludeScopes": true,   // this is can also be set in the formatter options
      "Path": "logs/log.txt",  // can contain environment variables
      "RollInteral" : "Infinite", // can be (Year, Month, Day, Hour) appends _yyyyMMddHH to the file name
      "MaxNumberFiles": 7,
      "MaxFileSizeInMB": 50,  // this can be decimal
      "FormatterName": "simple",  // simple or json
      "Append": true,
      "FlushToDisk" : false, // true to indicate that flush operations should flush all the way to disk (ie fsync)
      "MaxFlushInterval" : null, // max ms for which to allow before a flush operation on log writes
      "formatterOptions" : { 
          // see formatter options below 
      }
      
      "logLevel": {
        "default": "Information"
      }
    },

    "logLevel": {
      "default": "Information"
    }
  }
}

```

## Formatting
There are two built in formatters.  Simple text and Json.  The formatters have a few limited options.

```json
// simple text
"FormatterOptions": {
   "IncludeScopes" : false,
   "SingleLine" : false,
   "EmptyLineBetweenMessages" : true,
   "TimestampFormat" : "yyyy-MM-dd h:mm tt",
   "InvariantTimestampFormat" : false
   "UseUtcTimestamp" : false
}

// json formatter
"FormatterOptions" : {
   "IncludeScopes" : false,
   "EmptyLineBetweenMessages" : true,
   "TimestampFormat" : "yyyy-MM-dd h:mm tt",
   "InvariantTimestampFormat" : false
   "UseUtcTimestamp" : false,
   "JsonWriterOptions" : {
      // see https"://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonwriteroptions?view=netcore-3.1
   }
}
```

Custom formatters can be added using extensions method `.AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder, Action<TOptions> configure)`.  The Formatter name of the log provider will need to be set in order to use the formatter.

## Multiple log files

Multiple log files are supported.  Example below:
```csharp
l.AddFiles(b =>
{
    // example adding log provider, will use settings in configuration
    b.AddFile("TraceLog");

    // example configuration certain properties in code, which would override config settings
    b.AddFile("ErrorLog")
        .WithOptions(o =>
        {
            o.Path = "logs/errors.json";
        })
        .WithJsonFormatter(o =>
        {
            o.IncludeScopes = true;
            o.EmptyLineBetweenMessages = false;
            o.TimestampFormat = "dd h:mm tt";
        })
        .WithMinLevel(LogLevel.Error);
});

```

Example configuration
```json
 "Logging": {

    "Files": {

      "TraceLog": {
        "Path": "logs/trace.txt",
        "FormatterOptions": {
          "IncludeScopes": false,
          "SingleLine": true,
          "EmptyLineBetweenMessages": false,
          "TimestampFormat": "yyyy-MM-dd h:mm tt",
          "UseUtcTimestamp": true
        },
        "logLevel": {
          "Default": "Trace",
          "Microsoft": "Warning"
        }
      },


      "ErrorLog": {} // defined in code 

    },
```

## Rolling Behavior
 Log files can have a max file size at which time a new file will be create with a incremented id appended.  You may also specify a maximum number of files to retain.  Once the maximum number of files has been reached, the oldest will be overwritten.
 Using RollInterval setting, you can also specify that a date will be appended to the file name and the files will roll according to the date in 'yyyyMMddHH' format.

## Custom Formatters

 To implment a custom formatter, create a class that derives from ```FileFormatter<TOptions>```.  Then register the formatter using a name and specify that name on the Logger options

## Flush behavior
 
 For performance reasons, this library uses a message queue and a dedicated thread for writing log messages to the file.  By default, a flush() is called when the write thread empties the queue of log messages.  In situations where there is a high throughput of log messages, file buffers may not flush for quite some time as the queue is not cleared.  You can optionally specify a maximum amount of time in (MaxFlushInterval) ms to go without a file flush operation. A value of 0 would force a flush on every messsage write. Note that even a flush() operation may not actually ensure that the data is written to the physical disk. By default, the flush ensures that the data is written to the drive cache.  If you wish to ensure that a flush will ensure the data is written to disk set "FlushToDisk" to true.  This could have some minor performance implications.

## Benchmarks

 The library has been benchmarked against a couple of other popular file loggers for .NET core.  Specically NReco and Karambolo loggers. See the results below.

### Simple Text Message
 ```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  Job-BNUXKU : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10
```
| Method                 | Mean            | Error           | StdDev          | Median          | Gen0     | Gen1   | Allocated |
|----------------------- |----------------:|----------------:|----------------:|----------------:|---------:|-------:|----------:|
| Bleess_single_write    |        594.1 ns |        19.90 ns |        22.11 ns |        588.8 ns |   0.0877 | 0.0191 |     744 B |
| Karambolo_single_write |        541.3 ns |        14.60 ns |        16.23 ns |        539.3 ns |   0.0839 |      - |     707 B |
| NReco_single_write     |      2,199.3 ns |        59.97 ns |        66.66 ns |      2,171.6 ns |   0.0839 | 0.0019 |     718 B |
| Bleess_10000_writes    |  7,285,301.5 ns | 1,745,837.96 ns | 1,868,027.06 ns |  5,723,939.1 ns | 875.0000 |      - | 7440023 B |
| Karambolo_10000_write  |  8,466,456.8 ns |   144,135.60 ns |   165,986.77 ns |  8,468,877.3 ns | 843.7500 |      - | 7067165 B |
| NReco_10000_write      | 20,273,803.3 ns | 2,614,162.62 ns | 3,010,473.56 ns | 21,856,078.1 ns | 843.7500 |      - | 7165933 B |


### Json file
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  Job-BNUXKU : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10
```


| Method                      | Mean     | Error     | StdDev    | Gen0   | Gen1   | Gen2   | Allocated |
|---------------------------- |---------:|----------:|----------:|-------:|-------:|-------:|----------:|
| Bleess_single_write_json    | 1.950 us | 0.0633 us | 0.0704 us | 0.9766 | 0.0305 |      - |   7.99 KB |
| Karambolo_single_write_json | 1.717 us | 0.0329 us | 0.0352 us | 0.3204 | 0.0381 | 0.0229 |   2.64 KB |


### Multi-File
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  Job-BNUXKU : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10
```

| Method                           | Mean     | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------------------------- |---------:|----------:|----------:|-------:|-------:|----------:|
| Bleess_multifile_single_write    | 1.249 us | 0.0267 us | 0.0286 us | 0.1602 |      - |   1.36 KB |
| Karambolo_multifile_single_write | 1.281 us | 0.1082 us | 0.1246 us | 0.1602 | 0.0153 |   1.31 KB |

## Credits
 - Some of the code was a adapted from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was originally adapted from https://github.com/nreco/logging, but has since been significantly modified.
 
 
