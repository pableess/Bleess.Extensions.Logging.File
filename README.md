# Bleess.Extensions.Logging.File
Simple rolling file logger for Microsoft.Extensions.Logging with no 3rd party dependencies

Very similar implementation to other standard MS logging providers such as Console Logger.

Features include
- Text or Json output
- Rolling files 
- Standard Microsoft.Extensions.Logging configuration (similar to Console logging, etc)
- Plugable custom formatters
- Abitity to change settings while running
- Scopes

Credits
 - Most of the code was a port from dotnet source code (specifically Microsoft.Extensions.Logging.Console) https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Extensions.Logging.Console
 - The FileWriter was adapted from https://github.com/nreco/logging
 
 
