using DatabaseModule;
using DatabaseModule.Entities;
using ExternalStorageServices.GoogleBucket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploaderController : ControllerBase
    {
        private readonly ILogger<FileUploaderController> _logger;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;

        public FileUploaderController(ILogger<FileUploaderController> logger,
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep)
        {
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
        }

        [Authorize]
        [HttpPost("UploadFile")]
        public async Task<ActionResult<string>> SendFileAsync(IFormFile file)
        {
            var formData = HttpContext.Request.Form;
            var files = HttpContext.Request.Form.Files;
            

            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if(user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.Email, serviceConfig.ProjectId, 
                serviceConfig.ClientId, serviceConfig.Secret, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.UploadFile(files.First());
           

            if (!result)
            {
                return BadRequest("Error while uploading your file!");
            }
            return Ok("File uploaded succesfully");
        }


        [Authorize]
        [HttpGet("DownloadFile")]
        public async Task<ActionResult> DownloadFileAsync(string fileId)
        {

            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if (user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.Email, serviceConfig.ProjectId,
                serviceConfig.ClientId, serviceConfig.Secret, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DownloadFile(fileId);
           
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(fileId, out contentType);

            return File(result, contentType, fileId);
        }

        [Authorize]
        [HttpGet("DeleteFile")]
        public async Task<ActionResult> DeleteFileAsync(string fileId)
        {

            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if (user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.Email, serviceConfig.ProjectId,
                serviceConfig.ClientId, serviceConfig.Secret, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DeleteFile(fileId);

            if (!result)
            {
                return BadRequest("Error while deleting your file");
            }

            return Ok("File was deleted");
        }


        [Authorize]
        [HttpGet("GetAllBucketFiles")]
        public async Task<ActionResult<List<string>>> GetAllBucketFilesAsync()
        {

            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if (user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.Email, serviceConfig.ProjectId,
                serviceConfig.ClientId, serviceConfig.Secret, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.GetFilesList();

            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("GetMyUsername")]
        public async Task<ActionResult<string>> GetUsername()
        {

            return Ok(User.Identity.Name);
        }
    }
}
