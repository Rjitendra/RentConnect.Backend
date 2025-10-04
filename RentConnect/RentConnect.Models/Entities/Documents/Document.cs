namespace RentConnect.Models.Entities.Documents
{
    using RentConnect.Models.Entities.Properties;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Enums;

    public class Document : BaseEntity
    {
        public long? OwnerId { get; set; } // Landlord/Tenant ID

        public string? OwnerType { get; set; } // "Landlord" or "Tenant"

        public DocumentUploadContext? UploadContext { get; set; } = DocumentUploadContext.None;

        public long? LandlordId { get; set; } = -1;
        public long? PropertyId { get; set; } = -1;

        public long? TenantId { get; set; } = -1;

        public DocumentCategory? Category { get; set; } // Enum for Aadhaar, Photo, RentalAgreement, etc.
                                                       // File metadata
        public string? Name { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }  // For preview

        // Document metadata
        public string? DocumentIdentifier { get; set; }  // Unique identifier
        public string? UploadedOn { get; set; } = DateTime.UtcNow.ToString("o"); // ISO format
        public bool? IsVerified { get; set; } = true; // Default false
        public string? VerifiedBy { get; set; } = string.Empty; // Default empty
        public string? Description { get; set; } = string.Empty; // Default empty


    }

   
}