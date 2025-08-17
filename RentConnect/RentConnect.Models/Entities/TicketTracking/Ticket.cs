namespace RentConnect.Models.Entities.TicketTracking
{
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;

    public class Ticket : BaseEntity
    {
        public int LandlordId { get; set; }
        public int TenantId { get; set; }
        public int TenantGroupId { get; set; }
        public long PropertyId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public virtual IList<TicketStatus> Status { get; set; }
        public Tenant? Tenant { get; set; }
        public Property? Property { get; set; }
    }
}