namespace RentConnect.Models.Entities.Tenants
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class TenantChildren
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string? TenantGroupId { get; set; }
        public string? Name { get; set; }

        public string? Email { get; set; }

        public DateTime? DOB { get; set; }

        public string? Occupation { get; set; }

        public long TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }
}