using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Response
{
    public class Bucket
    {
        public Bucket() { }
        public string Id { get; set; }
        public string Name { get; set; }

        public bool Current { get; set; }

    }
}
