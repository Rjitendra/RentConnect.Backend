namespace RentConnect.Models.Dtos.Tickets
{
    using Microsoft.AspNetCore.Http;
    using RentConnect.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class TicketCreateRequestDto
    {
        public long? LandlordId { get; set; }

        public string? TenantGroupId { get; set; }

        public long? PropertyId { get; set; }

        public TicketCategory? Category { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public TicketPriority? Priority { get; set; }

        public long? CreatedBy { get; set; }

        public CreatedByType? CreatedByType { get; set; }

        public long? AssignedTo { get; set; }

        public long? TenantId { get; set; }

        // For file attachments
        public List<IFormFile>? Attachments { get; set; }
    }
}
