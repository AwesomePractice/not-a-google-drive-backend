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
    public class BucketController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        protected readonly IConfiguration _configuration;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;
        private readonly IMongoRepository<File> _filesRepository;
        private readonly IMongoRepository<Bucket> _bucketsRepository;

        public BucketController(IConfiguration configuration,
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep, MongoRepository<File> fileRep,
            MongoRepository<Bucket> bucketRep, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
            _filesRepository = fileRep;
            _bucketsRepository = bucketRep;
        }

        [Authorize]
        [HttpGet("MyBuckets")]
        public async Task<ActionResult<String>> MyBuckets()
        {
            var userId = AuthenticationManager.GetUserId(User);
        }

    }
}
