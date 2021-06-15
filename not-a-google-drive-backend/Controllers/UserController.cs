﻿using DatabaseModule;
using DatabaseModule.Entities;
using DatabaseModule.VO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using not_a_google_drive_backend.DTO.Request;
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

        public UserController(IConfiguration configuration, 
            MongoRepository<User> userRep, MongoRepository<Folder> folderRep, 
            ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _usersRepository = userRep;
            _foldersRepository = folderRep;
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
            var result = AuthenticationManager.GenerateJWT(cred, user.Id.ToString(), user.PasswordHash, user.PasswordSalt);
            if(result == null)
            {
                return BadRequest("Password is incorrect!");
            }
            return Ok(result);
        }

        [Authorize]
        [HttpPost("LinkGoogleBucket")]
        public async Task<ActionResult<string>> LinkGoogleBucketAsync(DTO.Request.GoogleBucketConfigData data)
        {
            var user = await _usersRepository.FindOneAsync(x => x.Id == new ObjectId(User.FindFirst("id").Value));
            if(user.GoogleBucketConfigData != null)
            {
                return BadRequest("Service is already linked!");
            }

            _usersRepository.UpdateOne(User.FindFirst("id").Value, "GoogleBucketConfigData", 
                new DatabaseModule.VO.GoogleBucketConfigData() { 
                    Id = ObjectId.GenerateNewId(),
                    ClientId = data.ClientId,
                    Secret = data.Secret,
                    Email = data.Email,
                    ProjectId = data.ProjectId,
                    SelectedBucket = data.SelectedBucket
                }
            );
            return Ok("You have linked google bucket to your account");
        }


        [Authorize(Roles = "admin")]
        [HttpGet("GetMyUsername")]
        public async Task<ActionResult<string>> GetUsername()
        {
          
            return Ok(User.Identity.Name);
        }

        //[HttpGet("GetConnectionDBString")]
        //public async Task<ActionResult<List<User>>> GetConDBString()
        //{
        //    return Ok(_configuration.GetValue<string>("DB_connection"));
        //}

    }
}
