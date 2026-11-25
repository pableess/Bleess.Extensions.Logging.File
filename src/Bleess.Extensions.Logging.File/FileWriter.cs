using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bleess.Extensions.Logging.File
{
    /// <summary>
    /// Writes the entries to the log file.
    /// 
    /// This code was adapted from https://github.com/nreco/logging/blob/master/src/NReco.Logging.File/FileLoggerProvider.cs
    ///   Copyright 2017-2020 Vitaliy Fedorchenko and contributors
    ///   MIT License
    /// 
    /// 
    /// Additions include
    ///  - ability to change file limits while running (thread safe)
    /// 
    /// since this writer is used from the single thread the operations are not thread safe with the exception of SetLimits which allows these values to be changed 
    /// from another thread
    /// 
    ///
    /// </summary>
    internal sealed class FileWriter : IDisposable
    {
        private static readonly TraceSource s_trace = new TraceSource(typeof(FileWriter).FullName ?? nameof(FileWriter));

        private WriteCountingFileStream? logFileStream;
        private TextWriter? logFileWriter;
        private long fileSizeLimitBytes;
        private bool flushToDisk;
        private int maxRollingFiles;
        private readonly bool append;
        private bool hasOpened;

        private volatile bool checkExtraFiles;

        private readonly RollingFileInfo rollingFileInfo;

#if NET9_0_OR_GREATER
        private readonly Lock limitsLock = new();
#else
        private readonly object limitsLock = new();
#endif

        internal FileWriter(
            string path,
            long fileSizeLimitBytes,
            int maxRollingFiles,
            bool append,
            RollingInterval rollInterval,
            bool flushToDisk,
            bool useUtcTimestamps,
            TimeProvider timeProvider)
        {
            this.FilePath = path;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.maxRollingFiles = maxRollingFiles;
            this.RollInterval = rollInterval;
            this.rollingFileInfo = new RollingFileInfo(path, rollInterval, useUtcTimestamps, timeProvider);
            this.flushToDisk = flushToDisk;

            this.append = append;

            // open eagerly so that an unwritable path or an invalid directory fails at startup instead of
            // being discovered later on the write thread (where the message would be lost)
            OpenAndAlignFile();
        }

        public string FilePath { get; }

        public RollingInterval RollInterval { get; }

        /// <summary>
        /// This method is thread safe to change the file limits
        /// 
        /// </summary>
        /// <param name="fileSizeLimitBytes"></param>
        /// <param name="maxRollingFile"></param>
        /// <param name="flushToDisk"></param>
        public void SetLimits(long fileSizeLimitBytes, int maxRollingFile, bool flushToDisk)
        {
            // we only really need to lock on the max number of files
            // and only protected it when rolling to a new file

            // for performance reasons we dont need to lock on size lime as we can just do an atomic read
            lock (limitsLock)
            {
                this.flushToDisk = flushToDisk;
                this.checkExtraFiles = true;
                Interlocked.Exchange(ref this.fileSizeLimitBytes, fileSizeLimitBytes);
                this.maxRollingFiles = maxRollingFile;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckOpen()
        {
            if (!this.hasOpened)
            {
                this.OpenAndAlignFile();
            }
        }

        private void OpenAndAlignFile()
        {
            // a non-appending writer starts a fresh file at the base name. Continuing the sequence
            // from a previous run would truncate the newest old file for no benefit, and would make
            // Append=false mean different things depending on whether size rolling is enabled.
            if (this.append && this.fileSizeLimitBytes > 0)
            {
                // update current log sequence number to last one
                this.rollingFileInfo.AlignToDirectory();
            }

            OpenFile(this.append);
        }

        private void OpenFile(bool append)
        {
            var fileInfo = new FileInfo(rollingFileInfo.CurrentFile);

            // Directory.Create will check if the directory already exists,
            // so there is no need for a "manual" check first.
            fileInfo.Directory?.Create();

            // wrap the file stream with a stream that tracks the size without an p/invoke on every .Lenght reference
            logFileStream = new FileStream(rollingFileInfo.CurrentFile, FileMode.OpenOrCreate, FileAccess.Write).ToWriteCountingStream();
            if (append)
            {
                logFileStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                logFileStream.SetLength(0); // clear the file
            }
            logFileWriter = new StreamWriter(logFileStream);

            this.hasOpened = true;
        }

        private void CheckForNewLogFile()
        {
            bool sizeExceeded = false;

            long sizeLimitInBytes = Interlocked.Read(ref fileSizeLimitBytes);

            if (sizeLimitInBytes > 0 && logFileStream!.Length > sizeLimitInBytes)
            {
                sizeExceeded = true;
            }

            if (rollingFileInfo.ShouldDateRoll() || sizeExceeded)
            {
                Close(); // close the current file before rolling to new one

                // if not rolling based on a date, then roll the sequence number back to 0
                int? maxSequence = this.RollInterval == RollingInterval.Infinite ? this.maxRollingFiles : null;
                rollingFileInfo.Roll(sizeExceeded, maxSequence);

                OpenFile(false);

                // remove old files if needed
                RemoveExtraFiles();
            }
        }

        /// <summary>
        /// if there are more files than the maxcount then remove the oldest ones if possible
        /// </summary>
        private void RemoveExtraFiles()
        {
            // read the limit once: it may be changed from another thread
            int maxFiles = Volatile.Read(ref this.maxRollingFiles);

            var matchingFiles = rollingFileInfo.GetMatchingFilesByOldest();

            // how many to remove
            int removeCount = matchingFiles.Length - maxFiles;
            if (removeCount <= 0)
            {
                return;
            }

            string currentFile = Path.GetFullPath(rollingFileInfo.CurrentFile);
            // file names are case sensitive on Unix
            var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var file in matchingFiles)
            {
                if (removeCount <= 0)
                {
                    break;
                }

                // never delete the file that is currently being written
                if (string.Equals(file.FullName, currentFile, pathComparison))
                {
                    continue;
                }

                try
                {
                    file.Delete();
                    removeCount--;
                }
                catch (Exception ex)
                {
                    s_trace.TraceEvent(TraceEventType.Warning, 0, $"Unable to remove log file '{file.FullName}': {ex.Message}");
                }
            }
        }

        internal void WriteMessage(string message, bool flush)
        {
            CheckOpen();

            if (logFileWriter is not null)
            {
                CheckForNewLogFile();

                if (checkExtraFiles)
                {
                    RemoveExtraFiles();
                    checkExtraFiles = false;
                }

                // write the message
                logFileWriter.WriteLine(message);

                if (flush)
                {
                    logFileWriter.Flush();
                    logFileStream!.Flush(this.flushToDisk);
                }
            }
        }

        public void Dispose() => Close();

        internal void Close()
        {
            if (logFileWriter is not null)
            {
                var logWriter = logFileWriter;
                logFileWriter = null;

                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
                logFileStream?.Dispose();
                logFileStream = null;
            }
        }
    }
}
