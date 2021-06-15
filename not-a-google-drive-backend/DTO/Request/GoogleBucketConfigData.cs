using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Request
{
    public class GoogleBucketConfigData
    {
        public string ClientId { get; set; }

        public string Secret { get; set; }

        public string Email { get; set; }

        public string ProjectId { get; set; }

        public string SelectedBucket { get; set; }
    }
}
