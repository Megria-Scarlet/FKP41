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
        private System.Windows.Threading.DispatcherTimer renderTimer;
        private System.Windows.Threading.DispatcherTimer statusTimer;
        private Timer backupTimer;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            watcherViewModels = [];
            watcherViewModels =
                [
                    //new(new DirectoryWatcher(string.Empty)),
                    new(new DirectoryWatcher(Environment.CurrentDirectory))
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
            this.renderTimer.Start();
            this.backupTimer.Change(1000, 1000);

            OnStatusUpdate(sender, e);
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
            foreach (var watcher in watcherViewModels)
            {

            }
        }
    }
}