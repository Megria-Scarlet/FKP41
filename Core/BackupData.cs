using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FKP41
{
    public class BackupData
    {
        private DirectoryInfo backupDirectory;
        private DateTime? lastBackupTime;
        private uint maxBackupCount;
        private bool isValid;

        public BackupData(DirectoryInfo backupDirectory)
        {
            this.backupDirectory = backupDirectory;
            this.maxBackupCount = 10;
            this.isValid = true;
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
                int indent = 0;
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.EndObject:
                            if (indent <= 0)
                                goto WHILEBREAK;
                            continue;
                        case JsonTokenType.StartObject:
                            indent++;
                            continue;
                        case JsonTokenType.StartArray:
                            reader.Skip();
                            continue;
                        case JsonTokenType.PropertyName:
                            string propertyName = reader.GetString() ?? string.Empty;
                            if (options.PropertyNameCaseInsensitive)
                            {

                            }
                            else
                            {

                            }
                            break;
                    }
                }
            WHILEBREAK:
                ;
            }
        }

        public override void Write(Utf8JsonWriter writer, BackupData value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
