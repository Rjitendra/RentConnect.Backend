namespace RentConnect.Models.Entities.TicketTracking
{
    public class TicketStatus : BaseEntity
    {
        public long TicketId { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public long AddedBy { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime DateCreated { get; set; }
    }
}