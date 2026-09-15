using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace FKP41
{
    public sealed class BackupManager : IDisposable
    {
        private ObservableObject<string> rootBackupDirectory;
        private bool disposedValue;
        private Dictionary<string, BackupData> backupPairs;
        private List<string> indexes;
        private bool isChengedBackupPairs;

        public BackupManager(string rootBackupDirectory) : this(new ObservableObject<string>(rootBackupDirectory))
        {

        }
        public BackupManager(ObservableObject<string> rootBackupDirectory)
        {
            this.rootBackupDirectory = rootBackupDirectory;
            this.rootBackupDirectory.PropertyChanged += OnRootBackupDirectoryChanged;
            OnRootBackupDirectoryChanged();
            isChengedBackupPairs = false;
            _ = this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DirectoryInfo GetBackupDirectory(string path)
        {
            return GetBackupData(path).BackupDirectory;
        }
        public BackupData GetBackupData(string path)
        {
            if (backupPairs.TryGetValue(path, out BackupData? backupData))
            {
                if (backupData is not null)
                {
                    return backupData;
                }
                backupPairs.Remove(path);
            }
            return Register(path);
        }

        private BackupData Register(string path)
        {
            string s;
            do
            {
                s = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString("N"));
            }
            while (Directory.Exists(s));
            BackupData backupData = new(new(s));
            backupPairs.Add(path, backupData);
            SaveIndexFile();
            return backupData;
        }

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs))]
        private void OnRootBackupDirectoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            OnRootBackupDirectoryChanged();
        }
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs), nameof(indexes))]
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
                    var pairs = JsonSerializer.Deserialize<Dictionary<string, BackupData>>(fileStream, GetJsonOptions())!;
                    indexes = [.. pairs.Select(x => x.Key)];
                    backupPairs = pairs; //pairs.ToDictionary();
                }
                catch (JsonException)
                {
                    backupPairs = [];
                    indexes = [];
                }
            }
            else
            {
                backupPairs = [];
                indexes = [];
            }
        }
        private void SaveIndexFile()
        {
            var oldFile = GetIndexFile();
            var directory = oldFile.Directory;
            if (!directory!.Exists)
            {
                directory.Create();
            }
            string oldFileName = oldFile.FullName;

            FileInfo newFile = new(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()));
            _ = newFile;
            FileStream fileStream;
            if (newFile.Exists)
            {
                fileStream = newFile.OpenWrite();
                fileStream.SetLength(0);
            }
            else
            {
                fileStream = newFile.Create();
            }
            JsonSerializer.Serialize(fileStream, backupPairs, GetJsonOptions());
            fileStream.Dispose();

            oldFile.MoveTo(Path.Combine(Path.GetDirectoryName(oldFile.FullName) ?? string.Empty, "index.tmp"), true);
            newFile.MoveTo(oldFileName, false);

            isChengedBackupPairs = false;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<IWatcher> CreateWatchers()
        {
            return indexes.Select(x => new DirectoryWatcher(x, this, backupPairs[x]));
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
