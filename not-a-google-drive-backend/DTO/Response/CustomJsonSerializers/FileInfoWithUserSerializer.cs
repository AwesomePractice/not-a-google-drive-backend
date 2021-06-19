

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace not_a_google_drive_backend.DTO.Response.CustomJsonSerializers
{
    public class FileInfoWithUserSerializer : JsonConverter<FileInfoWithUser>
    {
        public override FileInfoWithUser Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, FileInfoWithUser value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Id.ToString());
            writer.WriteString("name", value.Name);
            writer.WriteNumber("size", value.Size);
            writer.WriteString("type", value.Type);
            writer.WriteBoolean("encrypted", value.Encrypted);
            writer.WriteBoolean("compressed", value.Compressed);
            writer.WriteBoolean("favourite", value.Favourite);
            writer.WriteString("owner_id", value.OwnerId);
            writer.WriteEndObject();
        }
    }

}