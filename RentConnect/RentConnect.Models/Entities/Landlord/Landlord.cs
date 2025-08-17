namespace RentConnect.Models.Entities.Landlords
{
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Properties;

    public class Landlord : BaseEntity
    {
        public DateTime? DateCreated { get; set; }
        public DateTime? DateExpiry { get; set; }
        public bool? IsRenew { get; set; }

        public ICollection<Property> Properties { get; set; } = new List<Property>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}