namespace RentConnect.Models.Entities.Documents
{
    using RentConnect.Models.Enums;

    public class Document : BaseEntity
    {
        public long OwnerId { get; set; } // Landlord/Tenant ID
        public string OwnerType { get; set; } // "Landlord" or "Tenant"

        public DocumentType DocumentType { get; set; } // Enum for Aadhaar, Photo, RentalAgreement, etc.
        public string FileUrl { get; set; }

        public string DocumentIdentifier { get; set; } = string.Empty; // Unique identifier for the document, if applicable
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false; // Default to false, can be updated later
        public string VerifiedBy { get; set; } = string.Empty;
    }
}