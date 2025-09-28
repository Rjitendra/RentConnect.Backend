
namespace RentConnect.Services.Implementations
{
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos.Landlords;
    using RentConnect.Models.Dtos.Properties;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;

    public class LandlordService:ILandlordService
    {
        private readonly ApiContext _context;

        public LandlordService(ApiContext context)
        {
            _context = context;
        }
        public async Task<Result<LandlordDto>> GetLandlordByUserId(long userId)
        {
            try
            {
                var landlord = await this._context.Landlord
                    .Where(x => x.ApplicationUserId == userId)
                    .FirstOrDefaultAsync();

                if (landlord != null)
                {
                    var landlordDto = new LandlordDto
                    {
                        Id = landlord.Id,
                        ApplicationUserId = landlord.ApplicationUserId,
                    };
                    return Result<LandlordDto>.Success(landlordDto);
                }
                else
                {
                    return Result<LandlordDto>.Failure("Landlord not found");
                }
            }
            catch (Exception ex) { throw ex; }
        }
    }
}
