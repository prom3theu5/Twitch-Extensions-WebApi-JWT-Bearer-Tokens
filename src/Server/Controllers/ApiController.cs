using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;

namespace Server.Controllers
{
    [Authorize]
    [Route("api")]
    public class ApiController : Controller
    {
        [Route("Ping")]
        public string Ping()
        {
            return $"[{DateTime.Now.ToLongDateString()}] - Pong!";
        }
    }
}
