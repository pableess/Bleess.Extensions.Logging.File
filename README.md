# Bleess.Extensions.Logging.File
Simple rolling file logger for Microsoft.Extensions.Logging with no 3rd party dependencies

Very similar implementation to other standard MS logging providers such as Console Logger in dotnet 5.

## Features
- Rolling files 
- Text or Json output
- Standard idomatic configuration (similar to other MS logging providers) using IConfiguration or configuration callbacks
- Plugable custom formatters
- Abitity to update settings such as log level, filter rules, or log file path while application is running
- Logging scopes and activity tracking support
- High performance using dedicated write thread and message queue
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
   "UseUtcTimestamp" : false
}

// json formatter
"FormatterOptions" : {
   "IncludeScopes" : false,
   "EmptyLineBetweenMessages" : true,
   "TimestampFormat" : "yyyy-MM-dd h:mm tt",
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
 Log files can have a max file size at which time a new file will be create with a incremented id appended.  You may also specify a maximum number of files to retain.  Once the maximum number of files has been reteached the oldest will be overwritten.

## Credits
 - Most of the code was a adapted from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was adapted from https://github.com/nreco/logging
 
 
