namespace RentConnect.API.Controller
{
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Utility;

    //[Authorize(Policy = AuthPolicyNames.HasRole)]
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected IActionResult ProcessResult(Result result)
        {
            switch (result.Status)
            {
                case ResultStatusType.Success:
                    return this.Ok();

                case ResultStatusType.Failure:
                    return this.BadRequest(result.Message);

                case ResultStatusType.NotFound:
                    return this.NotFound(result.Message);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected IActionResult ProcessResult<T>(Result<T> result)
        {
            switch (result.Status)
            {
                case ResultStatusType.Success:
                    return this.Ok(result.Entity);

                case ResultStatusType.Failure:
                    return this.BadRequest(result.Message);

                case ResultStatusType.NotFound:
                    return this.NotFound(result.Message);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}