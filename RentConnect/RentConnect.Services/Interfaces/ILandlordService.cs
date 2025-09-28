
namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos.Landlords;
    using RentConnect.Services.Utility;
    public interface ILandlordService
    {
        Task<Result<LandlordDto>> GetLandlordByUserId(long userId);
    }
}
