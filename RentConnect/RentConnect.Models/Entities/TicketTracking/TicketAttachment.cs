namespace RentConnect.Models.Entities.TicketTracking
{
    using System.ComponentModel.DataAnnotations;

    public class TicketAttachment : BaseEntity
    {
        public long? CommentId { get; set; }

        [MaxLength(255)]
        public string? FileName { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(100)]
        public string? FileType { get; set; }

        public long? UploadedBy { get; set; }

        public DateTime? DateUploaded { get; set; }

        // Navigation property
        public virtual TicketComment? Comment { get; set; }
    }
}
