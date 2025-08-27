namespace RentConnect.Models.Entities.Documents
{
    using RentConnect.Models.Entities.Landlords;
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Enums;

    public class Document : BaseEntity
    {

        // Which entity this belongs to
        public long? LandlordId { get; set; }
        public Landlord? Landlord { get; set; }

        public long? PropertyId { get; set; }
        public Property? Property { get; set; }

        public long? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public string OwnerType { get; set; } // "Landlord" or "Tenant"

       
                         
        // File metadata
        public string? Name { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }  // For preview

        // Document details
        public DocumentCategory Category { get; set; } // Enum for Aadhaar, Photo, RentalAgreement, etc.
        public string? DocumentIdentifier { get; set; }  // Unique identifier
        public string? UploadedOn { get; set; } = DateTime.UtcNow.ToString("o"); // ISO format

        // Verification
        public bool IsVerified { get; set; } = true; // Default false
        public string? VerifiedBy { get; set; } = string.Empty; // Default empty
        public string? Description { get; set; } = string.Empty; // Default empty


    }
}