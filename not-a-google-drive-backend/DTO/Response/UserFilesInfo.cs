using DatabaseModule.Entities;
using MongoDB.Bson;
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
    }

    public class UserFilesInfoFolder
    {
        public UserFilesInfoFolder(Folder folder)
        {
            Name = folder.Name;
            Files = Array.Empty<UserFilesInfoFile>();
            Children = Array.Empty<UserFilesInfoFolder>();
        }

        public string Name;

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
        }

        public ObjectId Id;

        public string Name;

        public int Size;

        public int Type;
    }
}
