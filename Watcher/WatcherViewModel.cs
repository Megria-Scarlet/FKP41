using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41
{
    public class WatcherViewModel : INotifyPropertyChanged
    {
        private IWatcher _watcher;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string FilePath
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.FilePath;
        }

        public WatcherViewModel(IWatcher watcher)
        {
            this._watcher = watcher;
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
