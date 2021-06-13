using DatabaseModule;
using DatabaseModule.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
using not_a_google_drive_backend.DTO.Response;
using not_a_google_drive_backend.Models;
using not_a_google_drive_backend.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public UserController(IConfiguration configuration, MongoRepository<User> userRep, MongoRepository<Folder> folderRep, MongoRepository<File> fileRep, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
            _filesRepository = fileRep;
        }

        [HttpPost("SignUp")]
        public async Task<ActionResult<String>> SignUpAsync(NewUserData user)
        {
            if (!PasswordManager.ValidatePasswordStrength(user.Password))
            {
                return BadRequest("Password is not strong enough");
            }
            if(await _usersRepository.FindOneAsync(x => x.Login == user.Login) != null)
            {
                return BadRequest("Login is already used");
            }

            var salt = PasswordManager.GenerateSalt_128();

            var newUser = new DatabaseModule.Entities.User()
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate.Date,
                PasswordHash = PasswordManager.GeneratePasswordHash(user.Password, salt),
                PasswordSalt = salt,
            };

            await _usersRepository.InsertOneAsync(newUser);

            await _foldersRepository.InsertOneAsync(new DatabaseModule.Entities.Folder()
            {
                Name = "root",
                OwnerId = newUser.Id,
                Children = Array.Empty<Folder>()
            }) ;

            return Ok("User was added");
        }

        [HttpPost("SignIn")]
        public async Task<ActionResult<object>> SignInAsync(Credentials cred)
        {
            var user = await _usersRepository.FindOneAsync(x => x.Login == cred.Login);
            if(user == null)
            {
                return BadRequest("Login does not exist!");
            }
            var result = AuthenticationManager.GenerateJWT(cred, user.PasswordHash, user.PasswordSalt, user.Id);
            if(result == null)
            {
                return BadRequest("Password is incorrect!");
            }
            return Ok(result);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("GetMyUsername")]
        public async Task<ActionResult<string>> GetUsername()
        {
          
            return Ok(User.Identity.Name);
        }

        [Authorize]
        [HttpGet("FilesInfo")]
        public async Task<ActionResult<UserFilesInfo[]>> GetFilesInfo()
        {
            ObjectId userId = new ObjectId(User.Claims.First(claim => claim.Type == "UserId").Value);

            var folders = _foldersRepository.FilterBy(folder => folder.OwnerId == userId).ToList();
            var folderIds = folders.Select(folder => folder.Id);

            var files = _filesRepository.FilterBy(file => folderIds.Contains(file.FolderId)).ToList();


            UserFilesInfo[] response = folders.Select(folder => new UserFilesInfo()
            {
                OwnerId = userId,
                RootFolder = UserFilesInfo.CombineFilesAndFolders(folder, files)
            }).ToArray();

            return Ok(response);
        }

        //[HttpGet("GetConnectionDBString")]
        //public async Task<ActionResult<List<User>>> GetConDBString()
        //{
        //    return Ok(_configuration.GetValue<string>("DB_connection"));
        //}

    }
}
