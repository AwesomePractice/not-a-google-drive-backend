using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploaderController : ControllerBase
    {

        private readonly ILogger<FileUploaderController> _logger;

        public FileUploaderController(ILogger<FileUploaderController> logger)
        {
            _logger = logger;
        }


        [HttpPost("Uploadfile")]
        public async Task<ActionResult<FileUploadResult>> SendFileAsync(IFormFile file)
        {
            string filePath = Path.Combine("saved_files", file.FileName);
            using (Stream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return Ok(new FileUploadResult() { Date = DateTime.Now, Result="Your file was uploded"});
        }

        [HttpGet("ShowUploadedFiles")]
        public async Task<ActionResult<ListOfFiles>> GetListOfFiles()
        {
            string[] filePaths = Directory.GetFiles("saved_files");
            return Ok(new ListOfFiles() { Filenames = filePaths.ToList()});
        }
    }
}
