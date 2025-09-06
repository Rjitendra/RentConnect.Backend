namespace RentConnect.Models.Dtos.Tickets
{
    using Microsoft.AspNetCore.Http;
    using RentConnect.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class TicketAddCommentRequestDto
    {
        public long? TicketId { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public long? AddedBy { get; set; }

        [MaxLength(100)]
        public string? AddedByName { get; set; }

        public CreatedByType? AddedByType { get; set; }

        // For file attachments
        public List<IFormFile>? Attachments { get; set; }
    }
}
