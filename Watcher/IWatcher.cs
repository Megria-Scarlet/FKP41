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
        public void OnUpdateStatus();
    }
}
