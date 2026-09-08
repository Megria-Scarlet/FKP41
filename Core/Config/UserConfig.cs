using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FKP41.Config
{
    [JsonConverter(typeof(UserConfigJsonConverter))]
    public class UserConfig
    {
        public string? BackupDirectoryPath
        {
            get;
            set;
        }
    }
    internal class UserConfigJsonConverter : JsonConverter<UserConfig>
    {
        public override UserConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                JsonElement element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                UserConfig userConfig = new UserConfig();
                foreach (JsonProperty jsonProperty in element.EnumerateObject())
                {
                    if (options.PropertyNameCaseInsensitive)
                    {
                        if (string.Equals(jsonProperty.Name, nameof(UserConfig.BackupDirectoryPath), StringComparison.InvariantCultureIgnoreCase))
                        {
                            userConfig.BackupDirectoryPath = jsonProperty.Value.GetString();
                        }
                    }
                }
                return userConfig;
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, UserConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.BackupDirectoryPath is not null)
                writer.WriteString("BackupDirectoryPath", value.BackupDirectoryPath);
            writer.WriteEndObject();
        }
    }
}
