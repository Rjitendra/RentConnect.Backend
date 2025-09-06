namespace RentConnect.Models.Entities.TicketTracking
{
    using RentConnect.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class TicketComment : BaseEntity
    {
        public long? TicketId { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public long? AddedBy { get; set; }

        [MaxLength(100)]
        public string? AddedByName { get; set; }

        public CreatedByType? AddedByType { get; set; }

        public DateTime? DateCreated { get; set; }

        // Navigation properties
        public virtual Ticket? Ticket { get; set; }
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }
}
