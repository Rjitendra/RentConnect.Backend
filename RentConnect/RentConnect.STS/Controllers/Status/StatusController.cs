namespace RentConnect.STS.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Context;
    using System.Linq;

    /// <summary>
    /// Purpose of this controller is to offer a simple status end point that one can call to test STS site is able to communicate with the database and is reachable.
    /// </summary>
    [AllowAnonymous] // We need to allow anonymous for this status for simplicity sake.
    [SecurityHeaders]
    [ApiController]
    [Route("[controller]")]
    public class StatusController : Controller
    {
        public StatusController(ApiContext db)
        {
            this.Db = db;
        }

        private ApiContext Db { get; set; }

        // GET status
        [HttpGet]
        public IActionResult Get()
        {
            var client = this.Db.Users.Select(c => c.Email).FirstOrDefault();

            var message = new StatusMessage() { Message = $"Received {client}" };

            return this.Ok(message);
        }
    }

    public class StatusMessage
    {
        public string Message { get; set; }
    }
}