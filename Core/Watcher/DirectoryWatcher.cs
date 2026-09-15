using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41
{
    internal class DirectoryWatcher : IWatcher
    {
        private const int DefaultTimeout = 1000;
        private DirectoryInfo _directory;
        private BackupManager backupManager;
        private BackupOptionsData backupData;
        public BackupOptionsData Options
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => backupData;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public string FilePath
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => _directory.FullName;
        }
        private WatcherStatus status;
        public WatcherStatus Status
        {
            get
            {
                bool token = false;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    return this.status;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
        }
        private DateTime? lastBackupTime;
        public DateTime? LastBackupTime
        {
            get
            {
                bool token = false;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    return lastBackupTime;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
        }
        private uint fileCount;
        public uint FileCount
        {
            get
            {
                bool token = false;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    return fileCount;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
        }
        private bool isEnable;
        public bool IsEnable
        {
            get
            {
                bool token = false;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    return isEnable;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
            set
            {
                bool token = false;
                bool isChenged = true;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    if (this.isEnable != value)
                    {
                        this.isEnable = value;
                        if (!value)
                        {
                            this.status = WatcherStatus.Invalid;
                        }
                        else
                        {
                            this.status = WatcherStatus.Accepted;
                        }
                        isChenged = true;

                        if (backupData.IsValid != this.isEnable)
                        {
                            if (backupData is BackupDataCache cache)
                            {
                                cache.IsValid = this.isEnable;
                            }
                            else
                            {
                                cache = new BackupDataCache(backupData)
                                {
                                    IsValid = this.isEnable
                                };
                                backupData = cache;
                            }
                        }
                    }
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
                if (isChenged)
                {
                    NotifyPropertyChanged(nameof(IsEnable));
                    NotifyPropertyChanged(nameof(Status));
                }
            }
        }
        private long rawByteSize;
        public long RawByteSize
        {
            get
            {
                bool token = false;
                try
                {
                    spinLock.TryEnter(DefaultTimeout, ref token);
                    return rawByteSize;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
        }

        private SpinLock spinLock;

        public DirectoryWatcher(string path, BackupManager backupManager) : this(path, backupManager, backupManager.GetBackupData(path))
        {

        }
        public DirectoryWatcher(string path, BackupManager backupManager, BackupOptionsData backupData)
        {
            _directory = new(path);
            this.status = WatcherStatus.Unknown;
            this.isEnable = backupData.IsValid;
            spinLock = new SpinLock();
            this.backupManager = backupManager;
            this.backupData = backupData;
            this.lastBackupTime = this.backupData.LastBackupTime;
        }
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void OnUpdateStatus()
        {
            bool token = false;
            WatcherStatus status;
            bool isChangedStatus = false;
            bool isChangedCount = false;
            bool isChangedByteSize = false;
            try
            {
                spinLock.TryEnter(DefaultTimeout, ref token);
                if (this.status == WatcherStatus.Processing)
                    return;
                if (isEnable)
                {
                    status = this.status;
                    if (_directory.Exists)
                    {
                        if (status != WatcherStatus.Continue)
                        {
                            this.status = WatcherStatus.Continue;
                            isChangedCount = IsChanged(ref this.fileCount, (uint)_directory.EnumerateFiles().Count());
                            isChangedByteSize = IsChanged(ref this.rawByteSize, backupManager.RemovedBackupFiles(_directory.EnumerateFiles("*.*", SearchOption.AllDirectories)).Sum(fi => fi.Length));
                        }
                    }
                    else
                    {
                        this.status = WatcherStatus.NotFound;
                        isChangedByteSize = IsChanged(ref this.rawByteSize, 0);
                    }
                    isChangedStatus = status != this.status;
                }
                // BackupData がキャッシュの場合は更新
                if (this.backupData is BackupDataCache backupDataCache)
                {
                    if (backupDataCache.IsChanged)
                    {
                        BackupOptionsData backupData = backupDataCache.Clone();
                        if (backupManager.SetBackupData(FilePath, backupData))
                        {
                            this.backupData = backupData;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (token) spinLock.Exit();
            }
            if (isChangedStatus)
                NotifyPropertyChanged(nameof(Status));
            if (isChangedCount)
                NotifyPropertyChanged(nameof(FileCount));
            if (isChangedByteSize)
                NotifyPropertyChanged(nameof(RawByteSize));
        }
        private bool isRunningBackup;
        public void OnBackup()
        {
            bool token = false;

            try
            {
                spinLock.TryEnter(DefaultTimeout, ref token);
                if (isRunningBackup || !isEnable)
                {
                    return;
                }
                else
                {
                    isRunningBackup = true;
                    status = WatcherStatus.Processing;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (token) spinLock.Exit();
            }
            NotifyPropertyChanged(nameof(Status));

            IEnumerable<FileInfo> files = backupManager.RemovedBackupFiles(this._directory.EnumerateFiles("*.*", SearchOption.AllDirectories));
            IEnumerable<(string, string)> archivePair = files.Select(f => (f.FullName, Path.Combine(_directory.Name, Path.GetRelativePath(_directory.FullName, f.FullName))));
            backupData.CreateBackupArchive(DateTime.Now, archivePair);

#if DEBUG
            backupData.DeleteMostOldBackupFiles(1, true);
#else
            backupData.DeleteMostOldBackupFiles(5, false);
#endif

            token = false;
            bool isChangedByteSize = false;
            long rawByteSize = backupManager.RemovedBackupFiles(_directory.EnumerateFiles("*.*", SearchOption.AllDirectories)).Sum(fi => fi.Length);
            try
            {
                spinLock.TryEnter(ref token);
                lastBackupTime = DateTime.Now;
                isRunningBackup = false;
                status = WatcherStatus.Continue;
                isChangedByteSize = IsChanged(ref this.rawByteSize, rawByteSize);
            }
            finally
            {
                if (token) spinLock.Exit();
            }
            NotifyPropertyChanged(nameof(LastBackupTime));
            NotifyPropertyChanged(nameof(Status));
            if (isChangedByteSize)
                NotifyPropertyChanged(nameof(RawByteSize));
        }

        private static bool IsChanged<T>(scoped ref T destination, T value) where T : System.Numerics.IEqualityOperators<T, T, bool>
        {
            if (destination != value)
            {
                destination = value;
                return true;
            }
            return false;
        }

        private class BackupDataCache : BackupOptionsData, ICloneable
        {
            private readonly BackupOptionsData baseBackupData;
            public BackupDataCache(BackupOptionsData baseBackupData) : base(baseBackupData)
            {
                this.baseBackupData = baseBackupData;
            }
            public bool IsChanged
            {
                get
                {
                    return !ValueEquals(this, baseBackupData);
                }
            }
            public BackupOptionsData Clone() => new(this);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            object ICloneable.Clone() => Clone();
        }
    }
}
