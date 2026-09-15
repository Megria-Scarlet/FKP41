using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FKP41
{
    [JsonConverter(typeof(BackupDataJsonConverter))]
    public partial class BackupOptionsData : IEquatable<BackupOptionsData>, System.Numerics.IEqualityOperators<BackupOptionsData, BackupOptionsData, bool>
    {
        protected DirectoryInfo backupDirectory;
        protected DateTime? lastBackupTime;
        protected uint maxBackupCount;
        protected bool isValid;

        public BackupOptionsData(DirectoryInfo backupDirectory) : this(backupDirectory, 3, true) { }
        public BackupOptionsData(DirectoryInfo backupDirectory, uint maxBackupCount, bool isValid)
        {
            this.backupDirectory = backupDirectory;
            this.maxBackupCount = maxBackupCount;
            this.isValid = isValid;
            ReloadStorageData();
        }
        public BackupOptionsData(BackupOptionsData backupData)
        {
            this.backupDirectory = backupData.backupDirectory;
            this.maxBackupCount = backupData.maxBackupCount;
            this.isValid = backupData.isValid;
            this.lastBackupTime = backupData.lastBackupTime;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isValid;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => isValid = value;
        }
        public DirectoryInfo BackupDirectory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => backupDirectory;
        }
        public uint MaxBackupCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => maxBackupCount;
        }
        public DateTime? LastBackupTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => lastBackupTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => Equals(obj as BackupOptionsData);

        public virtual bool Equals(BackupOptionsData? other)
        {
            return other is not null && other.GetType() == this.GetType() && ValueEquals(other, this);
        }

        protected static bool ValueEquals(BackupOptionsData value1, BackupOptionsData value2)
        {
            return value1.isValid == value2.isValid
                   && value1.maxBackupCount == value2.maxBackupCount
                   && (ReferenceEquals(value1.backupDirectory, value2.backupDirectory)
                       || string.Equals(value1.backupDirectory.FullName, value2.backupDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(backupDirectory, lastBackupTime, maxBackupCount, isValid);
        }

        public void ReloadStorageData()
        {
            if (backupDirectory.Exists)
            {
                var fileInfo = GetBackupFiles().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();

                if (fileInfo is null)
                {
                    lastBackupTime = null;
                }
                else
                {
                    lastBackupTime = fileInfo.LastWriteTime;
                }
            }
            else
            {
                lastBackupTime = null;
            }
        }

        public void DeleteMostOldBackupFiles(int deleteMaxCount, bool isSendToRecycleBin)
        {
            if (deleteMaxCount > 0)
            {
                FileInfo[] backupFiles = [.. GetBackupFiles().OrderBy(f => f.LastWriteTimeUtc)];
                uint maxBackupCount = Math.Max(this.maxBackupCount, 1);
                if ((uint)backupFiles.Length > maxBackupCount)
                {
                    Span<FileInfo> span = backupFiles.AsSpan(0, (int)Math.Min((uint)backupFiles.Length - maxBackupCount, (uint)deleteMaxCount));
                    if (isSendToRecycleBin)
                    {
                        foreach (FileInfo fileInfo in span)
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fileInfo.FullName, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        }
                    }
                    else
                    {
                        foreach (FileInfo fileInfo in span)
                        {
                            fileInfo.Delete();
                        }
                    }
                }
            }
        }

        public void CreateBackupArchive(DateTime createTime, IEnumerable<(string filePath, string archivePath)> files)
        {
            if (!backupDirectory.Exists)
                backupDirectory.Create();
            string archiveName = Path.Combine(backupDirectory.FullName, $"{createTime:yyyy-MM-ddTHH-mm-ss}.zip" );

            var archive = System.IO.Compression.ZipFile.Open(archiveName, System.IO.Compression.ZipArchiveMode.Create);

            foreach (var (filePath, archivePath) in files)
            {
                _ = System.IO.Compression.ZipFileExtensions.CreateEntryFromFile(archive, filePath, archivePath, System.IO.Compression.CompressionLevel.SmallestSize);
            }
            archive.Dispose();
        }

        public IEnumerable<FileInfo> GetBackupFiles()
        {
            System.Text.RegularExpressions.Regex regex = BackupFileRegex();
            return backupDirectory.EnumerateFiles("*.zip", SearchOption.TopDirectoryOnly).Where(x => regex.IsMatch(Path.GetFileNameWithoutExtension(x.Name)));
        }

        public static bool operator ==(BackupOptionsData? left, BackupOptionsData? right)
        {
            return EqualityComparer<BackupOptionsData>.Default.Equals(left, right);
        }

        public static bool operator !=(BackupOptionsData? left, BackupOptionsData? right)
        {
            return !(left == right);
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}$")]
        public static partial System.Text.RegularExpressions.Regex BackupFileRegex();
    }
    public class BackupDataJsonConverter : JsonConverter<BackupOptionsData>
    {
        public override BackupOptionsData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string? backupDirectory = null;
                uint maxBackupCount = 3;
                bool isValid = true;

                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.EndObject:
                            goto WHILEBREAK;
                        case JsonTokenType.PropertyName:
                            string? propertyName = reader.GetString();
                            if (IsMatchPropertyName(propertyName, nameof(BackupOptionsData.BackupDirectory), options))
                            {
                                reader.Read();
                                backupDirectory = reader.GetString();
                            }
                            else if (IsMatchPropertyName(propertyName, nameof(BackupOptionsData.MaxBackupCount), options))
                            {
                                reader.Read();
                                if (reader.TryGetUInt32(out uint u))
                                    maxBackupCount = u;
                            }
                            else if (IsMatchPropertyName(propertyName, nameof(BackupOptionsData.IsValid), options))
                            {
                                reader.Read();
                                if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
                                    isValid = reader.GetBoolean();
                            }
                            break;
                        default:
                            reader.Skip();
                            continue;
                    }
                }
            WHILEBREAK:

                if (backupDirectory is not null)
                    return new BackupOptionsData(new DirectoryInfo(backupDirectory), maxBackupCount, isValid);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string? backupDirectory = reader.GetString();
                if (!string.IsNullOrWhiteSpace(backupDirectory))
                {
                    return new BackupOptionsData(new DirectoryInfo(backupDirectory));
                }
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, BackupOptionsData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(BackupOptionsData.BackupDirectory), value.BackupDirectory.FullName);
            writer.WriteNumber(nameof(BackupOptionsData.MaxBackupCount), value.MaxBackupCount);
            if (!value.IsValid)
            {
                writer.WriteBoolean(nameof(BackupOptionsData.IsValid), false);
            }
            writer.WriteEndObject();
        }


        private static bool IsMatchPropertyName([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? propertyName0, string propertyName1, JsonSerializerOptions? options)
        {
            if (options is null || !options.PropertyNameCaseInsensitive)
            {
                return propertyName0 == propertyName1;
            }
            else
            {
                return string.Equals(propertyName0, propertyName1, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
