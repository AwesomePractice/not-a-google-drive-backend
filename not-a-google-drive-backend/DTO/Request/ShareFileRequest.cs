using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Request
{
    public class ShareFileRequest
    {
        public string UserId { get; set; }

        public string FileId { get; set; }
    }
}
