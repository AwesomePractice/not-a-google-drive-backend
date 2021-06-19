using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalStorageServices
{
    interface IStorageAccessor
    {
        UploadResult UploadFile(IFormFile file, string fileName, bool encryption, bool compressing);

        byte[] DownloadFile(string fileId);

        bool DeleteFile(string fileId);
    }
}
