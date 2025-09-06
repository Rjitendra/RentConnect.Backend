namespace RentConnect.Models.Dtos.Tickets
{
    using RentConnect.Models.Enums;
    using RentConnect.Models.Dtos.Properties;
    using RentConnect.Models.Dtos.Tenants;

    public class TicketDto
    {
        public long? Id { get; set; }
        public string? TicketNumber { get; set; }
        public long? LandlordId { get; set; }
        public string? TenantGroupId { get; set; }
        public long? PropertyId { get; set; }
        public TicketCategory? Category { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TicketPriority? Priority { get; set; }
        public TicketStatusType? CurrentStatus { get; set; }
        public long? CreatedBy { get; set; }
        public CreatedByType? CreatedByType { get; set; }
        public long? AssignedTo { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? DateResolved { get; set; }

        // Navigation properties
        public List<TicketStatusDto>? StatusHistory { get; set; }
        public long? TenantId { get; set; }
        public TenantDto? Tenant { get; set; }
        public PropertyDto? Property { get; set; }
        public List<TicketCommentDto>? Comments { get; set; }
    }
}
