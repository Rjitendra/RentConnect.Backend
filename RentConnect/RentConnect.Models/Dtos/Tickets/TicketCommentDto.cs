namespace RentConnect.Models.Dtos.Tickets
{
    using RentConnect.Models.Enums;

    public class TicketCommentDto
    {
        public long? Id { get; set; }
        public long? TicketId { get; set; }
        public string? Comment { get; set; }
        public long? AddedBy { get; set; }
        public string? AddedByName { get; set; }
        public CreatedByType? AddedByType { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<TicketAttachmentDto>? Attachments { get; set; }
    }
}
