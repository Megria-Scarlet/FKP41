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

        private SpinLock spinLock;

        public DirectoryWatcher(string path)
        {
            _directory = new(path);
            this.status = WatcherStatus.Unknown;
            this.lastBackupTime = null;
            this.isEnable = true;
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
                spinLock.TryEnter(DefaultTimeout, ref token);
                if (isEnable)
                {
                    status = this.status;
                    if (_directory.Exists)
                    {
                        if (status != WatcherStatus.Continue)
                        {
                            this.status = WatcherStatus.Continue;
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
