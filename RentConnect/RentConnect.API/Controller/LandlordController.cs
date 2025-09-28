
namespace RentConnect.API.Controller
{
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Services.Interfaces;

    [Route("api/[controller]")]
    [ApiController]
    public class LandlordController : BaseController
    {
        private readonly ILandlordService _landlordService;

        public LandlordController(ILandlordService landlordService)
        {
            _landlordService = landlordService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetLandlordByUserId(long userId)
        {
            var result = await _landlordService.GetLandlordByUserId(userId);
            if (result.IsSuccess)
            {
                return ProcessResult(result);
            }

            return BadRequest("Landlord not found");
        }
    }
}
