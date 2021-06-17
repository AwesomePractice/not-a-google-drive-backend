using DatabaseModule;
using DatabaseModule.Entities;
using ExternalStorageServices.GoogleBucket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using not_a_google_drive_backend.Tools;
using System.Text.Json;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
using not_a_google_drive_backend.DTO.Response;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploaderController : ControllerBase
    {
        private readonly ILogger<FileUploaderController> _logger;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;
        private readonly IMongoRepository<DatabaseModule.Entities.File> _filesRepository;

        public FileUploaderController(ILogger<FileUploaderController> logger,
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep, MongoRepository<DatabaseModule.Entities.File> fileRep)
        {
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
            _filesRepository = fileRep;
        }

        [Authorize]
        [HttpPost("UploadFile")]
        public async Task<ActionResult<string>> SendFileAsync(IFormFile fileUpload, bool compressed, bool encrypted, bool favourite, string folderId)
        {
            var formData = HttpContext.Request.Form;
            var files = HttpContext.Request.Form.Files;

            var file = files.First();

            ObjectId _folderId = new ObjectId(folderId);
            bool available = await FileFolderManager.CheckIsFolderAvailableToUser(Tools.AuthenticationManager.GetUserId(User), _folderId, _foldersRepository);
            if (!available) return BadRequest("Folder not available or doesn't exist");


            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if (user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var newFile = new DatabaseModule.Entities.File()
            {
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                OwnerId = Tools.AuthenticationManager.GetUserId(User),
                FolderId = _folderId,
                Compressed = compressed,
                Encrypted = encrypted,
                Favourite = favourite
            };
            await _filesRepository.InsertOneAsync(newFile);



            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.UploadFile(file, FileFolderManager.GetFileId(newFile, folderId));

            if (!result)
            {
                return BadRequest("Error while uploading your file!");
            }


            return Ok(JsonSerializer.Serialize(
                new UserFilesInfoFile(newFile), 
                new JsonSerializerOptions() { 
                    Converters =
                    {
                        new UserFilesInfoFileSerializer()
                    }
                }));
        }


        [Authorize]
        [HttpGet("DownloadFile")]
        public async Task<ActionResult> DownloadFileAsync(string fileId)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);



            var user = await _usersRepository.FindOneAsync(x => x.Id == userId);
            if (user.GoogleBucketConfigData == null)
            {
                return BadRequest("You have not linked any cloud storage");
            }


            var serviceConfig = user.GoogleBucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DownloadFile(fileId);
           
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(fileId, out contentType);

            return File(result, contentType, fileId);
        }

        [Authorize]
        [HttpPost("FileInfo")]
        public async Task<ActionResult> GetFileInfo(ObjectIdRequest fileIdRequest)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);
            var file = await _filesRepository.FindByIdAsync(fileIdRequest.Id);

            if(!FileFolderManager.CanAccessFile(userId, file)) {
                return BadRequest("You don't have access to file or it doesn't exist");
            }

            return Ok(JsonSerializer.Serialize(
                new UserFilesInfoFile(file),
                new JsonSerializerOptions()
                {
                    Converters =
                    {
                        new UserFilesInfoFileSerializer()
                    }
                }));
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
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DeleteFile(fileId);

            if (!result)
            {
                return BadRequest("Error while deleting your file");
            }

            return Ok("File was deleted");
        }

        [Authorize]
        [HttpPost("SwitchFavouriteFile")]
        public async Task<ActionResult> SwitchFavouriteFile(FavouriteSwitch favouriteSwitch)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);
            var file = await _filesRepository.FindByIdAsync(favouriteSwitch.FileId);

            if (!FileFolderManager.CanAccessFile(userId, file))
            {
                return BadRequest("You don't have access to file or it doesn't exist");
            }

            await _filesRepository.UpdateOneAsync(favouriteSwitch.FileId.ToString(), "Favourite", favouriteSwitch.IsFavourite);
            file.Favourite = favouriteSwitch.IsFavourite;

            return Ok(JsonSerializer.Serialize(
            new UserFilesInfoFile(file),
            new JsonSerializerOptions()
            {
                Converters =
                {
                        new UserFilesInfoFileSerializer()
                }
            }));
        }

            //[Authorize]
            //[HttpGet("GetAllBucketFiles")]
            //public async Task<ActionResult<List<string>>> GetAllBucketFilesAsync()
            //{

            //    var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            //    if (user.GoogleBucketConfigData == null)
            //    {
            //        return BadRequest("You have not linked any cloud storage");
            //    }


            //    var serviceConfig = user.GoogleBucketConfigData;
            //    var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.Email, serviceConfig.ProjectId,
            //        serviceConfig.ClientId, serviceConfig.Secret, serviceConfig.SelectedBucket);
            //    var result = googleBucketUploader.GetFilesList();

            //    return Ok(result);
            //}

        }
}
