using JECS.Abstractions;
using JECS.UnityStuff;
using System;
using System.IO;
using System.Timers;

namespace JECS
{
    /// <summary>
    /// A read-only version of DataFile. Data can be read from disk, but not saved to disk.
    /// </summary>
    public class ReadOnlyDataFile : ReadableDataFile, IDataFileOnDisk
    {
        /// <summary>
        /// Creates a new <see cref="ReadOnlyDataFile"/> object, using a text file in Unity's Resources folder for the default file text.
        /// </summary>
        /// <param name="path"> The path of the file. Can be either absolute or relative to the default path. </param>
        /// <param name="defaultFile"> The path of the default file, relative to any Resources parent. </param>
        /// <returns></returns>
        public static ReadOnlyDataFile WithDefaultFile(string path, string defaultFile)
        {
            string defaultFileText = ResourcesUtilities.ReadTextFromFile(defaultFile);
            return new ReadOnlyDataFile(path, defaultFileText);
        }


        /// <summary>
        /// Creates a new ReadOnlyDataFile object corresponding to a JECS file in system storage.
        /// </summary>
        /// <param name="path"> The path of the file. Can be either absolute or relative to the default path. </param>
        /// <param name="defaultFileText"> If there isn't already a file at the path, one can be created from the text supplied here. </param>
        public ReadOnlyDataFile(string path, string defaultFileText = null) : base(defaultFileText)
        {
            path = Utilities.AbsoluteJecsPath(path);
            this.FilePath = path;

            if (!Utilities.JecsFileExists(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);

                if (defaultFileText == null)
                    File.Create(path).Close(); // create empty file on disk
                else
                    File.WriteAllText(path, defaultFileText);
            }

            this.ReloadAllData();
        }

        /// <inheritdoc/>
        protected override string GetSavedText()
        {
            lock (FileSystemReadWriteLock)
            {
                if (File.Exists(FilePath))
                {
                    LastKnownWriteTimeUTC = GetCurrentLastWriteTimeUTC();
                    return File.ReadAllText(FilePath);
                }

                return String.Empty;
            }
        }

        /// <inheritdoc/>
        public override string Identifier => FilePath;


        #region IDataFileOnDisk implementation
        // this code is copied between DataFile and ReadOnlyDataFile.
        // todo: once we upgrade to c# 8, this can probably be abstracted to a default interface implementation.

        /// <inheritdoc/>
        public string FilePath { get; protected set; }
        /// <inheritdoc/>
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        /// <inheritdoc/>
        public long SizeOnDisk => new FileInfo(FilePath).Length;
        /// <inheritdoc/>
        public event Action OnAutoReload;

        /// <inheritdoc/>
        public bool AutoReload
        {
            get => _AutoReload;
            set
            {
                _AutoReload = value;

                if (value == true)
                {
                    EnsureTimerIsSetup();
                    AutoReloadTimer.Start();
                }
                else
                {
                    AutoReloadTimer?.Stop();
                }
            }
        }
        private bool _AutoReload = false;


        // To make AutoReload work, we regularly check the last write time on the filesystem.
        // We used to use FileSystemWatcher, but that class is a nasty bastard that loves to randomly not work, especially on Linux, and especially on Mono.
        // It was also a problem because it's very hard to determine whether FileSystemWatcher is firing legitimately or just because this code has saved a
        // new value to disk.

        private static readonly TimeSpan AutoReloadTimerInterval = TimeSpan.FromSeconds(1);

        private Timer AutoReloadTimer;
        private readonly object TimerLock = new object();
        private void EnsureTimerIsSetup()
        {
            if (AutoReloadTimer == null)
            {
                AutoReloadTimer = new Timer(AutoReloadTimerInterval.TotalMilliseconds);
                AutoReloadTimer.AutoReset = false;
                AutoReloadTimer.Elapsed += AutoReloadTimerElapsed;
            }
        }

        private void AutoReloadTimerElapsed(object _, ElapsedEventArgs __)
        {
            // Restart the timer manually, only after we finish ReloadIfChanged.
            // Use the lock to ensure we can properly stop the timer when Disposing.
            lock (TimerLock)
            {
                ReloadIfChanged();
                AutoReloadTimer.Start();
            }
        }
        private void ReloadIfChanged()
        {
            lock (FileSystemReadWriteLock)
            {
                if (GetCurrentLastWriteTimeUTC() != LastKnownWriteTimeUTC)
                {
                    ReloadAllData();
                    OnAutoReload?.Invoke();
                }
            }
        }

        private readonly object FileSystemReadWriteLock = new object();

        private DateTime LastKnownWriteTimeUTC;
        private DateTime GetCurrentLastWriteTimeUTC() => File.GetLastWriteTimeUtc(this.FilePath);

        public void Dispose()
        {
            lock (TimerLock)
            {
                AutoReloadTimer?.Stop();
                AutoReloadTimer?.Dispose();
            }
        }

        #endregion
    }
}