namespace RentConnect.Models.Entities.Payments
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class RentPaymentHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long TenantId { get; set; }
        public decimal RentPaid { get; set; }
        public DateTime RentDate { get; set; }
        public string PaymentMode { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
