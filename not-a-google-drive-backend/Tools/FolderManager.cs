
using DatabaseModule.Entities;
using not_a_google_drive_backend.DTO.Response;
using System.Collections.Generic;
using System.Linq;

public class FolderManager
{
    public static UserFilesInfoFolder CombineFilesAndFolders(Folder folder, IEnumerable<File> files)
    {
        List<Folder> stack = new List<Folder> { folder };
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