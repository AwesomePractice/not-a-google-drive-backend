using DatabaseModule;
using DatabaseModule.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using not_a_google_drive_backend.DTO.Response;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
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
        private readonly ILogger<UserController> _logger;
        protected readonly IConfiguration _configuration;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;

        public FolderController(IConfiguration configuration, MongoRepository<User> userRep, MongoRepository<Folder> folderRep, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
        }

        [Authorize]
        [HttpPost("CreateFolder")]
        public async Task<ActionResult<String>> CreateFolder(CreateFolder createFolder)
        {
            ObjectId parentId = new ObjectId(createFolder.ParentId);
            ObjectId userId = new ObjectId(User.FindFirst("id").Value);

            var parent = await _foldersRepository.FindOneAsync(folder => folder.Id == parentId && folder.OwnerId == userId);
            if (parent == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new Error() { Reason = "There's no such parent" });
            }

            var siblings = await _foldersRepository.FilterByAsync(folder => folder.ParentId == parentId && folder.OwnerId == userId);
            if(siblings.Any(f => f.Name == createFolder.Name))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new Error() { Reason = "There's folder with such name already" });
            }
            Folder folder = new Folder(createFolder.Name, userId, parentId);
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

    }
}
