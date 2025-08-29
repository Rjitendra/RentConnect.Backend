namespace RentConnect.Models.Dtos.Tenants
{
    public class TenantCreateRequestDto
    {
        // Property & Tenancy Details (shared for all tenants)
        public long PropertyId { get; set; }
        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal MaintenanceCharges { get; set; }
        public DateTime TenancyStartDate { get; set; }
        public DateTime? TenancyEndDate { get; set; }
        public DateTime RentDueDate { get; set; }
        public int LeaseDuration { get; set; } = 12; // in months
        public int NoticePeriod { get; set; } = 30; // in days
        public long LandlordId { get; set; }

        // Tenant Data
        public List<TenantDto> Tenants { get; set; } = new();
    }
}
