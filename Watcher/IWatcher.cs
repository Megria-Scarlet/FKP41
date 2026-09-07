using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FKP41
{
    public interface IWatcher : INotifyPropertyChanged
    {
        public string FilePath { get; }
        public WatcherStatus Status { get; }
        public DateTime? LastBackupTime { get; }
        public uint FileCount { get; }
        public void OnUpdateStatus();
    }
}
