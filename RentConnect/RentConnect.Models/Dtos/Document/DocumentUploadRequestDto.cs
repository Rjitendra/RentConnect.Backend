namespace RentConnect.Models.Dtos.Document
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Enums;

    public class DocumentUploadItemDto
    {
        [FromForm] public IFormFile File { get; set; }
        [FromForm] public long OwnerId { get; set; }
        [FromForm] public string OwnerType { get; set; }
        [FromForm] public DocumentType DocumentType { get; set; }
    }

    public class DocumentUploadRequestDto
    {
        [FromForm] public List<DocumentUploadItemDto> Documents { get; set; } = new();
    }
}