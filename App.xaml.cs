using System.Configuration;
using System.Data;
using System.Windows;

namespace FKP41
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string dllFilePath = string.Empty;
        public static string DllFilePath
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => dllFilePath;
        }
        public static System.Diagnostics.FileVersionInfo MyFileVersionInfo =>
                //自分自身のバージョン情報を取得する
                System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string exePath = Environment.GetCommandLineArgs()[0];
            App.dllFilePath = System.IO.Path.GetFullPath(exePath);
        }
    }

}
