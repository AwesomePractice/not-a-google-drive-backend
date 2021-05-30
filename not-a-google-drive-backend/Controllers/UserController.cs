using DatabaseModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using not_a_google_drive_backend.Models;
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

        [HttpPost("AddUser")]
        public async Task<ActionResult<String>> SendFileAsync(User newUser)
        {
            DBConnectionService.UsersRepository.InsertOne(new DatabaseModule.Entities.User()
            {
                Login = newUser.Login,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                PasswordHash = "test",
                PasswordSalt = "test",
            });
            return Ok("User was added");
        }

        [HttpGet("GetUser")]
        public async Task<ActionResult<List<User>>> GetUserByLogin(string login)
        {
            return Ok(DBConnectionService.UsersRepository.FilterBy(x => x.Login == login).ToList().ConvertAll(x => new User() {Login = x.Login, FirstName = x.FirstName }));
        }

        [HttpGet("GetConnectionDBString")]
        public async Task<ActionResult<List<User>>> GetConDBString()
        {
            return Ok(_configuration.GetValue<string>("DB_connection"));
        }

    }
}
