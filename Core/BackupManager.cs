using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace FKP41.Core
{
    public sealed class BackupManager : IDisposable
    {

        private const string IndexFileName = "index.json";

        private ObservableObject<string> rootBackupDirectory;
        private bool disposedValue;
        private Dictionary<string, OptionsPair>? reservationDeleteBackups;

        private Dictionary<string, OptionsPair> backupPairs1;

        private List<string> indexes;
        private bool isChengedBackupPairs;

        /// <summary>
        /// <see cref="backupPairs"/> の値が変更されたかどうかを示す値を取得します。
        /// </summary>
        /// <returns><see cref="backupPairs"/> の値が変更された場合は <see langword="true"/> 。</returns>
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
        }

        #region Add・Remove

        public IWatcher Register(string path)
        {
            if (reservationDeleteBackups is not null && reservationDeleteBackups.TryGetValue(path, out var backupOptions))
            {
                // backupPairs.Add(path, backupOptions);
                backupPairs1.Add(path, backupOptions);
                reservationDeleteBackups.Remove(path);
                if (reservationDeleteBackups.Count == 0)
                    reservationDeleteBackups = null;
                return backupOptions.watcher;
            }
            string s;
            do
            {
                s = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString("N"));
            }
            while (Directory.Exists(s));
            BackupOptionsData backupData = new(new DirectoryInfo(s));
            OptionsPair pair = CreateOptionsPair(path, backupData);
            backupPairs1.Add(path, pair);
            SaveIndexFile();
            return pair.watcher;
        }

        public bool Remove(string path)
        {
            if (backupPairs1.Remove(path, out var value))
            {
                isChengedBackupPairs = true;
                if (reservationDeleteBackups is null)
                {
                    reservationDeleteBackups = new(4)
                    {
                        {path, value }
                    };
                }
                else if (!reservationDeleteBackups.TryAdd(path, value))
                {
                    reservationDeleteBackups[path] = value;
                }
                return true;
            }
            return false;
        }

        #endregion

        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs1), nameof(indexes))]
        private void OnRootBackupDirectoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            OnRootBackupDirectoryChanged();
        }
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs1), nameof(indexes))]
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
                    LoadJson(fileStream);
                }
                catch (JsonException)
                {
                    // backupPairs = [];
                    backupPairs1 = [];
                    indexes = [];
                }
            }
            else
            {
                // backupPairs = [];
                backupPairs1 = [];
                indexes = [];
            }

            [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(backupPairs1), nameof(indexes))]
            void LoadJson(Stream stream)
            {
                var pairs = JsonSerializer.Deserialize<Dictionary<string, BackupOptionsJsonData>>(stream, GetJsonOptions())!;
                int capacity = Math.Max(((pairs.Count + 3) >> 2) << 2, 16);
                this.backupPairs1 = new(capacity);

                foreach (var pair in pairs)
                {
                    BackupOptionsData data = pair.Value.ToOptions(rootBackupDirectory.Value);
                    backupPairs1.Add(pair.Key, CreateOptionsPair(pair.Key, data));
                }
                this.indexes = [.. this.backupPairs1.Keys];
            }
        }
        /// <summary>
        /// index.json ファイルに現在の情報を保存します。
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="System.Security.SecurityException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="PathTooLongException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        public void SaveIndexFile()
        {
            if (reservationDeleteBackups is not null)
            {
                foreach (var optionsData in reservationDeleteBackups.Values)
                {
                    DirectoryInfo backupDirectory = optionsData.fileData.BackupDirectory;
                    if (backupDirectory.Exists)
                    {
                        do
                        {
                            try
                            {
#if DEBUG
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(backupDirectory.FullName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
#else
                                backupDirectory.Delete(true);
#endif
                            }
                            catch (IOException e)
                            {
#if WINDOWS
                                string msg = $"\"{backupDirectory.FullName}\" の削除で IOException エラーが発生しました。\n再実行しますか？\n詳細\n{e.Message}";
                                if (System.Windows.MessageBox.Show(msg, "IOException エラー", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error) == System.Windows.MessageBoxResult.Yes)
                                {
                                    continue;
                                }
#else
                                throw;
#endif
                            }
                        }
                        while (false);
                    }
                }
                reservationDeleteBackups = null;
            }


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
                OnUpdateBackupData();
                SaveNewFile(newFile, rootBackupDirectory.Value, backupPairs1);
                oldFile.MoveTo(Path.Combine(Path.GetDirectoryName(oldFile.FullName) ?? string.Empty, Path.ChangeExtension(IndexFileName, "tmp")), true);
                newFile.MoveTo(oldFileName, false);
            }
            else
            {
                OnUpdateBackupData();
                SaveNewFile(oldFile, rootBackupDirectory.Value, backupPairs1);
            }

            isChengedBackupPairs = false;

            static void SaveNewFile(FileInfo newFileInfo, string backupDirectory, Dictionary<string, OptionsPair> backupPairs)
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
                JsonSerializer.Serialize(fileStream, backupPairs.ToDictionary(x => x.Key, x => BackupOptionsJsonData.Create(x.Key, x.Value.fileData, backupDirectory)), GetJsonOptions());
                fileStream.Dispose();
            }
        }
        private FileInfo GetIndexFile()
        {
            return new(Path.Combine(rootBackupDirectory.Value, IndexFileName));
        }

        public bool OnUpdateBackupData()
        {
            bool result = false;
            foreach (var p in backupPairs1)
            {
                var value = p.Value;
                if (!value.watcher.Options.Equals(value.fileData))
                {
                    value = value with { fileData = new(value.watcher.Options) };
                    backupPairs1[p.Key] = value;
                    result = true;
                }
            }
            return result;
        }
        public bool OnUpdateBackupData(string filePath) => OnUpdateBackupData(filePath, out _);
        internal bool OnUpdateBackupData(string filePath, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out BackupOptionsData data)
        {
            if (backupPairs1.TryGetValue(filePath, out var value) && !value.watcher.Options.Equals(value.fileData))
            {
                data = new(value.watcher.Options);
                value = value with { fileData = data };
                backupPairs1[filePath] = value;
                isChengedBackupPairs = true;
                return true;
            }
            data = null;
            return false;
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

        private OptionsPair CreateOptionsPair(string filePath, BackupOptionsData data)
        {
            DirectoryWatcher watcher = new(filePath, this, data);
            return new(watcher, data);
        }

        /// <summary>
        /// 入力された <see cref="FileInfo"/> 型のオブジェクトを列挙するオブジェクトから、バックアップディレクトリーに含まれる
        /// <see cref="FileInfo"/> 型のオブジェクトを差集合します。
        /// </summary>
        /// <param name="files"><see cref="FileInfo"/> 型のオブジェクトを列挙するオブジェクト。</param>
        /// <returns>
        /// <paramref name="files"/> からバックアップディレクトリーに含まれる <see cref="FileInfo"/>
        /// 型のオブジェクトを差集合したものを列挙するオブジェクト。</returns>
        public IEnumerable<FileInfo> RemovedBackupFiles(IEnumerable<FileInfo> files)
        {
            DirectoryInfo rootDirectory = new(this.rootBackupDirectory.Value);
            return rootDirectory.Exists ? files.Where(f => !Contains(rootDirectory, f)) : files;
        }
        /// <summary>
        /// <see cref="DirectoryInfo"/> 型のオブジェクトが示すディレクトリーに、指定した
        /// <see cref="FileInfo"/> 型のオブジェクトが示すファイルが含まれるかどうかを判定します。
        /// </summary>
        /// <param name="directoryInfo">ディレクトリーを示す <see cref="DirectoryInfo"/> 型のオブジェクト。</param>
        /// <param name="fileInfo">ファイルを示す <see cref="FileInfo"/> 型のオブジェクト。</param>
        /// <returns>
        /// <paramref name="fileInfo"/> が示すファイルが <paramref name="directoryInfo"/> が示すディレクトリーに含まれる場合は
        /// <see langword="true"/> 。それ以外の場合は <see langword="false"/> 。
        /// </returns>
        private static bool Contains(DirectoryInfo directoryInfo, FileInfo fileInfo)
        {
            string dirPath = Path.GetFullPath(directoryInfo.FullName);
            string filePath = Path.GetFullPath(fileInfo.FullName);

            // Windows の場合は末尾の区切り文字（\）を考慮して統一する
            if (!dirPath.AsSpan().EndsWith([Path.DirectorySeparatorChar], StringComparison.OrdinalIgnoreCase))
            {
                dirPath += Path.DirectorySeparatorChar;
            }

            // ファイルパスが、ディレクトリパスから始まっているかを前方一致で判定
            // ※Windows（標準）は大文字小文字を区別しないため OrdinalIgnoreCase を使用
            return filePath.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<IWatcher> GetWatchers() => backupPairs1.Values.Select(x => x.watcher);

        private void OptionsPropertyChangedCallback(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            isChengedBackupPairs = true;
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

        private struct OptionsPair
        {
            public IWatcher watcher;
            public BackupOptionsData fileData;

            public OptionsPair(IWatcher watcher, BackupOptionsData data)
            {
                this.watcher = watcher;
                this.fileData = data;
            }
        }
    }
}
