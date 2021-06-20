using DatabaseModule;
using DatabaseModule.Entities;
using ExternalStorageServices.GoogleBucket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using not_a_google_drive_backend.DTO.Response;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
using not_a_google_drive_backend.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FolderController : ControllerBase
    {
        private readonly ILogger<FileUploaderController> _logger;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;
        private readonly IMongoRepository<DatabaseModule.Entities.File> _filesRepository;
        private readonly IMongoRepository<DatabaseModule.Entities.Bucket> _bucketsRepository;

        public FolderController(
            ILogger<FileUploaderController> logger,
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
        [HttpPost("CreateFolder")]
        public async Task<ActionResult<String>> CreateFolder(CreateFolder createFolder)
        {
            ObjectId parentId = new ObjectId(createFolder.ParentId);
            ObjectId userId = AuthenticationManager.GetUserId(User);

            var parent = await _foldersRepository.FindOneAsync(folder => folder.Id == parentId && folder.OwnerId == userId);
            if (parent == null)
            {
                return BadRequest("There's no such parent");
            }

            var siblings = await _foldersRepository.FilterByAsync(folder => folder.ParentId == parentId && folder.OwnerId == userId);
            if(siblings.Any(f => f.Name == createFolder.Name))
            {
                return BadRequest("There's folder with such name already");
            }
            Folder folder = new Folder(createFolder.Name, userId, parentId, createFolder.IsFavourite);
            await _foldersRepository.InsertOneAsync(folder);

            var options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new UserFilesInfoFolderSerializer()
                }
            };
            return Ok(JsonSerializer.Serialize(new UserFilesInfoFolder(folder), options));
        }


        [Authorize]
        [HttpPost("DeleteFolder")]
        public async Task<ActionResult> DeleteFolder(DTO.Request.ObjectIdRequest request)
        {
            var userId = AuthenticationManager.GetUserId(User);

            if (!await FileFolderManager.CanDeleteFolder(userId, new ObjectId(request.Id), _foldersRepository))
            {
                return BadRequest("You don't have right to delete folder, or it doesn't exist");
            }
            var foldersTree = await FileFolderManager.GetFolderTree(new ObjectId(request.Id), _foldersRepository);


            var buckets = (await _bucketsRepository.FilterByAsync(bucket => bucket.OwnerId == null || bucket.OwnerId == userId)).ToList();

            var files = (await _filesRepository.FilterByAsync(file => foldersTree.Contains(file.FolderId))).ToList();

            if (files.Count() != 0) {
                foreach (var file in files) {
                    var bucket = buckets.First(b => b.Id == file.BucketId);

                    var serviceConfig = bucket.BucketConfigData;
                    var googleBucketUploader = new RequestHandlerGoogleBucket(serviceConfig.ConfigData, serviceConfig.SelectedBucket);
                    var result = googleBucketUploader.DeleteFile(file.Id.ToString());

                    if (!result)
                    {
                        _logger.LogError("Error while deleting file " + file.Id + " during deletion folder " + request.Id);
                    }
                }
                var filesIds = files.Select(f_ => f_.Id);
                await _filesRepository.DeleteManyAsync(f => filesIds.Contains(f.Id));
            }

            await _foldersRepository.DeleteManyAsync(folder => foldersTree.Contains(folder.Id));

            return Ok();
        }

        
        [Authorize]
        [HttpGet("AllFavouriteFolders")]
        public async Task<ActionResult<List<string>>> AllFavouriteFolders()
        {
            var userId = AuthenticationManager.GetUserId(User);

            var favouriteFolders = (await _foldersRepository.FilterByAsync(folder => folder.OwnerId == userId && folder.Favourite))
                .Select(folder => folder.Id);
            return Ok(JsonSerializer.Serialize(favouriteFolders.Select( folderId => folderId.ToString())));
        }

        [Authorize]
        [HttpPost("SwitchFavouriteFolder")]
        public async Task<ActionResult> SwitchFavouriteFolder(FavouriteSwitch favouriteSwitch)
        {
            var userId = Tools.AuthenticationManager.GetUserId(User);

            if (!await FileFolderManager.CanDeleteFolder(userId, new ObjectId(favouriteSwitch.Id), _foldersRepository))
            {
                return BadRequest("You don't have access to folder or it doesn't exist");
            }

            await _foldersRepository.UpdateOneAsync(favouriteSwitch.Id, "Favourite", favouriteSwitch.IsFavourite);

            return Ok();
        }


        [Authorize]
        [HttpGet("AllMyFolders")]
        public async Task<ActionResult<List<SimpleFolder>>> AllMyFolders()
        {
            var userId = AuthenticationManager.GetUserId(User);

            var folders = (await _foldersRepository.FilterByAsync(f => f.OwnerId == userId)).ToList();

            return Ok(folders.Select(f => new SimpleFolder(f)));
        }
    }
}
