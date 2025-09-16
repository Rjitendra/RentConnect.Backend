namespace RentConnect.Models.Dtos.Tenants
{
    public class AgreementStatusDto
    {
        public long TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? TenantGroup { get; set; }
        public bool IsPrimaryTenant { get; set; }
        public bool AgreementCreated { get; set; }
        public DateTime? AgreementDate { get; set; }
        public bool AgreementEmailSent { get; set; }
        public DateTime? AgreementEmailDate { get; set; }
        public bool AgreementAccepted { get; set; }
        public DateTime? AgreementAcceptedDate { get; set; }
        public string? AgreementAcceptedBy { get; set; }
        public bool CanAcceptAgreement { get; set; }
        public bool CanLogin { get; set; }
        public string? Message { get; set; }
    }
}
