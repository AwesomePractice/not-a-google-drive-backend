using DatabaseModule.Entities;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Response
{
    public class UserFilesInfo
    {
        //[JsonConverter(typeof(ObjectIdSerializer))]
        public ObjectId OwnerId;

        //[JsonConverter(typeof(UserFilesInfoFolder))]
        public UserFilesInfoFolder RootFolder;

        public List<UserFilesInfoFile> AvailableFiles;
    }

    public class UserFilesInfoFolder
    {
        public UserFilesInfoFolder(Folder folder)
        {
            Name = folder.Name;
            Files = Array.Empty<UserFilesInfoFile>();
            Children = Array.Empty<UserFilesInfoFolder>();
            Id = folder.Id;
            IsFavourite = folder.Favourite;
        }

        public string Name;

        public ObjectId Id;

        public bool IsFavourite;

        public UserFilesInfoFile[] Files;

        public UserFilesInfoFolder[] Children;
    }

    public class UserFilesInfoFile
    {
        public UserFilesInfoFile(File file)
        {
            Id = file.Id;
            Name = file.FileName;
            Size = file.FileSize;
            Type = file.FileType;
            Encrypted = file.Encrypted;
            Compressed = file.Compressed;
            Favourite = file.Favourite;
        }

        //[JsonConverter(typeof(ObjectId))]
        public ObjectId Id;

        public string Name;

        public long Size;

        public string Type;

        public bool Encrypted;

        public bool Compressed;

        public bool Favourite;
    }
}
