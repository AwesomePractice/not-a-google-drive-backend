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

        public List<FileInfoWithUser> AvailableFiles;
    }

    public class SimpleFolder
    {
        public SimpleFolder(Folder folder)
        {
            Name = folder.Name;
            Id = folder.Id.ToString();
            IsFavourite = folder.Favourite;
        }

        public string Name { get; set; }

        public string Id { get; set; }

        public bool IsFavourite { get; set; }
    }

    public class UserFilesInfoFolder : SimpleFolder
    {
        public UserFilesInfoFolder(Folder folder) : base(folder)
        {
            Files = Array.Empty<UserFilesInfoFile>();
            Children = Array.Empty<UserFilesInfoFolder>();
        }

        public UserFilesInfoFile[] Files;

        public UserFilesInfoFolder[] Children;
    }

    public class UserFilesInfoFile
    {
        public UserFilesInfoFile(File file)
        {
            Id = file.Id.ToString();
            Name = file.FileName;
            Size = file.FileSize;
            Type = file.FileType;
            Encrypted = file.Encrypted;
            Compressed = file.Compressed;
            Favourite = file.Favourite;
        }

        //[JsonConverter(typeof(ObjectId))]
        public string Id;

        public string Name;

        public long Size;

        public string Type;

        public bool Encrypted;

        public bool Compressed;

        public bool Favourite;
    }

    public class FileInfoWithUser : UserFilesInfoFile
    {
        public FileInfoWithUser(File file) : base(file)
        {
            OwnerId = file.OwnerId.ToString();
        }

        public string OwnerId;
    }
}
