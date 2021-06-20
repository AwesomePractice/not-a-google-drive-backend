using DatabaseModule;
using DatabaseModule.Entities;
using ExternalStorageServices.GoogleBucket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using not_a_google_drive_backend.Tools;
using System.Text.Json;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
using not_a_google_drive_backend.DTO.Response;
using System.IO;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IMongoRepository<DatabaseModule.Entities.Bucket> _bucketsRepository;

        public FileUploaderController(ILogger<FileUploaderController> logger,
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep, MongoRepository<DatabaseModule.Entities.File> fileRep,
            MongoRepository<DatabaseModule.Entities.Bucket> bucketRep)
        {
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
            _filesRepository = fileRep;
            _bucketsRepository = bucketRep;
        }

        [Authorize]
        [HttpPost("UploadFile")]
        public async Task<ActionResult<string>> SendFileAsync(IFormFile fileUpload, bool compressed, bool encrypted, bool favourite, string folderId)
        {
            var formData = HttpContext.Request.Form;
            var files = HttpContext.Request.Form.Files;

            var file = files.First();

            ObjectId _folderId = new ObjectId(folderId);
            bool available = await FileFolderManager.CanAccessFolder(Tools.AuthenticationManager.GetUserId(User), _folderId, _foldersRepository);
            if (!available) return BadRequest("Folder not available or doesn't exist");


            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if (user.Buckets.Count() == 0)
            {
                return BadRequest("You have not linked any cloud storage");
            }

            ObjectId fileId = ObjectId.GenerateNewId();
            var serviceConfig = user.CurrentBucket.BucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.UploadFile(file, fileId.ToString(), encrypted, compressed);

            if (!result.Success)
            {
                return BadRequest("Error while uploading your file!");
            }

            var newFile = new DatabaseModule.Entities.File()
            {
                Id = fileId,
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                OwnerId = Tools.AuthenticationManager.GetUserId(User),
                FolderId = _folderId,
                Compressed = compressed,
                Encrypted = encrypted,
                EncryptionKey = result.EncryptionKey,
                IV = result.IV,
                Favourite = favourite,
                BucketId = user.CurrentBucket.Id,
                AllowedUsers = new List<ObjectId>()
            };
            await _filesRepository.InsertOneAsync(newFile);


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

            #region
            DatabaseModule.Entities.File file;
            try
            {
                file = await _filesRepository.FindByIdAsync(fileId);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest("File not found");
            }
            #endregion

            if (!FileFolderManager.CanAccessFile(userId, file))
            {
                return BadRequest("User doesn't have access to file");
            }

            #region Get file from db
            DatabaseModule.Entities.Bucket bucket;
            try
            {
                bucket = await _bucketsRepository.FindByIdAsync(file.BucketId.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest("Bucket not found");
            }
            #endregion

            var serviceConfig = bucket.BucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DownloadFile(file);
          

            return File(result, file.FileType, file.FileName);
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
                new FileInfoWithUser(file),
                new JsonSerializerOptions()
                {
                    Converters =
                    {
                        new FileInfoWithUserSerializer()
                    }
                }));
        }

        [Authorize]
        [HttpPost("DeleteFile")]
        public async Task<ActionResult> DeleteFileAsync(ObjectIdRequest request)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);

            #region Get file from db
            DatabaseModule.Entities.File file;
            try
            {
                file = await _filesRepository.FindByIdAsync(request.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest("File not found");
            }
            #endregion

            if (!FileFolderManager.CanDeleteFile(userId, file))
            {
                return BadRequest("You can't delete file or it doesn't exist");
            }

            #region Get bucket from db
            DatabaseModule.Entities.Bucket bucket;
            try
            {
                bucket = await _bucketsRepository.FindByIdAsync(file.BucketId.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest("Bucket not found");
            }
            #endregion


            var serviceConfig = bucket.BucketConfigData;
            var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
            var result = googleBucketUploader.DeleteFile(request.Id);

            if (!result)
            {
                return BadRequest("Error while deleting your file");
            }

            #region Delete file from db
            try
            {
                await _filesRepository.DeleteByIdAsync(request.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest("Deletion file from db failed");
            }
            #endregion

            return Ok();
        }

        [Authorize]
        [HttpPost("SwitchFavouriteFile")]
        public async Task<ActionResult> SwitchFavouriteFile(FavouriteSwitch favouriteSwitch)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);
            var file = await _filesRepository.FindByIdAsync(favouriteSwitch.Id);

            if (!FileFolderManager.CanDeleteFile(userId, file))
            {
                return BadRequest("You don't have access to file or it doesn't exist");
            }

            await _filesRepository.UpdateOneAsync(favouriteSwitch.Id.ToString(), "Favourite", favouriteSwitch.IsFavourite);
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

        [Authorize]
        [HttpGet("AllFavouriteFiles")]
        public async Task<ActionResult<List<FileInfo>>> AllFavouriteFiles()
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);

            var files = await _filesRepository.FilterByAsync(file => file.OwnerId == userId && file.Favourite);

            return Ok(JsonSerializer.Serialize(
                files.Select(f => new UserFilesInfoFile(f)),
                new JsonSerializerOptions()
                    {
                        Converters =
                        {
                            new UserFilesInfoFileSerializer()
                        }
                    }
                ));
        }

        [Authorize]
        [HttpGet("AllMyFiles")]
        public async Task<ActionResult<List<string>>> AllMyFiles()
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);

            var files = (await _filesRepository.FilterByAsync(file => file.OwnerId == userId)).ToList();

            return Ok(JsonSerializer.Serialize(
                files.Select(f => new FileInfoWithUser(f)),
                new JsonSerializerOptions()
                {
                    Converters =
                    {
                        new FileInfoWithUserSerializer()
                    }
                }
                ));
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
