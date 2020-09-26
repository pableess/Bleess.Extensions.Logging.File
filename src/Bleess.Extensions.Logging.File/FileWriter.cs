using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
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
    internal class FileWriter 
    { 
        private string currentLogFileName;
        private Stream logFileStream;
        private TextWriter logFileWriter;
        private long fileSizeLimitBytes;
        private int maxRollingFiles;

        private volatile bool checkExtraFiles;

        private readonly object limitsLock = new object();

        internal FileWriter(string path, long fileSizeLimitBytes, int maxRollingFiles, bool append)
        {
            this.FilePath = path;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.maxRollingFiles = maxRollingFiles;
            DetermineLastFileLogName();
            OpenFile(append);
        }

        public string FilePath { get; }

        /// <summary>
        /// This method is thread safe to change the file limits
        /// 
        /// </summary>
        /// <param name="fileSizeLimitBytes"></param>
        /// <param name="maxRollingFile"></param>
        public void SetLimits(long fileSizeLimitBytes, int maxRollingFile)
        {
            // we only really need to lock on the max number of files
            // and only protected it when rolling to a new file

            // for performance reasons we dont need to lock on size lime as we can just do an atomic read
            lock (limitsLock)
            {
                this.checkExtraFiles = true;
                Interlocked.Exchange(ref this.fileSizeLimitBytes, fileSizeLimitBytes);
                this.maxRollingFiles = maxRollingFile;
            }
        }

        void DetermineLastFileLogName()
        {
            if (this.fileSizeLimitBytes > 0)
            {
                // rolling file is used
                var logFileMask = Path.GetFileNameWithoutExtension(this.FilePath) + "*" + Path.GetExtension(this.FilePath);
                var logDirName = Path.GetDirectoryName(this.FilePath);
                if (String.IsNullOrEmpty(logDirName))
                    logDirName = Directory.GetCurrentDirectory();
                var logFiles = Directory.GetFiles(logDirName, logFileMask, SearchOption.TopDirectoryOnly);
                if (logFiles.Length > 0)
                {
                    var lastFileInfo = logFiles
                            .Select(fName => new FileInfo(fName))
                            .OrderByDescending(fInfo => fInfo.Name)
                            .OrderByDescending(fInfo => fInfo.LastWriteTime).First();
                    currentLogFileName = lastFileInfo.FullName;
                }
                else
                {
                    // no files yet, use default name
                    currentLogFileName = this.FilePath;
                }
            }
            else
            {
                currentLogFileName = this.FilePath;
            }
        }

        void OpenFile(bool append)
        {
            var fileInfo = new FileInfo(currentLogFileName);

            // Directory.Create will check if the directory already exists,
            // so there is no need for a "manual" check first.
            fileInfo.Directory.Create();

            logFileStream = new FileStream(currentLogFileName, FileMode.OpenOrCreate, FileAccess.Write);
            if (append)
            {
                logFileStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                logFileStream.SetLength(0); // clear the file
            }
            logFileWriter = new StreamWriter(logFileStream);
        }

        string GetNextFileLogName()
        {
            lock (limitsLock)
            {
                int currentFileIndex = 0;
                var baseFileNameOnly = Path.GetFileNameWithoutExtension(this.FilePath);
                var currentFileNameOnly = Path.GetFileNameWithoutExtension(currentLogFileName);

                var suffix = currentFileNameOnly.Substring(baseFileNameOnly.Length);
                if (suffix.Length > 0 && Int32.TryParse(suffix, out var parsedIndex))
                {
                    currentFileIndex = parsedIndex;
                }
                var nextFileIndex = currentFileIndex + 1;
                if (maxRollingFiles > 0)
                {
                    nextFileIndex %= maxRollingFiles;
                }

                var nextFileName = baseFileNameOnly + (nextFileIndex > 0 ? nextFileIndex.ToString() : "") + Path.GetExtension(this.FilePath);

             

                return Path.Combine(Path.GetDirectoryName(this.FilePath), nextFileName);
            }
        }

        void CheckForNewLogFile()
        {
            bool openNewFile = false;

            long sizeLimitInBytes = Interlocked.Read(ref fileSizeLimitBytes);

            if (sizeLimitInBytes > 0 && logFileStream.Length > sizeLimitInBytes)
                openNewFile = true;

            if (openNewFile)
            {
                Close();
                currentLogFileName = GetNextFileLogName();
                OpenFile(false);
                RemoveExtraFiles();
            }
        }

        /// <summary>
        /// if there are more files than the maxcount then remove the oldest ones if possible
        /// </summary>
        void RemoveExtraFiles()
        {
            var baseFileNameOnly = Path.GetFileNameWithoutExtension(this.FilePath);
            // check for files that have exceed max
            // this only happens when reducing the file size
            var allMatchingFile = Directory.GetFiles(Path.GetDirectoryName(this.FilePath), $"{baseFileNameOnly}*{Path.GetExtension(this.FilePath)}");

            // how many to remove
            int removeCount = allMatchingFile.Length - this.maxRollingFiles;

            var orderedFiles = allMatchingFile.Select(f => new FileInfo(f)).OrderBy(f => f.LastWriteTime).GetEnumerator();

            for (int i = 0; i < removeCount; i++)
            {
                if (orderedFiles.MoveNext())
                {
                    try
                    {
                        orderedFiles.Current.Delete();
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine($"unable to remove { orderedFiles.Current}");
                    }
                }
                
            }
        }

        internal void WriteMessage(string message, bool flush)
        {
            if (logFileWriter != null)
            {
                CheckForNewLogFile();

                if (checkExtraFiles) 
                {
                    RemoveExtraFiles();
                    checkExtraFiles = false;
                }

                logFileWriter.WriteLine(message);
                if (flush)
                    logFileWriter.Flush();
            }
        }

        internal void Close()
        {
            if (logFileWriter != null)
            {
                var logWriter = logFileWriter;
                logFileWriter = null;

                logWriter.Dispose();
                logFileStream.Dispose();
                logFileStream = null;
            }

        }
    }
}
