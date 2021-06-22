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
            var userId = AuthenticationManager.GetUserId(User);

            // Currently supports only own folders
            var folders = (await _foldersRepository.FilterByAsync(folder => folder.OwnerId == userId)).ToList();
            var folderIds = folders.Select(folder => folder.Id);

            var files = (await _filesRepository.FilterByAsync(file => folderIds.Contains(file.FolderId) || file.AllowedUsers.Contains(userId))).ToList();

            if (folders.Count == 0)
            {
                return BadRequest("User doesn't have any folder (even required root)");
            }

            try
            {
                var rootFolder = FileFolderManager.CombineFilesAndFolders(folders, files.Where(file => file.OwnerId == userId));

                List<UserFilesInfo> response = new List<UserFilesInfo>
            {
                new UserFilesInfo()
                {
                    OwnerId = userId,
                    RootFolder = rootFolder,
                    AvailableFiles = files.Where(file => file.OwnerId != userId)
                        .Select(f => new FileInfoWithUser(f)).ToList()
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
            catch(ArgumentException e)
            {
                _logger.LogError("User {userId} requested /FilesInfo but ArgumentException happened: {e}", userId, e);
                return BadRequest("Unknown error");
            }
        }

        [HttpPost("UserInfo")]
        public async Task<ActionResult<String>> UserInfo(ObjectIdRequest request)
        {
            var user = await _usersRepository.FindByIdAsync(request.Id);
            var userInfo = new UserInfo(user);

            return Ok(JsonSerializer.Serialize(userInfo));
        }

        [Authorize]
        [HttpGet("FindUser")]
        public async Task<ActionResult<List<DTO.Response.UserInfo>>> FindUser(string query)
        {
            var list = new List<FilterDefinition<User>>();
            list.Add(Builders<User>.Filter.Regex("FirstName", new BsonRegularExpression(query)));
            list.Add(Builders<User>.Filter.Regex("LastName", new BsonRegularExpression(query)));
            list.Add(Builders<User>.Filter.Regex("Login", new BsonRegularExpression(query)));
            var filter = Builders<User>.Filter.Or(list);

            var users = await _usersRepository.FindAsync(filter);

            return Ok(users.Select(u => new UserInfo(u)));
        }


        [Authorize]
        [HttpPost("ShareFile")]
        public async Task<ActionResult> ShareFileWithUser(ShareFileRequest request)
        {
            var userId = AuthenticationManager.GetUserId(User);

            if(request.UserId == userId.ToString())
            {
                return BadRequest("You already have access to this file");
            }

            var file = await _filesRepository.FindByIdAsync(request.FileId);

            if(file.AllowedUsers.Contains(new ObjectId(request.UserId)))
            {
                return BadRequest("User already has access to this file");
            }

            var updatedAllowedList = file.AllowedUsers;
            updatedAllowedList.Add(new ObjectId(request.UserId));

            if (!FileFolderManager.CanDeleteFile(userId, file))
            {
                return BadRequest("You don't have rights to share this file");
            }

            await _filesRepository.UpdateOneAsync(request.FileId, "AllowedUsers", updatedAllowedList);

            return Ok();
        }


        [Authorize]
        [HttpPost("StopSharingFile")]
        public async Task<ActionResult> StopSharingFileWithUser(ShareFileRequest request)
        {
            var userId = AuthenticationManager.GetUserId(User);

            if (request.UserId == userId.ToString())
            {
                return BadRequest("You can't stop sharing file with yourself");
            }

            var file = await _filesRepository.FindByIdAsync(request.FileId);

            if (!file.AllowedUsers.Contains(new ObjectId(request.UserId)))
            {
                return BadRequest("File is not shared with this user");
            }

            var updatedAllowedList = file.AllowedUsers;
            updatedAllowedList.Remove(new ObjectId(request.UserId));

            if (!FileFolderManager.CanDeleteFile(userId, file))
            {
                return BadRequest("You don't have rights to stop sharing this file");
            }

            await _filesRepository.UpdateOneAsync(request.FileId, "AllowedUsers", updatedAllowedList);

            return Ok();
        }


        [Authorize]
        [HttpGet("FilesSharedByMe")]
        public async Task<ActionResult<List<UserFilesInfoFile>>> FilesSharedByMe()
        {
            var userId = AuthenticationManager.GetUserId(User);

            var files = (await _filesRepository.FilterByAsync(file => file.OwnerId == userId && file.AllowedUsers.Count() != 0)).ToList();

            return Ok(JsonSerializer.Serialize(files.Select(f => new SharedFileInfo(f)),
                            new JsonSerializerOptions()
                            {
                                Converters =
                        {
                            new SharedFileSerializer()
                        }
                            }
                ));


        }
    }
}
