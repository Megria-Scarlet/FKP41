using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41.Core
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

        // バックアップ実行時のファイル更新時刻をキャッシュするコレクション。
        private SortedList<string, DateTime>? backupCache;

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
                            isChangedCount = IsPropertyChanged(ref this.fileCount, (uint)_directory.EnumerateFiles().Count());
                            isChangedByteSize = IsPropertyChanged(ref this.rawByteSize, backupManager.RemovedBackupFiles(_directory.EnumerateFiles("*.*", SearchOption.AllDirectories)).Sum(fi => fi.Length));
                        }
                    }
                    else
                    {
                        this.status = WatcherStatus.NotFound;
                        isChangedByteSize = IsPropertyChanged(ref this.rawByteSize, 0);
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

            FileInfo[] files;
            bool isCreateBackupArchive;
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

                    files = [.. backupManager.RemovedBackupFiles(this._directory.EnumerateFiles("*.*", SearchOption.AllDirectories))];
                    isCreateBackupArchive = IsAnyChangedFiles(files);
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

            if (isCreateBackupArchive)
            {
                IEnumerable<(string, string)> archivePair = files.Select(f => (f.FullName, Path.Combine(_directory.Name, Path.GetRelativePath(_directory.FullName, f.FullName))));
                backupData.CreateBackupArchive(DateTime.Now, archivePair);
            }
#if DEBUG
            backupData.DeleteMostOldBackupFiles(1, true);
#else
            backupData.DeleteMostOldBackupFiles(5, false);
#endif

            token = false;
            bool isChangedByteSize = false;
            long rawByteSize = files.Sum(fi => fi.Length);
            try
            {
                spinLock.TryEnter(ref token);
                isRunningBackup = false;
                status = WatcherStatus.Continue;
                isChangedByteSize = IsPropertyChanged(ref this.rawByteSize, rawByteSize);

                if (isCreateBackupArchive)
                {
                    lastBackupTime = DateTime.Now;
                    UpdateBackupCache(files); // バックアップが作成された場合はキャッシュを更新。
                }
            }
            finally
            {
                if (token) spinLock.Exit();
            }
            if (isCreateBackupArchive)
                NotifyPropertyChanged(nameof(LastBackupTime));
            NotifyPropertyChanged(nameof(Status));
            if (isChangedByteSize)
                NotifyPropertyChanged(nameof(RawByteSize));
        }

        private static bool IsPropertyChanged<T>(scoped ref T destination, T value) where T : System.Numerics.IEqualityOperators<T, T, bool>
        {
            if (destination != value)
            {
                destination = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// <see cref="backupCache"/> の要素を更新します。
        /// </summary>
        /// <param name="files"></param>
        private void UpdateBackupCache(scoped ReadOnlySpan<FileInfo> files)
        {
            if (files.IsEmpty)
            {
                this.backupCache = null;
            }
            else
            {
                if (this.backupCache is null)
                    this.backupCache = new(RoundUp4(files.Length));
                else
                    this.backupCache.Clear();
                foreach (var file in files)
                {
                    this.backupCache.Add(file.FullName, file.LastWriteTimeUtc);
                }
            }

            static int RoundUp4(int value) => (int)Math.Min(((uint)value + 3) & 0xFFFFFFFCu, int.MaxValue);
        }
        private bool IsAnyChangedFiles(scoped ReadOnlySpan<FileInfo> files)
        {
            var cache = this.backupCache;
            if (cache is null || cache.Count == 0)
            {
                return !files.IsEmpty;
            }
            else if (files.Length != cache.Count)
            {
                return true;
            }
            else
            {
                foreach (var file in files)
                {
                    if (!cache.TryGetValue(file.FullName, out var result) || result != file.LastWriteTimeUtc)
                    {
                        return true;
                    }
                }
                return false;
            }
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
