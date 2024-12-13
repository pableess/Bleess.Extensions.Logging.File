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

 ## Benchmarks

 The library has be benchmarked against a couple of other popular file loggers for .NET core.  Specically NReco and Karambolo loggers. See the results below.

 ### Simple Text Message
 ```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-HSNTJM : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10  

```

| Method                 | Mean            | Error         | StdDev        | Gen0      | Gen1     | Allocated  |
|----------------------- |-----------------|---------------|---------------|-----------|----------|------------|
| Bleess_single_write    |        659.6 ns |      20.31 ns |      22.57 ns |    0.1068 |   0.0038 |      904 B |
| Karambolo_single_write |        679.7 ns |      82.32 ns |      94.80 ns |    0.0839 |        - |      706 B |
| NReco_single_write     |      1,547.5 ns |      13.10 ns |      14.56 ns |    0.1640 |   0.0057 |     1373 B |
| Bleess_10000_writes    |  6,469,397.9 ns | 163,216.34 ns | 181,414.53 ns | 1078.1250 |        - |  9040006 B |
| Karambolo_10000_write  |  6,522,278.8 ns | 318,328.54 ns | 366,587.62 ns |  843.7500 |        - |  7083785 B |
| NReco_10000_write      | 15,962,119.6 ns | 380,106.19 ns | 406,709.37 ns | 1625.0000 | 125.0000 | 13713687 B |


### Json file
```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-HSNTJM : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10  

```

| Method                      | Mean     | Error     | StdDev    | Gen0   | Gen1   | Gen2   | Allocated |
|---------------------------- |----------|-----------|-----------|--------|--------|--------|-----------|
| Bleess_single_write_json    | 1.990 μs | 0.0469 μs | 0.0521 μs | 0.9766 |      - |      - |   7.99 KB |
| Karambolo_single_write_json | 2.118 μs | 0.1781 μs | 0.2051 μs | 0.3204 | 0.0458 | 0.0153 |   2.65 KB |


### Multi-File
```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-HSNTJM : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=2  WarmupCount=10  

```

| Method                           | Mean     | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------------------------- |----------|-----------|-----------|--------|--------|-----------|
| Bleess_multifile_single_write    | 1.374 μs | 0.0396 μs | 0.0441 μs | 0.1984 |      - |   1.67 KB |
| Karambolo_multifile_single_write | 1.554 μs | 0.1430 μs | 0.1647 μs | 0.1602 | 0.0229 |   1.33 KB |


## Credits
 - Some of the code was a adapted from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was originally adapted from https://github.com/nreco/logging, but has since been significantly modified.
 
 
