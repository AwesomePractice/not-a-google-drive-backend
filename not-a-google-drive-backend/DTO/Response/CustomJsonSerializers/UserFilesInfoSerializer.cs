using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Response;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace not_a_google_drive_backend.DTO.Response.CustomJsonSerializers
{
    public class UserFilesInfoSerializer : JsonConverter<UserFilesInfo>
    {
        public override UserFilesInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, UserFilesInfo value, JsonSerializerOptions options)
        {
            //ObjectIdSerializer objectIdSerializer = new ObjectIdSerializer();
            UserFilesInfoFolderSerializer folderSerializer = new UserFilesInfoFolderSerializer();
            UserFilesInfoFileSerializer fileSerializer = new UserFilesInfoFileSerializer();

            writer.WriteStartObject();
            writer.WriteString("owner_id", value.OwnerId.ToString());
            //objectIdSerializer.Write(writer, value.OwnerId, new JsonSerializerOptions());
            writer.WritePropertyName("root_folder");
            folderSerializer.Write(writer, value.RootFolder, new JsonSerializerOptions());
            writer.WritePropertyName("available_files");
            writer.WriteStartArray();
            foreach (var file in value.AvailableFiles) {
                fileSerializer.Write(writer, file, new JsonSerializerOptions());
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    public class UserFilesInfoFolderSerializer : JsonConverter<UserFilesInfoFolder>
    {
        public override UserFilesInfoFolder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, UserFilesInfoFolder value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Id.ToString());
            writer.WriteBoolean("favourite", value.IsFavourite);
            writer.WriteString("name", value.Name);
            writer.WritePropertyName("children");
            writer.WriteStartArray();
            foreach(UserFilesInfoFolder child in value.Children)
            {
                Write(writer, child, new JsonSerializerOptions());
            }
            writer.WriteEndArray();
            
            UserFilesInfoFileSerializer fileSerializer = new UserFilesInfoFileSerializer();
            writer.WritePropertyName("files");
            writer.WriteStartArray();
            foreach (UserFilesInfoFile file in value.Files)
            {
                fileSerializer.Write(writer, file, new JsonSerializerOptions());
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }

    public class UserFilesInfoFileSerializer : JsonConverter<UserFilesInfoFile>
    {
        public override UserFilesInfoFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, UserFilesInfoFile value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Id.ToString());
            writer.WriteString("name", value.Name);
            writer.WriteNumber("size", value.Size);
            writer.WriteString("type", value.Type);
            writer.WriteBoolean("encrypted", value.Encrypted);
            writer.WriteBoolean("compressed", value.Compressed);
            writer.WriteBoolean("favourite", value.Favourite);
            writer.WriteEndObject();
        }
    }
}