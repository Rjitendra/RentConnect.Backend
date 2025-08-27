namespace RentConnect.Models.Dtos.Document
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Enums;

    public class DocumentDto
    {
        [FromForm] public IFormFile File { get; set; }
        [FromForm] public long OwnerId { get; set; }
        [FromForm] public string OwnerType { get; set; }
        [FromForm] public DocumentCategory Category { get; set; }


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

    public class DocumenDto
    {
        [FromForm] public List<DocumentDto> Documents { get; set; } = new();
    }
}