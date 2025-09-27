namespace RentConnect.Models.Entities.Landlords
{
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;

    public class Landlord : BaseEntity
    {
        public long ApplicationUserId { get; set; } // Foreign key to AspNetUsers
        public DateTime? DateCreated { get; set; }
        public DateTime? DateExpiry { get; set; }
        public bool? IsRenew { get; set; }

        public ICollection<Property> Properties { get; set; } = new List<Property>();
        public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>(); // 👈 Add this
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}