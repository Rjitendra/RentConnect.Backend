namespace RentConnect.Models.Dtos.Tickets
{
    public class TicketAttachmentDto
    {
        public long? Id { get; set; }
        public long? CommentId { get; set; }
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        public long? UploadedBy { get; set; }
        public DateTime? DateUploaded { get; set; }
    }
}
