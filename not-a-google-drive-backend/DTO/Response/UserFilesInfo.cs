using DatabaseModule.Entities;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Response
{
    public class UserFilesInfo
    {
        public ObjectId OwnerId;

        public UserFilesInfoFolder RootFolder;

        internal static UserFilesInfoFolder CombineFilesAndFolders(Folder folder, IEnumerable<File> files)
        {
            List<Folder> stack = new List<Folder>{ folder };
            List<UserFilesInfoFolder> stackDto = new List<UserFilesInfoFolder> { new UserFilesInfoFolder(folder) };
            UserFilesInfoFolder result = stackDto.First();
            while (stack.Count > 0)
            {
                Folder f = stack.First();
                UserFilesInfoFolder fDto = stackDto.First();
                stack.RemoveAt(0);
                stackDto.RemoveAt(0);

                fDto.Children = f.Children.Select(child => new UserFilesInfoFolder(child)).ToArray();
                fDto.Files = files
                    .Where(file => file.FolderId == f.Id)
                    .Select(
                        _file => new UserFilesInfoFile(_file)
                    ).ToArray();

                stack.AddRange(f.Children);
                stackDto.AddRange(fDto.Children);
            }
            return result;
        }
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
