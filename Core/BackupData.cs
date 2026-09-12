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
    public class BackupData
    {
        private DirectoryInfo backupDirectory;
        private DateTime? lastBackupTime;
        private uint maxBackupCount;
        private bool isValid;

        public BackupData(DirectoryInfo backupDirectory) : this(backupDirectory, 3, true) { }
        public BackupData(DirectoryInfo backupDirectory, uint maxBackupCount, bool isValid)
        {
            this.backupDirectory = backupDirectory;
            this.maxBackupCount = maxBackupCount;
            this.isValid = isValid;
            ReloadStorageData();
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
        public void ReloadStorageData()
        {
            if (backupDirectory.Exists)
            {
                System.Text.RegularExpressions.Regex regex = new(@"^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}$");

                var fileInfo = backupDirectory.EnumerateFiles("*.zip", SearchOption.TopDirectoryOnly).Where(x => regex.IsMatch(System.IO.Path.GetFileNameWithoutExtension(x.Name))).OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();

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
    }
    public class BackupDataJsonConverter : JsonConverter<BackupData>
    {
        public override BackupData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                            if (IsMatchPropertyName(propertyName, nameof(BackupData.BackupDirectory), options))
                            {
                                reader.Read();
                                backupDirectory = reader.GetString();
                            }
                            else if (IsMatchPropertyName(propertyName, nameof(BackupData.MaxBackupCount), options))
                            {
                                reader.Read();
                                if (reader.TryGetUInt32(out uint u))
                                    maxBackupCount = u;
                            }
                            else if (IsMatchPropertyName(propertyName, nameof(BackupData.IsValid), options))
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
                    return new BackupData(new DirectoryInfo(backupDirectory), maxBackupCount, isValid);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string? backupDirectory = reader.GetString();
                if (!string.IsNullOrWhiteSpace(backupDirectory))
                {
                    return new BackupData(new DirectoryInfo(backupDirectory));
                }
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, BackupData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(BackupData.BackupDirectory), value.BackupDirectory.FullName);
            writer.WriteNumber(nameof(BackupData.MaxBackupCount), value.MaxBackupCount);
            if (!value.IsValid)
            {
                writer.WriteBoolean(nameof(BackupData.IsValid), false);
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
