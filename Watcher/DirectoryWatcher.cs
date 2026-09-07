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
        private DirectoryInfo _directory;
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
                    spinLock.TryEnter(1000, ref token);
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
                    spinLock.TryEnter(1000, ref token);
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
                    spinLock.TryEnter(1000, ref token);
                    return fileCount;
                }
                finally
                {
                    if (token) spinLock.Exit();
                }
            }
        }

        private SpinLock spinLock;

        public DirectoryWatcher(string path)
        {
            _directory = new(path);
            this.status = WatcherStatus.Unknown;
            this.lastBackupTime = null;
            spinLock = new SpinLock();
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
            uint fileCount;
            bool isChengedStatus = false;
            bool isChengedCount = false;
            try
            {
                spinLock.TryEnter(1000, ref token);
                status = this.status;
                if (_directory.Exists)
                {
                    if (status != WatcherStatus.Accepted)
                    {
                        this.status = WatcherStatus.Forbidden;
                        fileCount = (uint)_directory.EnumerateFiles().Count();
                        if (fileCount != this.fileCount)
                        {
                            this.fileCount = fileCount;
                            isChengedCount = true;
                        }
                    }
                }
                else
                {
                    this.status = WatcherStatus.NotFound;
                }
                isChengedStatus = status != this.status;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (token) spinLock.Exit();
            }
            if (isChengedStatus)
                NotifyPropertyChanged(nameof(Status));
            if (isChengedCount)
                NotifyPropertyChanged(nameof(FileCount));
        }
    }
}
