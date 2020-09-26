# Bleess.Extensions.Logging.File
Simple rolling file logger for Microsoft.Extensions.Logging with no 3rd party dependencies

Very similar implementation to other standard MS logging providers such as Console Logger in dotnet 5.

## Features include
- Text or Json output
- Rolling files 
- Standard Microsoft.Extensions.Logging configuration (similar to Console logging, etc)
- Plugable custom formatters
- Abitity to change settings while running (using IOptionsMonitor)
- Logging scopes
- High performance using dedicated write thread and message queue


This project is very similar to nReco/logging with a few additions noteably logging scopes, json output, streamlined configuration, and abiltity to modify settings while running.

## Usage
 The log provider is configured just like any other Microsoft.Extensions.Logging providers.  There are extensions methods on the ILogBuilder to add the provider.
 
 When using Host.CreateDefaultBuilder you only need to call AddFile(), and the logger will be configured using configuration providers.  There are also other overloads to configure the logger using options callbacks etc.
 
 ```
 logBuilder.AddFile();
 ```
 
## Configuration

Below is a sample configuration for the file provider.  The values shown are the defaults.

```
{
  "Logging": {

    "File": {
      "IncludeScopes": true,
      "Path": "logs/log.txt",
      "MaxNumberFile": 7,
      "MaxFileSizeInMB": 50,  // this can be decimal
      "FormatterName": "simple",  // simple or json
      "Append": true,
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

TODO: Document formatter configuration

Custom formatters can be plugged in the same way as with Console Logger in dotnet 5.


## Rolling Behavior
 Log files can have a max file size at which time a new file will be create with a increment id.  You can also specify a maximum nuber of files to retain.  Once the maximum number of files has been reteached the oldest will be overwritten.

## Credits
 - Most of the code was a port from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was adapted from https://github.com/nreco/logging
 
 
