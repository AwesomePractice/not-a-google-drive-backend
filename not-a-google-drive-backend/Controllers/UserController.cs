using DatabaseModule;
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
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        protected readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            new DBConnectionService(_configuration.GetValue<string>("DB_connection"), _configuration.GetValue<string>("DB_name"));
        }

        [HttpPost("SignUp")]
        public async Task<ActionResult<String>> SignUpAsync(NewUserData user)
        {
            if (!PasswordManager.ValidatePasswordStrength(user.Password))
            {
                return BadRequest("Password is not strong enough");
            }
            if(await DBConnectionService.UsersRepository.FindOneAsync(x => x.Login == user.Login) != null)
            {
                return BadRequest("Login is already used");
            }

            var salt = PasswordManager.GenerateSalt_128();
            await DBConnectionService.UsersRepository.InsertOneAsync(new DatabaseModule.Entities.User()
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate.Date,
                PasswordHash = PasswordManager.GeneratePasswordHash(user.Password, salt),
                PasswordSalt = salt,
            });
            return Ok("User was added");
        }


        [HttpPost("SignIn")]
        public async Task<ActionResult<object>> SignInAsync(Credentials cred)
        {
            var user = await DBConnectionService.UsersRepository.FindOneAsync(x => x.Login == cred.Login);
            if(user == null)
            {
                return BadRequest("Login does not exist!");
            }
            var result = AuthenticationManager.GenerateJWT(cred, user.PasswordHash, user.PasswordSalt);
            if(result == null)
            {
                return BadRequest("Password is incorrect!");
            }
            return Ok(result);
        }

        //[HttpGet("GetUser")]
        //public async Task<ActionResult<List<User>>> GetUserByLogin(string login)
        //{
        //    return Ok(DBConnectionService.UsersRepository.FilterBy(x => x.Login == login).ToList().ConvertAll(x => new User() {Login = x.Login, FirstName = x.FirstName }));
        //}

        //[HttpGet("GetConnectionDBString")]
        //public async Task<ActionResult<List<User>>> GetConDBString()
        //{
        //    return Ok(_configuration.GetValue<string>("DB_connection"));
        //}

    }
}
