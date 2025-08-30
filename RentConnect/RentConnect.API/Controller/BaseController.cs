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
            var response = new
            {
                Status = result.Status,
                Message = result.Message,
                Entity = (object?)null
            };

            switch (result.Status)
            {
                case ResultStatusType.Success:
                    return this.Ok(response);

                case ResultStatusType.Failure:
                    return this.BadRequest(response);

                case ResultStatusType.NotFound:
                    return this.NotFound(response);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected IActionResult ProcessResult<T>(Result<T> result)
        {
            var response = new
            {
                Status = result.Status,
                Message = result.Message,
                Entity = result.Entity
            };
            switch (result.Status)
            {
                case ResultStatusType.Success:
                    return this.Ok(response);

                case ResultStatusType.Failure:
                    return this.BadRequest(response);

                case ResultStatusType.NotFound:
                    return this.NotFound(response);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}