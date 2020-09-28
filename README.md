# Bleess.Extensions.Logging.File
Simple rolling file logger for Microsoft.Extensions.Logging with no 3rd party dependencies

Very similar implementation to other standard MS logging providers such as Console Logger in dotnet 5.

## Features
- Text or Json output
- Rolling files 
- Standard Microsoft.Extensions.Logging configuration (similar to Console logging, etc)
- Plugable custom formatters
- Abitity to change settings while running (using IOptionsMonitor)
- Logging scopes
- High performance using dedicated write thread and message queue


This project is very similar to nReco/logging with a few additions: logging scopes, json output, streamlined configuration, and abiltity to modify settings while running.

## Usage

Add the nuget package Bleess.Extensions.Logging.File

 The log provider is configured just like any other Microsoft.Extensions.Logging providers.  There are extensions methods on the ILogBuilder to add the provider.
 
 When using Host.CreateDefaultBuilder you only need to call `AddFile()`, and the logger will be configured using configuration providers.  There are also other overloads to configure the logger using options callbacks etc.
 
 ```csharp
 logBuilder.AddFile();
 ```
 
## Configuration

Below is a sample configuration for the file provider.  The values shown are the defaults.

```
{
  "Logging": {

    "File": {    
      "IncludeScopes": true,   // this is can also be set in the formatter options
      "Path": "logs/log.txt",  // can contain environment variables
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

```
// simple text
"formatterOptions": {
   "IncludeScopes" : false,
   "SingleLine" : false,
   "EmptyLineBetweenMessages" : true,
   "TimestampFormat" : "yyyy-MM-dd h:mm tt",
   "UseUtcTimestamp" : false
}

// json formatter
"formatterOptions" : {
   "IncludeScopes" : false,
   "EmptyLineBetweenMessages" : true,
   "TimestampFormat" : "yyyy-MM-dd h:mm tt",
   "UseUtcTimestamp" : false,
   "JsonWriterOptions" : {
      // see https"://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonwriteroptions?view=netcore-3.1
   }
}
```

Custom formatters can be plugged usng extensions method `.AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder, Action<TOptions> configure)`


## Rolling Behavior
 Log files can have a max file size at which time a new file will be create with a incremented id appended.  You may also specify a maximum number of files to retain.  Once the maximum number of files has been reteached the oldest will be overwritten.

## Credits
 - Most of the code was a adapted from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was adapted from https://github.com/nreco/logging
 
 
