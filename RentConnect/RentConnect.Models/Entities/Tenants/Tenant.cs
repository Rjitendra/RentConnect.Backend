namespace RentConnect.Models.Entities.Tenants
{
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Properties;

    public class Tenant : BaseEntity
    {
        public long LandLordId { get; set; }
        public long PropertyId { get; set; }

        // Personal info
        public string Name { get; set; }

        public string? Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DOB { get; set; }
        public string Occupation { get; set; }

        // Govt. IDs (India specific)
        public string? AadhaarNumber { get; set; }

        public string? PANNumber { get; set; }

        // Tenancy details
        public DateTime TenancyStartDate { get; set; }

        public DateTime? TenancyEndDate { get; set; }
        public DateTime RentDueDate { get; set; }
        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }

        // File references (store as URL, not byte[])
        public string? BackgroundCheckFileUrl { get; set; }

        public string? RentGuideFileUrl { get; set; }
        public string? DepositReceiptUrl { get; set; }

        // Acknowledgement & verification
        public bool IsAcknowledge { get; set; }

        public DateTime? AcknowledgeDate { get; set; }
        public bool IsVerified { get; set; }

        // Flags
        public bool IsNewTenant { get; set; }

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }

        // Audit
        public string? IpAddress { get; set; }

        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Identity mapping

        public virtual ApplicationUser User { get; set; }

        public virtual Property? Property { get; set; }

        // Extra grouping (normalize later if needed)
        public int TenantGroup { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}