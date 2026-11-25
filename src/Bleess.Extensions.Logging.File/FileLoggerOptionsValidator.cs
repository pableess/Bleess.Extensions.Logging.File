using Microsoft.Extensions.Options;
using System;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Validates <see cref="FileLoggerOptions"/> so that a misconfiguration fails at startup instead of
    /// silently discarding log messages or deleting the file that is currently being written.
    /// </summary>
    internal sealed class FileLoggerOptionsValidator : IValidateOptions<FileLoggerOptions>
    {
        public ValidateOptionsResult Validate(string? name, FileLoggerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Path))
            {
                return ValidateOptionsResult.Fail(
                    $"FileLoggerOptions{(string.IsNullOrEmpty(name) ? string.Empty : $" ('{name}')")}: Path must not be empty.");
            }

            if (options.MaxNumberFiles < 1)
            {
                return ValidateOptionsResult.Fail(
                    $"FileLoggerOptions{(string.IsNullOrEmpty(name) ? string.Empty : $" ('{name}')")}: MaxNumberFiles must be at least 1, including the current file, but was {options.MaxNumberFiles}.");
            }

            if (options.MaxFileSizeInMB < 0)
            {
                return ValidateOptionsResult.Fail(
                    $"FileLoggerOptions{(string.IsNullOrEmpty(name) ? string.Empty : $" ('{name}')")}: MaxFileSizeInMB must be greater than or equal to 0, but was {options.MaxFileSizeInMB}.");
            }

            // a null or negative MaxFlushInterval disables interval based flushing, which is valid
            return ValidateOptionsResult.Success;
        }
    }
}
