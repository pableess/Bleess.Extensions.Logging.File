using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        static TraceSource ts = new TraceSource(typeof(FileWriter).FullName);

        private Stream logFileStream;
        private TextWriter logFileWriter;
        private long fileSizeLimitBytes;
        private int maxRollingFiles;
        private bool append;
        private bool hasOpened;

        private volatile bool checkExtraFiles;

        private RollingFileInfo rollingFileInfo;

        private readonly object limitsLock = new object();

        internal FileWriter(string path, long fileSizeLimitBytes, int maxRollingFiles, bool append, RollingInterval rollInterval)
        {
            this.FilePath = path;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.maxRollingFiles = maxRollingFiles;
            this.RollInterval = rollInterval;
            this.rollingFileInfo = new RollingFileInfo(path, rollInterval, false);

            this.append = append;
         }

        public string FilePath { get; }

        public RollingInterval RollInterval { get; }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckOpen() 
        {
            if (!this.hasOpened)
            {
                this.OpenAndAlignFile();
            }
        }

        void OpenAndAlignFile()
        {
            if (this.fileSizeLimitBytes > 0)
            {
                // update current log sequence number to last one
                this.rollingFileInfo.AlignToDirectory();
            }

            OpenFile(this.append);
        }

        void OpenFile(bool append)
        {
            var fileInfo = new FileInfo(rollingFileInfo.CurrentFile);

            // Directory.Create will check if the directory already exists,
            // so there is no need for a "manual" check first.
            fileInfo.Directory.Create();

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

        void CheckForNewLogFile()
        {
            bool sizeExceeded = false;

            long sizeLimitInBytes = Interlocked.Read(ref fileSizeLimitBytes);

            if (sizeLimitInBytes > 0 && logFileStream.Length > sizeLimitInBytes) 
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
        void RemoveExtraFiles()
        {
            var matchingFiles = rollingFileInfo.GetMatchingFilesByOldest();

            // how many to remove
            int removeCount = matchingFiles.Count() - this.maxRollingFiles;

            var iterator = matchingFiles.GetEnumerator();

            for (int i = 0; i < removeCount; i++)
            {
                if (iterator.MoveNext())
                {
                    if (iterator.Current.FullName == new FileInfo(rollingFileInfo.CurrentFile).FullName)
                    {
                        System.Diagnostics.Debug.Fail("Error");
                    }

                    try
                    {
                        iterator.Current.Delete();
                    }
                    catch (Exception)
                    {
                        ts.TraceInformation($"unable to remove {iterator.Current}");
                    }
                }
                
            }
        }

        internal void WriteMessage(string message, bool flush)
        {
            CheckOpen();

            if (logFileWriter != null)
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
                    logFileStream.Flush();
                }
            }
        }

        internal void Close()
        {
            if (logFileWriter != null)
            {
                var logWriter = logFileWriter;
                logFileWriter = null;

                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
                logFileStream.Dispose();
                logFileStream = null;
            }

        }
    }
}
