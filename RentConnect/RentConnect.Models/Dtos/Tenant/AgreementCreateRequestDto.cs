namespace RentConnect.Models.Dtos.Tenants
{
    public class AgreementCreateRequestDto
    {
        public long TenantId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
    }
}
