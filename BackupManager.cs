using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FKP41
{
    public sealed class BackupManager : IDisposable
    {
        private ObservableObject<string> rootBackupDirectory;
        private bool disposedValue;
        private Dictionary<string, string> backupPairs;

        public BackupManager(string rootBackupDirectory) : this(new ObservableObject<string>(rootBackupDirectory))
        {

        }
        public BackupManager(ObservableObject<string> rootBackupDirectory)
        {
            this.rootBackupDirectory = rootBackupDirectory;
            this.rootBackupDirectory.PropertyChanged += OnRootBackupDirectoryChanged;
            OnRootBackupDirectoryChanged();
        }

        public string GetBackupDirectory(string path)
        {
            if (backupPairs.TryGetValue(path, out string? s))
            {
                return s;
            }
            else
            {
                string s1;
                do
                {
                    s1 = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString("N"));
                }
                while (Directory.Exists(s1));
                backupPairs.Add(path, s1);
                SaveIndexFile();
                return s1;
            }
        }

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs))]
        private void OnRootBackupDirectoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            OnRootBackupDirectoryChanged();
        }
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs))]
        private void OnRootBackupDirectoryChanged()
        {
#pragma warning disable CS8774 // 終了時にメンバーには null 以外の値が含まれている必要があります。
            if (disposedValue)
                return;
#pragma warning restore CS8774 // 終了時にメンバーには null 以外の値が含まれている必要があります。
            var file = GetIndexFile();
            if (file.Exists)
            {
                using FileStream fileStream = file.OpenRead();
                try
                {
                    backupPairs = JsonSerializer.Deserialize<Dictionary<string, string>>(fileStream, GetJsonOptions())!;
                }
                catch (JsonException)
                {
                    backupPairs = [];
                }
            }
            else
            {
                backupPairs = [];
            }
        }
        private void SaveIndexFile()
        {
            var file = GetIndexFile();
            var directory = file.Directory;
            if (!directory!.Exists)
            {
                directory.Create();
            }
            using FileStream fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);
            fileStream.SetLength(0);
            JsonSerializer.Serialize(fileStream, backupPairs, GetJsonOptions());
        }
        private FileInfo GetIndexFile()
        {
            return new(Path.Combine(rootBackupDirectory.Value, "index.json"));
        }
        private static JsonSerializerOptions GetJsonOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                IndentSize = 4,
                WriteIndented = true,
            };
            return options;
        }
        public IEnumerable<FileInfo> RemovedBackupFiles(IEnumerable<FileInfo> files)
        {
            DirectoryInfo rootDirectory = new(this.rootBackupDirectory.Value);
            if (rootDirectory.Exists)
            {
                return files.Where(x => !x.FullName.AsSpan().StartsWith(rootDirectory.FullName));
            }
            else
            {
                return files;
            }
        }

        #region Dispose

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }
                rootBackupDirectory.PropertyChanged -= OnRootBackupDirectoryChanged;
                rootBackupDirectory = null!;

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~BackupManager()
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
