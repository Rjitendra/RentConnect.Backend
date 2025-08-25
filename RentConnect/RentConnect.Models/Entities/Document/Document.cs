namespace RentConnect.Models.Entities.Documents
{
    using Microsoft.AspNetCore.Http;
    using RentConnect.Models.Enums;

    public class Document : BaseEntity
    {
        public long OwnerId { get; set; } // Landlord/Tenant ID
        public string OwnerType { get; set; } // "Landlord" or "Tenant"

        public DocumentCategory Category { get; set; } // Enum for Aadhaar, Photo, RentalAgreement, etc.
                                                   // File metadata
        public IFormFile? File { get; set; }   // For uploaded file (ASP.NET Core)
        public string? Name { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
        public string? Url { get; set; }  // For preview

        // Document metadata
        public string? DocumentIdentifier { get; set; }  // Unique identifier
        public string? UploadedOn { get; set; } = DateTime.UtcNow.ToString("o"); // ISO format
        public bool IsVerified { get; set; } = true; // Default false
        public string? VerifiedBy { get; set; } = string.Empty; // Default empty
        public string? Description { get; set; } = string.Empty; // Default empty
    }
}