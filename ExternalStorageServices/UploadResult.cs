using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalStorageServices
{
    public class UploadResult
    {
        public UploadResult() { }

        public UploadResult(bool status, string key, string iv)
        {
            Success = status;
            EncryptionKey = key;
            IV = iv;
        }

        public bool Success { get; set; }

        public string EncryptionKey { get; set; }

        public string IV { get; set; }
    }
}
