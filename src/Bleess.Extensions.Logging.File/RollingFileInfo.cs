using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#nullable enable

namespace Bleess.Extensions.Logging.File;

/// <summary>
/// Representation for the rolling file
/// </summary>
internal sealed class RollingFileInfo
{
    private const string SequenceFormat = "000";
    private const string Separator = "_";
    private readonly bool _utc;
    private readonly RollingInterval _rollInterval;
    private readonly TimeProvider _timeProvider;
    private DateTime? _currentInterval;
    private string _formatted;
    private readonly string _filePathTemplateWithoutExtension;
    private readonly string _extension;

    private int _fileSequence;

    /// <summary>
    /// Creates an object to represent the rolling file
    /// </summary>
    /// <param name="template">the file path</param>
    /// <param name="interval">the rolling interval</param>
    /// <param name="utc">if the interval is utc</param>
    /// <param name="timeProvider">the clock used for date rolling</param>
    public RollingFileInfo(string template, RollingInterval interval, bool utc, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _utc = utc;
        _rollInterval = interval;
        var expanded = Environment.ExpandEnvironmentVariables(template);
        _extension = Path.GetExtension(expanded);
        _filePathTemplateWithoutExtension = Path.ChangeExtension(expanded, null) ?? expanded;

        // set the current date/time 
        _currentInterval = this.Now.Truncate(_rollInterval);
        _formatted = GetFileName(_currentInterval, _fileSequence);
    }


    public bool ShouldDateRoll()
    {
        // nothing to roll when the interval is infinite; avoid reading the clock on the hot path
        if (_rollInterval == RollingInterval.Infinite)
        {
            return false;
        }

        return Now.Truncate(_rollInterval) != _currentInterval;
    }

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
        var logDir = Path.GetDirectoryName(_filePathTemplateWithoutExtension);
        // no files yet
        if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
        {
            return false;
        }

        var logFileMask = GetFileMask();

        var logFiles = Directory.GetFiles(logDir, logFileMask, SearchOption.TopDirectoryOnly);
        if (logFiles.Length > 0)
        {
            var lastFileInfo = logFiles
                    .Select(fName => new FileInfo(fName))
                    .OrderByDescending(fInfo => fInfo.LastWriteTimeUtc)
                    .First();

            // the base name for this interval, without a sequence number, for example "log_20240115" or "log"
            string currentBase = Path.GetFileNameWithoutExtension(GetFileName(_currentInterval, 0));
            string lastBase = Path.GetFileNameWithoutExtension(lastFileInfo.Name);

            // the newest file belongs to the current interval but has no sequence number
            if (string.Equals(lastBase, currentBase, StringComparison.OrdinalIgnoreCase))
            {
                _fileSequence = 0;
                return true;
            }

            // only a suffix that follows the date part can be a sequence. Comparing against the date part
            // itself would treat "log_20240115" as sequence 20240115.
            if (lastBase.StartsWith(currentBase + Separator, StringComparison.OrdinalIgnoreCase))
            {
                var fileSeq = lastBase.Substring(currentBase.Length + Separator.Length);

                if (int.TryParse(fileSeq, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedSequence))
                {
                    _fileSequence = parsedSequence;

                    // update the cached formatted file name
                    _formatted = GetFileName(_currentInterval, _fileSequence);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///  Gets a sorts list of matching files
    /// </summary>
    /// <returns></returns>
    public FileInfo[] GetMatchingFilesByOldest()
    {
        var logDir = Path.GetDirectoryName(_filePathTemplateWithoutExtension);

        if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
        {
            return Array.Empty<FileInfo>();
        }

        // check for files that have exceed max
        // this only happens when reducing the file size
        return Directory.GetFiles(logDir, GetFileMask(), SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f))
            // currently just sorting using last write time
            .OrderBy(f => f.LastWriteTimeUtc)
            .ToArray();
    }

    private string GetFileMask()
    {
        return Path.GetFileNameWithoutExtension(_filePathTemplateWithoutExtension) + "*" + _extension;
    }

    private DateTime Now => _utc
        ? _timeProvider.GetUtcNow().UtcDateTime
        : _timeProvider.GetLocalNow().DateTime;

    private static string? GetSequenceString(int sequenceNumber) => sequenceNumber == 0 ? null : sequenceNumber.ToString(SequenceFormat, CultureInfo.InvariantCulture);

    private string GetFileName(DateTime? dateTime, int seqenceNum)
    {
        string? seq = GetSequenceString(seqenceNum);

        var builder = new StringBuilder(_filePathTemplateWithoutExtension);

        var datePart = dateTime.ToFormattedString(_rollInterval);

        if (!string.IsNullOrEmpty(datePart))
        {
            builder.Append(Separator[0]);
            builder.Append(datePart);
        }
        if (!string.IsNullOrEmpty(seq))
        {
            builder.Append(Separator[0]);
            builder.Append(seq);
        }

        // Path.ChangeExtension with an empty extension would append a trailing '.'
        var name = builder.ToString();
        return string.IsNullOrEmpty(_extension) ? name : Path.ChangeExtension(name, _extension);
    }
}
