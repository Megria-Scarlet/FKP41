using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41
{
    public class WatcherViewModel : INotifyPropertyChanged, IDisposable, IWatcher
    {
        private IWatcher _watcher;
        private bool disposedValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FilePath
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.FilePath;
        }
        public WatcherStatus Status
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.Status;
        }
        public DateTime? LastBackupTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.LastBackupTime;
        }
        private DateTime nextBackupTime;
        private TimeSpan nextBackupSpan;
        public TimeSpan NextBackupSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => nextBackupSpan;
        }
        public uint FileCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.FileCount;
        }
        public bool IsEnable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _watcher.IsEnable;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _watcher.IsEnable = value;
        }

        public WatcherViewModel(IWatcher watcher)
        {
            this._watcher = watcher;
            this._watcher.PropertyChanged += OnPropertyChanged;
            this.nextBackupTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IWatcher.Status):
                    NotifyPropertyChanged(nameof(Status));
                    break;
                case nameof(IWatcher.LastBackupTime):
                    NotifyPropertyChanged(nameof(LastBackupTime));
                    break;
                case nameof(IWatcher.FileCount):
                    NotifyPropertyChanged(nameof(FileCount));
                    break;
                case nameof(IWatcher.IsEnable):
                    NotifyPropertyChanged(nameof(IsEnable));
                    break;
            }
        }
        public void OnUpdateNextBackupSpan()
        {
            TimeSpan span = nextBackupTime - DateTime.UtcNow;
            if (span.Ticks < 0)
                span = TimeSpan.Zero;
            if (span != this.nextBackupSpan)
            {
                this.nextBackupSpan = span;
                NotifyPropertyChanged(nameof(NextBackupSpan));
            }
        }
        public void OnUpdateStatus()
        {
            _watcher.OnUpdateStatus();
        }

        public void OnBackup()
        {
            try
            {
                _watcher.OnBackup();
            }
            catch (System.IO.IOException)
            {
                return;
            }
            this.nextBackupTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            OnUpdateNextBackupSpan();
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }
                _watcher.PropertyChanged -= OnPropertyChanged;
                _watcher = null!;

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~WatcherViewModel()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
