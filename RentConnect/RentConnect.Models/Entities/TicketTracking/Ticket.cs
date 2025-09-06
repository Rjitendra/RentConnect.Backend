namespace RentConnect.Models.Entities.TicketTracking
{
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class Ticket : BaseEntity
    {
        [MaxLength(50)]
        public string? TicketNumber { get; set; } // Auto-generated ticket number

        public long? LandlordId { get; set; }

        [MaxLength(50)]
        public string? TenantGroupId { get; set; }

        public long? PropertyId { get; set; }

        public TicketCategory? Category { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; } // Header/title of the ticket

        [MaxLength(2000)]
        public string? Description { get; set; }

        public TicketPriority? Priority { get; set; }

        public TicketStatusType? CurrentStatus { get; set; }

        public long? CreatedBy { get; set; } // User ID who created the ticket

        public CreatedByType? CreatedByType { get; set; } // Who created it

        public long? AssignedTo { get; set; } // User ID assigned to handle the ticket

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public DateTime? DateResolved { get; set; }

        // Navigation properties
        public virtual ICollection<TicketStatus> StatusHistory { get; set; } = new List<TicketStatus>();

        public long? TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        public virtual Property? Property { get; set; }

        public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    }
}