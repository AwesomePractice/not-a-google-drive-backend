using DatabaseModule;
using DatabaseModule.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using not_a_google_drive_backend.Models;
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
        public async Task<ActionResult<String>> CreateFolder(ObjectId parentId, string name)
        {
            ObjectId id = new ObjectId(User.FindFirst("id").Value);

            var parent = await _foldersRepository.FindOneAsync(folder => folder.Id == parentId && folder.OwnerId == id);
            if(parent == null)
            {
                return Ok("Error: There's no such parent");
            }
            var folderWithThisName = await _foldersRepository.FindOneAsync(folder => 
                parent.Children.Contains(folder.Id) && folder.Name == name);
            if(folderWithThisName != null)
            {
                return Ok("Error: There's folder with such name already");
            }
            Folder folder = new Folder(name, parent.OwnerId, Array.Empty<ObjectId>());
            await _foldersRepository.InsertOneAsync(folder);

            var options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new UserInfoFolderSerializer()
                }
            };
            return JsonSerializer.Serialize(folder, options);
        }

    }
}
