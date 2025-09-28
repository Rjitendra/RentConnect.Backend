

namespace RentConnect.Models.Dtos.Landlords
{
    public class LandlordDto
    {
        public long ApplicationUserId { get; set; } // Foreign key to AspNetUsers
        public long Id { get; set; }
        // add other properties as needed
    }
}
