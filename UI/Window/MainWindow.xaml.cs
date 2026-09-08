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
            InitializeComponent();
            this.Loaded += OnLoaded;
            backupPath = new(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(App.DllFilePath)!, "backup"));
            backupManager = new(backupPath);

            watcherViewModels = [];
            watcherViewModels =
                [
                    //new(new DirectoryWatcher(string.Empty)),
                    new(new DirectoryWatcher(Environment.CurrentDirectory, backupManager))
                ];
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