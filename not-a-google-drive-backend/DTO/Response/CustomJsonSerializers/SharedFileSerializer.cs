

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace not_a_google_drive_backend.DTO.Response.CustomJsonSerializers
{
    public class SharedFileSerializer : JsonConverter<SharedFileInfo>
    {
        public override SharedFileInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SharedFileInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Id.ToString());
            writer.WriteString("name", value.Name);
            writer.WriteNumber("size", value.Size);
            writer.WriteString("type", value.Type);
            writer.WriteBoolean("encrypted", value.Encrypted);
            writer.WriteBoolean("compressed", value.Compressed);
            writer.WriteBoolean("favourite", value.Favourite);
            writer.WritePropertyName("allowed_users");
            writer.WriteStartArray();
            foreach (var userId in value.AllowedUsers)
            {
                writer.WriteStringValue(userId);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

}