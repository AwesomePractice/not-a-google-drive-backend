
using DatabaseModule;
using DatabaseModule.Entities;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Tools
{
    public class FileFolderManager
    {
        public static UserFilesInfoFolder CombineFilesAndFolders(List<Folder> folders, IEnumerable<File> files)
        {
            var rootFolder = folders.First(f => f.ParentId == null);
            List<Folder> stack = new List<Folder> { rootFolder };
            List<UserFilesInfoFolder> stackDto = new List<UserFilesInfoFolder> { new UserFilesInfoFolder(rootFolder) };
            UserFilesInfoFolder result = stackDto.First();
            while (stack.Count > 0)
            {
                Folder f = stack.First();
                UserFilesInfoFolder fDto = stackDto.First();
                stack.RemoveAt(0);
                stackDto.RemoveAt(0);

                var childrenFolders = folders.Where(folder => folder.ParentId == f.Id);

                fDto.Children = childrenFolders
                    .Select(folder => new UserFilesInfoFolder(folder))
                    .ToArray();
                fDto.Files = files
                    .Where(file => file.FolderId == f.Id)
                    .Select(
                        _file => new UserFilesInfoFile(_file)
                    ).ToArray();

                stack.AddRange(childrenFolders);
                stackDto.AddRange(fDto.Children);
            }
            return result;
        }

        public static async Task<bool> CheckIsFolderAvailableToUser(ObjectId userId, ObjectId folderId,
            IMongoRepository<Folder> foldersRepository)
        {
            var folder = await foldersRepository.FindByIdAsync(folderId.ToString());
            return !(folder == null || folder.OwnerId != userId);
        }

        //internal static string GetFileId(File file, string folderId)
        //{
        //    return folderId.ToString() + "_" + file.Id.ToString();
        //}

        internal static bool CanAccessFile(ObjectId userId, File file)
        {
            return file.OwnerId == userId;
        }

        internal static bool CanDeleteFile(ObjectId userId, File file)
        {
            throw new NotImplementedException();
        }
    }
}