namespace RentConnect.Models.Entities.Tenants
{
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Models.Entities.Landlords;
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.TicketTracking;

    public class Tenant : BaseEntity
    {
        // ✅ Relationship to Landlord
        public long LandlordId { get; set; }
        public virtual Landlord Landlord { get; set; } // Navigation
                                                       // Navigation property

        // ✅ Relationship to Property
        public long PropertyId { get; set; }
        public virtual Property Property { get; set; }



        // Personal info
        public string Name { get; set; }
        public string? Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }
        public DateTime DOB { get; set; }
        public string Occupation { get; set; }
        public string? Gender { get; set; } // 'Male', 'Female', 'Other'
        public string? MaritalStatus { get; set; } // 'Single', 'Married', 'Divorced', 'Widowed'

        // Address info
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        // Govt. IDs
        public string? AadhaarNumber { get; set; }
        public string? PanNumber { get; set; } // Changed from PANNumber to match TypeScript
        public string? DrivingLicenseNumber { get; set; }
        public string? VoterIdNumber { get; set; }

        // Employment details
        public string? EmployerName { get; set; }
        public string? EmployerAddress { get; set; }
        public string? EmployerPhone { get; set; }
        public decimal? MonthlyIncome { get; set; }
        public int? WorkExperience { get; set; } // in years

        // Tenancy details
        public DateTime TenancyStartDate { get; set; }
        public DateTime? TenancyEndDate { get; set; }
        public DateTime RentDueDate { get; set; }
        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal? MaintenanceCharges { get; set; }
        public int? LeaseDuration { get; set; } // in months
        public int? NoticePeriod { get; set; } // in days

        // File references
        public string? BackgroundCheckFileUrl { get; set; }
        public string? RentGuideFileUrl { get; set; }
        public string? DepositReceiptUrl { get; set; }

        // Acknowledgement & verification
        public bool IsAcknowledge { get; set; }
        public DateTime? AcknowledgeDate { get; set; }
        public bool IsVerified { get; set; }
        public string? VerificationNotes { get; set; }

        // Flags
        public bool IsNewTenant { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }

        // Audit
        public string? IpAddress { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }

        // Identity mapping
        public long? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // Extra grouping
        public int TenantGroup { get; set; }

        //Onboarding and agrement
        public bool? OnboardingEmailSent { get; set; }
        public DateTime? OnboardingEmailDate { get; set; }
        public bool? OnboardingCompleted { get; set; }
        public bool? NeedsOnboarding { get; set; }
        public bool? AgreementSigned { get; set; }
        public DateTime? AgreementDate { get; set; }
        public string? AgreementUrl { get; set; }


        // Navigation collections
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}