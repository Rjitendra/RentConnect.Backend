namespace RentConnect.Models.Entities.Payments
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public class RentPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long PropertyId { get; set; }
        public long TenantId { get; set; }
        public decimal RentAmount { get; set; }
        public DateTime RentDate { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
