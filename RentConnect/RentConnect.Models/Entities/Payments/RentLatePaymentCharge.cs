namespace RentConnect.Models.Entities.Payments
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class RentLatePaymentCharge
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long TenantId { get; set; }
        public decimal LatePaymentCharge { get; set; }
        public DateTime RentDate { get; set; }
        public string Note { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
