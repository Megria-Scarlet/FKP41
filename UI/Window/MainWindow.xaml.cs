using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FKP41
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Collections.ObjectModel.ObservableCollection<WatcherViewModel> watcherViewModels;
        private ObservableObject<string> backupPath;
        public string BackupPath
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => backupPath;
        }
        private BackupManager backupManager;

        #region Timer

        private System.Windows.Threading.DispatcherTimer renderTimer;
        private System.Windows.Threading.DispatcherTimer statusTimer;
        private Timer backupTimer;
        #endregion

        public MainWindow()
        {
            // 例外が処理されなかったら発生する（.NET 1.0 より）
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();
            this.Loaded += OnLoaded;
            backupPath = new(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(App.DllFilePath)!, "backup"));
            backupManager = new(backupPath);

            watcherViewModels = [.. backupManager.CreateWatchers().Select(x => new WatcherViewModel(x))];
            
            renderTimer = new(System.Windows.Threading.DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * 10)
            };
            renderTimer.Tick += OnRenderUpdate;
            statusTimer = new(System.Windows.Threading.DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromTicks(TimeSpan.TicksPerMinute)
            };
            statusTimer.Tick += OnStatusUpdate;
            backupTimer = new(OnAutoBackup);

            // Title の設定
            Title = $"{Title} ver.{App.MyFileVersionInfo.FileMajorPart}.{App.MyFileVersionInfo.FileMinorPart}.{App.MyFileVersionInfo.FileBuildPart}.{App.MyFileVersionInfo.FilePrivatePart}";
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.WatcherListView.ItemsSource = watcherViewModels;
            OnStartTimer();
            OnStatusUpdate(sender, e);
            Binding binding = new(nameof(ObservableObject<>.Value))
            {
                Source = backupPath,
                StringFormat = "バックアップ保存先:\"{0}\""
            };
            BackupDirectoryView.SetBinding(TextBlock.TextProperty, binding);

            void OnStartTimer()
            {
                this.renderTimer.Start();
                this.statusTimer.Start();
                this.backupTimer.Change(1000, 1000);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            renderTimer.Stop();
            backupTimer.Dispose();
        }


        /// <summary>
        /// 最終的に処理されなかった未処理例外を処理します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? exception = e.ExceptionObject as Exception;
            while (exception?.InnerException != null)
                exception = exception.InnerException;
            var message = $"予期せぬエラーが発生しました。続けて発生する場合は開発者に報告してください。";
            if (exception != null) message += $"\n({exception.Message} @ {exception.TargetSite?.Name ?? "null"})";
            try
            {
                string errerPath = $"{System.IO.Path.GetDirectoryName(App.DllFilePath)}\\ErrerLog\\{DateTime.Now:yyyy-MM-dd-HHmmss}.txt";
                string? d = System.IO.Path.GetDirectoryName(errerPath);
                if (!string.IsNullOrEmpty(d))
                {
                    System.IO.DirectoryInfo directoryInfo = new(d);
                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                    }
                }
                using System.IO.FileStream fileStream = new(errerPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);
                System.Text.UTF8Encoding encoding = new(false);
                using System.IO.StreamWriter writer = new(fileStream, encoding);
                if (exception != null)
                {
                    writer.WriteLine(exception.ToString());
                }
                else
                {
                    writer.WriteLine("詳細不明なエラーが発生しました。");
                }
            }
            catch (Exception e2)
            {
                System.Diagnostics.Debug.WriteLine(e2.ToString());
            }
            // Logger.Fatal("未処理例外", exception); // 適当なログ記録
            MessageBox.Show(message, "未処理例外", MessageBoxButton.OK, MessageBoxImage.Stop);
#if DEBUG

#else
            Environment.Exit(1);
#endif
        }

        private void OnRenderUpdate(object? sender, EventArgs e)
        {
            foreach (var watcher in watcherViewModels)
            {
                watcher.OnUpdateNextBackupSpan();
            }
        }
        private void OnStatusUpdate(object? sender, EventArgs e)
        {
            foreach (var watcher in watcherViewModels)
            {
                watcher.OnUpdateStatus();
            }
        }
        private void OnAutoBackup(object? state)
        {
            Timer? timer = (Timer?)state;
            foreach (var watcher in watcherViewModels.Where(x => x.IsEnable))
            {
                if (watcher.NextBackupSpan <= TimeSpan.Zero)
                    watcher.OnBackup();
            }
        }

        private void AddButton_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void AddButton_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var name in fileNames)
                {
                    if (System.IO.File.Exists(name))
                    {

                    }
                    else if (System.IO.Directory.Exists(name))
                    {
                        DirectoryWatcher watcher = new(name, backupManager);
                        watcher.OnUpdateStatus();
                        watcherViewModels.Add(new(watcher));
                    }
                }
            }
        }
    }
}