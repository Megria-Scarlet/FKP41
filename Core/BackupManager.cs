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
        private Dictionary<string, BackupOptionsData> backupPairs;
        private List<string> indexes;
        private bool isChengedBackupPairs;

        public bool IsChengedBackupPairs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isChengedBackupPairs;
        }

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
        public BackupOptionsData GetBackupData(string path)
        {
            if (backupPairs.TryGetValue(path, out BackupOptionsData? backupData))
            {
                if (backupData is not null)
                {
                    return backupData;
                }
                backupPairs.Remove(path);
            }
            return Register(path);
        }

        private BackupOptionsData Register(string path)
        {
            string s;
            do
            {
                s = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString("N"));
            }
            while (Directory.Exists(s));
            BackupOptionsData backupData = new(new DirectoryInfo(s));
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
                    var pairs = JsonSerializer.Deserialize<Dictionary<string, BackupOptionsData>>(fileStream, GetJsonOptions())!;
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
        public void SaveIndexFile()
        {
            var oldFile = GetIndexFile();
            var directory = oldFile.Directory;
            if (!directory!.Exists)
            {
                directory.Create();
            }

            if (oldFile.Exists)
            {
                string oldFileName = oldFile.FullName;
                FileInfo newFile = new(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()));
                SaveNewFile(newFile);
                oldFile.MoveTo(Path.Combine(Path.GetDirectoryName(oldFile.FullName) ?? string.Empty, "index.tmp"), true);
                newFile.MoveTo(oldFileName, false);
            }
            else
            {
                SaveNewFile(oldFile);
            }

            isChengedBackupPairs = false;

            void SaveNewFile(FileInfo newFileInfo)
            {
                FileStream fileStream;
                if (newFileInfo.Exists)
                {
                    fileStream = newFileInfo.OpenWrite();
                    fileStream.SetLength(0);
                }
                else
                {
                    fileStream = newFileInfo.Create();
                }
                JsonSerializer.Serialize(fileStream, backupPairs, GetJsonOptions());
                fileStream.Dispose();
            }
        }
        private FileInfo GetIndexFile()
        {
            return new(Path.Combine(rootBackupDirectory.Value, "index.json"));
        }

        /// <summary>
        /// 指定したファイルパスの <see cref="BackupOptionsData"/> を設定します。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="backupData">設定する <see cref="BackupOptionsData"/> 型のオブジェクト。</param>
        /// <param name="isAdd">ファイルパスが存在しない場合、新規に追加する場合は <see langword="true"/> 。</param>
        /// <returns>正常に設定できた場合は <see langword="true"/> 。</returns>
        public bool SetBackupData(string filePath, BackupOptionsData backupData, bool isAdd = true)
        {
            if (backupPairs.TryGetValue(filePath, out var value))
            {
                if (!backupData.Equals(value))
                {
                    backupPairs[filePath] = backupData;
                    isChengedBackupPairs = true;
                    return true;
                }
                return false;
            }
            else if (isAdd)
            {
                backupPairs.Add(filePath, backupData);
                indexes.Add(filePath);
                isChengedBackupPairs = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// このオブジェクトが保持する <see cref="BackupOptionsData"/> 型のオブジェクトを、
        /// <see cref="IWatcher"/> オブジェクトに関連付けされている <see cref="BackupOptionsData"/> 型のオブジェクトに更新します。
        /// </summary>
        /// <param name="watchers"></param>
        /// <returns>いずれかの <see cref="BackupOptionsData"/> 型のオブジェクトを更新した場合は <see langword="true"/> 。</returns>
        public bool OnUpdateBackupData(IEnumerable<IWatcher> watchers)
        {
            bool result = false;
            foreach (var watcher in watchers)
            {
                result |= SetBackupData(watcher.FilePath, watcher.Options, false);
            }
            return result;
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
