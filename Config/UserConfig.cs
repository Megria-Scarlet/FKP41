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
        /// <summary>
        /// 全てのパラメーターが既定値かどうか。
        /// </summary>
        /// <returns>全てのパラメーターが既定値の場合は <see langword="true"/> 。</returns>
        internal bool IsDefault()
        {
            if (BackupDirectoryPath is not null)
                return false;
            return true;
        }
    }
    internal class UserConfigJsonConverter : JsonConverter<UserConfig>
    {
        public override UserConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(UserConfig))
            {

            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, UserConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
