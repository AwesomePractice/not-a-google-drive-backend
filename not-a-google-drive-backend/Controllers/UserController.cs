using DatabaseModule;
using DatabaseModule.Entities;
using DatabaseModule.VO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using not_a_google_drive_backend.DTO.Response;
using not_a_google_drive_backend.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using not_a_google_drive_backend.DTO.Response.CustomJsonSerializers;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        protected readonly IConfiguration _configuration;

        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Folder> _foldersRepository;
        private readonly IMongoRepository<File> _filesRepository;
        private readonly IMongoRepository<DatabaseModule.Entities.Bucket> _bucketsRepository;

        private readonly FileFolderManager _folderManager;

        public UserController(IConfiguration configuration,
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep, MongoRepository<File> fileRep, 
            MongoRepository<DatabaseModule.Entities.Bucket> bucketRep, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
            _filesRepository = fileRep;
            _bucketsRepository = bucketRep;
            _folderManager = new FileFolderManager();
        }

        [HttpPost("SignUp")]
        public async Task<ActionResult<String>> SignUpAsync(NewUserData user)
        {
            if (!PasswordManager.ValidatePasswordStrength(user.Password))
            {
                return BadRequest("Password is not strong enough");
            }
            if (await _usersRepository.FindOneAsync(x => x.Login == user.Login) != null)
            {
                return BadRequest("Login is already used");
            }

            var salt = PasswordManager.GenerateSalt_128();

            var Buckets = new List<ObjectId>();
            DatabaseModule.Entities.Bucket defaultBucket;
            try
            {
                defaultBucket = await AuthenticationManager.GetGoogleBucketDefault(_bucketsRepository);
                Buckets.Add(defaultBucket.Id);
            }
            catch(Exception e)
            {
                return Ok("User was added without linking default bucket");
            }

            var newUser = new DatabaseModule.Entities.User()
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate.Date,
                PasswordHash = PasswordManager.GeneratePasswordHash(user.Password, salt),
                PasswordSalt = salt,
                Buckets = Buckets,
                CurrentBucket = defaultBucket
                //GoogleBucketConfigData = AuthenticationManager.GoogleBucketConfigData(await AuthenticationManager.GetGoogleBucketDefault())
            };

            await _usersRepository.InsertOneAsync(newUser);

            await _foldersRepository.InsertOneAsync(new Folder()
            {
                Name = "root",
                OwnerId = newUser.Id,
                ParentId = null
            });

            return Ok("User was added");
        }

        [HttpPost("SignIn")]
        public async Task<ActionResult<object>> SignInAsync(Credentials cred)
        {
            var user = await _usersRepository.FindOneAsync(x => x.Login == cred.Login);
            if (user == null)
            {
                return BadRequest("Login does not exist!");
            }
            var result = AuthenticationManager.GenerateJWT(cred, user.Id.ToString(), user.PasswordHash, user.PasswordSalt);
            if(result == null)
            {
                return BadRequest("Password is incorrect!");
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPost("LinkGoogleBucket")]
        public async Task<ActionResult<string>> LinkGoogleBucketAsync(string selectedBucket, string bucketName, DTO.Request.GoogleBucketConfigData data)
        {
            //var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            var userId = AuthenticationManager.GetUserId(User);

            var bucketWithTheSameName = await _bucketsRepository.FindOneAsync(bucket => bucket.Name == bucketName && (bucket.OwnerId == null || bucket.OwnerId == userId));

            if(bucketWithTheSameName != null)
            {
                return BadRequest("Bucket with such name is already linked to this user");
            }

            var newBucket = new DatabaseModule.Entities.Bucket()
            {
                Name = bucketName,
                OwnerId = userId,
                BucketConfigData = new DatabaseModule.VO.GoogleBucketConfigData()
                {
                    ConfigData = JsonSerializer.Serialize(data),
                    SelectedBucket = selectedBucket
                }
            };

            await _bucketsRepository.InsertOneAsync(newBucket);
            await _usersRepository.FindOneAndUpdateAsync(userId.ToString(),
                Builders<User>.Update.Combine(
                    Builders<User>.Update.Set("CurrentBucket", newBucket), 
                    Builders<User>.Update.Push("Buckets", newBucket.Id)));
            return Ok("You have linked google bucket to your account");
        }


        //[Authorize(Roles = "admin")]
        //[HttpGet("GetMyUsername")]
        //public async Task<ActionResult<string>> GetUsername()
        //{

        //    return Ok(User.Identity.Name);
        //}

        [Authorize]
        [HttpGet("FilesInfo")]
        public async Task<ActionResult<String>> GetFilesInfo()
        {
            ObjectId userId = new ObjectId(User.FindFirst("id").Value);

            // Currently supports only own folders
            var folders = _foldersRepository.FilterBy(folder => folder.OwnerId == userId).ToList();
            var folderIds = folders.Select(folder => folder.Id);

            var files = _filesRepository.FilterBy(file => folderIds.Contains(file.FolderId)).ToList();

            if (folders.Count == 0)
            {
                return BadRequest("User doesn't have any folder (even required root)");
            }

            List<UserFilesInfo> response = new List<UserFilesInfo>
            {
                new UserFilesInfo()
                {
                    OwnerId = userId,
                    RootFolder = FileFolderManager.CombineFilesAndFolders(folders, files)
                }   
            };

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters =
                {
                    new UserFilesInfoSerializer()
                }
            };

            return Ok(JsonSerializer.Serialize<List<UserFilesInfo>>(response, options));
        }

        [HttpPost("UserInfo")]
        public async Task<ActionResult<String>> UserInfo(ObjectIdRequest request)
        {
            var user = await _usersRepository.FindByIdAsync(request.Id);
            var userInfo = new UserInfo(user);

            return Ok(JsonSerializer.Serialize(userInfo));
        }

        //[HttpGet("GetConnectionDBString")]
        //public async Task<ActionResult<List<User>>> GetConDBString()
        //{
        //    return Ok(_configuration.GetValue<string>("DB_connection"));
        //}

    }
}
