using DatabaseModule.Entities;
using Microsoft.AspNetCore.Http;

namespace ExternalStorageServices
{
    interface IStorageAccessor
    {
        UploadResult UploadFile(IFormFile file, string fileName, bool encryption, bool compressing);

        byte[] DownloadFile(File fileInfo);

        bool DeleteFile(string fileId);
    }
}
