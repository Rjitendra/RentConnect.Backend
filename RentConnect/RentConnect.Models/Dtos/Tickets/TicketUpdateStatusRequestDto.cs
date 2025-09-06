namespace RentConnect.Models.Dtos.Tickets
{
    using RentConnect.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class TicketUpdateStatusRequestDto
    {
        public long? TicketId { get; set; }

        public TicketStatusType? Status { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public long? UpdatedBy { get; set; }

        public CreatedByType? UpdatedByType { get; set; }

        [MaxLength(100)]
        public string? UpdatedByName { get; set; }
    }
}
