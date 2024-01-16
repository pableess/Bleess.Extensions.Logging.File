using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Bleess.Extensions.Logging.File;

/// <summary>
/// Representation for the rolling file
/// </summary>
internal class RollingFileInfo
{
    private const string SequenceFormat = "000";
    private const string Separator = "_";
    private readonly bool _utc;
    private RollingInterval _rollInterval;
    DateTime? _currentInterval;
    private string _formatted;
    private string _filePathTemplateWithoutExtension;
    private string _extension;

    int _fileSequence = 0;
    

    /// <summary>
    /// Creates an object to represent the rolling file
    /// </summary>
    /// <param name="template">the file path</param>
    /// <param name="interval">the rolling interval</param>
    /// <param name="utc">if the interval is utc</param>
    public RollingFileInfo(string template, RollingInterval interval, bool utc)
    {
        _utc = utc;
        _rollInterval = interval;
        var expanded = Environment.ExpandEnvironmentVariables(template);
        _extension = Path.GetExtension(expanded) ?? ".txt";
        _filePathTemplateWithoutExtension = Path.ChangeExtension(expanded, null);

        // get the output template
        


        // set the current date/time 
        _currentInterval = this.Now.Truncate(_rollInterval);
        _formatted = GetFileName(_currentInterval, _fileSequence);
    }


    public bool ShouldDateRoll() => Now.Truncate(_rollInterval) != _currentInterval;

    public string CurrentFile => _formatted;

    public int FileSequence => _fileSequence;


    /// <summary>
    /// Rolls the file based on the current time, optionally increase the sequence
    /// </summary>
    /// <param name="rollSequence"></param>
    /// <param name="maxSequence">A maximum sequence number if you wish to wrap file sequence back to 0</param>
    /// <returns>null if the file was not rolled</returns>
    public string Roll(bool rollSequence, int? maxSequence = null)
    {
        if (rollSequence)
        {
            _fileSequence++;
            if (maxSequence.HasValue && _fileSequence > maxSequence - 1) // because we use 0 
            {
                _fileSequence = 0;
            }
        }

        // set the current date/time 
        _currentInterval = this.Now.Truncate(_rollInterval);
        _formatted = GetFileName(_currentInterval, _fileSequence);

        return _formatted;
    }

    /// <summary>
    /// set current the rolling file info based on the contents of the files in the directory
    /// </summary>
    public bool AlignToDirectory() 
    {
        var path = Path.ChangeExtension(_filePathTemplateWithoutExtension, _extension);
        var logFileMask = Path.GetFileName(_filePathTemplateWithoutExtension) + "*" + _extension;

        var logDir = Path.GetDirectoryName(path);
        // no files yet
        if (!Directory.Exists(logDir) || logDir == null)
        {
            return false;
        }

        var logFiles = Directory.GetFiles(logDir, logFileMask, SearchOption.TopDirectoryOnly);
        if (logFiles?.Length > 0)
        {
            var lastFileInfo = logFiles
                    .Select(fName => new FileInfo(fName))
                    .OrderByDescending(fInfo => fInfo.LastWriteTime).First();

            var current = Path.GetFileName(GetFileName(_currentInterval, 0));

            // if the dates match, then parse the sequence number
            if (lastFileInfo.Name.StartsWith(Path.GetFileNameWithoutExtension(current)))
            {
                var lastFileName = Path.GetFileNameWithoutExtension(lastFileInfo.Name);

                int index = lastFileName.LastIndexOf(Separator);
                if (index < 0 || index == lastFileName.Length - 1)
                {
                    _fileSequence = 0;
                }
                else 
                {
                    // if the file has a sequence num, try to parse it
                    if (CountSubstring(lastFileName, Separator) == 2)
                    {
                        int.TryParse(lastFileName.Substring(index + 1), out _fileSequence);

                        // update the cached formatted file name
                        _formatted = GetFileName(_currentInterval, _fileSequence);
                        return true;
                    }
                }

           
            }
        }

        return false;
    }

    /// <summary>
    ///  Gets a sorts list of matching files
    /// </summary>
    /// <returns></returns>
    public IEnumerable<FileInfo> GetMatchingFilesByOldest()
    {
        var path = Path.ChangeExtension(_filePathTemplateWithoutExtension, _extension);
        var logFileMask = Path.GetFileNameWithoutExtension(_filePathTemplateWithoutExtension) + "*" + _extension;

        var logDir = Path.GetDirectoryName(path);

        if (logDir != null)
        {
            // check for files that have exceed max
            // this only happens when reducing the file size
            var matchingFiles = Directory.GetFiles(logDir, logFileMask, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f));

            // currently just sorting using last write time
            return matchingFiles.OrderBy(f => f.LastWriteTimeUtc).ToArray();
        }

        return Array.Empty<FileInfo>();
    }

    private DateTime Now => _utc ? DateTime.UtcNow : DateTime.Now;

    private static string? GetSequenceString(int sequenceNumber) => sequenceNumber == 0 ? null : sequenceNumber.ToString(SequenceFormat);

    private string GetFileName(DateTime? dateTime, int seqenceNum)
    {
        string? seq = GetSequenceString(seqenceNum);

        var builder = new StringBuilder(_filePathTemplateWithoutExtension);

        var datePart = dateTime.ToFormattedString(_rollInterval);

        if (!string.IsNullOrEmpty(datePart))
        { 
            builder.Append(Separator);
            builder.Append(datePart);
        }
        if (!string.IsNullOrEmpty(seq))
        {
            builder.Append(Separator);
            builder.Append(seq);
        }

        // set the formatted file path
        return Path.ChangeExtension(builder.ToString(), _extension);
    }

    public static int CountSubstring(string text, string value)
    {
        int count = 0, minIndex = text.IndexOf(value, 0);
        while (minIndex != -1)
        {
            minIndex = text.IndexOf(value, minIndex + value.Length);
            count++;
        }
        return count;
    }

}