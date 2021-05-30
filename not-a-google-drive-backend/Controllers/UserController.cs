using Microsoft.AspNetCore.Mvc;
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

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("AutoDeployCheck")]
        public async Task<ActionResult<ListOfFiles>> GetString()
        {
            return Ok("Autodeploy to kubernetes works!");
        }

    }
}
