using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Properties;

namespace RentConnect.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController : BaseController
    {
        // Property related endpoints would go here
        [HttpPost("property-create")]
        public async Task<IActionResult> PropertyCreate([FromForm] PropertyDto request)
        {
            return Ok("Property create endpoint");
        }

    }
}
