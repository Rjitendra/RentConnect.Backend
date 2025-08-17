using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RentConnect.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Swagger is working");

        [HttpGet("secret")]
        [Authorize]
        public IActionResult GetSecret()
        {
            return Ok("✅ You are authorized! This is a protected endpoint.");
        }
    }
}