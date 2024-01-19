# Bleess.Extensions.Logging.File
High performance rolling file logger for Microsoft.Extensions.Logging with no other 3rd party dependencies. Modeled after the standard console logger.

- Rolling files, max file size, max number or files, rolling by time period
- Text or Json output as well as custom formatters
- Standard idomatic configuration (similar to other MS logging providers) using IConfiguration or configuration callbacks
- Abitity to update settings such as log level, filter rules, or log file path while application is running
- Logging scopes and activity tracking support
- High performance using dedicated write thread and 
- Ability to specify multiple log files with independent settings


This project is very similar to nReco/logging with a few additions: multiple files, logging scopes, json output, streamlined configuration, and abiltity to modify settings while running.

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

 For performance benchmarked aginst a couple of other popular file loggers for .NET core.  Specically NReco and Karambolo loggers. See the results below.

 #### Simple Text Message
 ```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-WGADCC : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=2  LaunchCount=2  WarmupCount=10  

```
| Method         | Mean       | Error    | StdDev    | Gen0   | Gen1   | Allocated |
|--------------- |-----------:|---------:|----------:|-------:|-------:|----------:|
| BleessWrite    |   662.4 ns | 190.9 ns |  29.54 ns | 0.1068 |      - |     904 B |
| KaramboloWrite | 1,064.2 ns | 975.5 ns | 150.96 ns | 0.0839 | 0.0381 |     709 B |
| NRecoWrite     | 1,585.5 ns | 139.9 ns |  21.65 ns | 0.1621 | 0.0076 |    1371 B |


#### Json file
```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-WGADCC : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=2  LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error    | StdDev    | Gen0   | Gen1   | Allocated |
|--------------- |---------:|---------:|----------:|-------:|-------:|----------:|
| BleessWrite    | 621.8 ns | 162.6 ns |  25.16 ns | 0.1068 |      - |     904 B |
| KaramboloWrite | 985.7 ns | 721.7 ns | 111.68 ns | 0.0839 | 0.0381 |     710 B |

#### Multi-File
```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3007/23H2/2023Update/SunValley3)
AMD Ryzen 5 5600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  Job-WGADCC : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

IterationCount=2  LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------- |---------:|----------:|----------:|-------:|-------:|----------:|
| BleessWrite    | 1.441 μs | 0.2313 μs | 0.0358 μs | 0.2213 |      - |    1856 B |
| KaramboloWrite | 1.335 μs | 0.2949 μs | 0.0456 μs | 0.0877 | 0.0458 |     734 B |


## Credits
 - Most of the code was a adapted from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was adapted from https://github.com/nreco/logging
 
 
