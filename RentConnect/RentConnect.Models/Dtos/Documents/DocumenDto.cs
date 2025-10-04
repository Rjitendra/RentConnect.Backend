namespace RentConnect.Models.Dtos.Document
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Enums;

    public class DocumentDto
    {
        public long? Id { get; set; }
        public IFormFile? File { get; set; }
        public long? OwnerId { get; set; }
        public string? OwnerType { get; set; }
        public DocumentCategory? Category { get; set; }
        public DocumentUploadContext? UploadContext { get; set; } = DocumentUploadContext.None;
        public long? LandlordId { get; set; }
        public long? PropertyId { get; set; }

        public long? TenantId { get; set; }
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
        public string?  DownloadUrl { get; set; } = string.Empty;

    }

    public class DocumentUploadRequestDto
    {
        [FromForm] public List<DocumentDto> Documents { get; set; } = new();
    }
}