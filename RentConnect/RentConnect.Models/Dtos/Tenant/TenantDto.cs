namespace RentConnect.Models.Dtos.Tenants
{
    using RentConnect.Models.Dtos.Document;


    public class TenantDto
    {
        public long Id { get; set; }
        public long LandlordId { get; set; }
        public long PropertyId { get; set; }

        // Personal Information
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternatePhoneNumber { get; set; }
        public DateTime DOB { get; set; }
        public string Occupation { get; set; } = string.Empty;

        // Calculated property - Age is computed from DOB
        public int Age => CalculateAge(DOB);
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }

        // Address Information
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }

        // Emergency Contact
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        // Government IDs
        public string? AadhaarNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? DrivingLicenseNumber { get; set; }
        public string? VoterIdNumber { get; set; }

        // Employment Details
        public string? EmployerName { get; set; }
        public string? EmployerAddress { get; set; }
        public string? EmployerPhone { get; set; }
        public decimal? MonthlyIncome { get; set; }
        public int? WorkExperience { get; set; }

        // Tenancy Details
        public DateTime TenancyStartDate { get; set; }
        public DateTime? TenancyEndDate { get; set; }
        public DateTime RentDueDate { get; set; }
        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal? MaintenanceCharges { get; set; }
        public int? LeaseDuration { get; set; } = 12; // in months
        public int? NoticePeriod { get; set; } = 30; // in days

        // Agreement & Onboarding
        public bool? AgreementSigned { get; set; }
        public DateTime? AgreementDate { get; set; }
        public string? AgreementUrl { get; set; }
        public bool? OnboardingEmailSent { get; set; }
        public DateTime? OnboardingEmailDate { get; set; }
        public bool? OnboardingCompleted { get; set; }

        // File References
        public string? BackgroundCheckFileUrl { get; set; }
        public string? RentGuideFileUrl { get; set; }
        public string? DepositReceiptUrl { get; set; }

        // Acknowledgement & Verification
        public bool IsAcknowledge { get; set; }
        public DateTime? AcknowledgeDate { get; set; }
        public bool IsVerified { get; set; }
        public string? VerificationNotes { get; set; }

        // Status Flags
        public bool IsNewTenant { get; set; } = true;
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public bool? NeedsOnboarding { get; set; } = true;

        // Grouping
        public int TenantGroup { get; set; }

        // Audit
        public string? IpAddress { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Navigation Properties
        public List<DocumentDto> Documents { get; set; } = new();
        public List<TenantChildrenDto> Children { get; set; } = new();

        // Additional Properties for UI
        public string? PropertyName { get; set; }
        public int TenantCount { get; set; }
        public string? StatusDisplay { get; set; }
        public string? StatusClass { get; set; }
        public string? StatusIcon { get; set; }

        // Helper method for age calculation
        private static int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
