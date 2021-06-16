
using DatabaseModule.Entities;
using not_a_google_drive_backend.DTO.Response;
using System.Collections.Generic;
using System.Linq;

public class FolderManager
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
}