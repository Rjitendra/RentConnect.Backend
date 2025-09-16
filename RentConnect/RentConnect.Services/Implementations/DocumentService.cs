namespace RentConnect.Services.Implementations
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Models.Dtos.Properties;
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;

    public class DocumentService : IDocumentService
    {
        private readonly ApiContext _context;

        public DocumentService(ApiContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<DocumentDto>>> UploadDocuments(DocumentUploadRequestDto request)
        {
            try
            {
                if (request.Documents == null || !request.Documents.Any())
                    return Result<IEnumerable<DocumentDto>>.Failure("Property not found");

                var savedDocs = new List<Document>();
                foreach (var doc in request.Documents.Where(d => d.File != null && d.File.Length > 0))
                {
                    var fileUrl = await SaveFileAsync(doc.File, doc.OwnerType, doc.OwnerId.Value);
                    savedDocs.Add(new Document
                    {
                        OwnerId = doc.OwnerId,
                        OwnerType = doc.OwnerType,
                        PropertyId = doc.PropertyId,
                        LandlordId = doc.LandlordId,
                        TenantId = doc.TenantId,
                        Category = doc.Category,
                        Url = fileUrl,
                        Name = doc.File.FileName,
                        Size = doc.File.Length,
                        Type = doc.File.ContentType,
                        Description = doc.Description,
                        UploadedOn = DateTime.UtcNow.ToString("o"), // Keep consistent with current entity
                        IsVerified = true, // Keep consistent with current defaults
                        DocumentIdentifier = null
                    });
                }

                if (!savedDocs.Any())
                    return Result<IEnumerable<DocumentDto>>.Failure("No valid files to upload");

                await _context.Document.AddRangeAsync(savedDocs);
                await _context.SaveChangesAsync();
                // Map Document -> DocumentDto
                var docsDto = savedDocs.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    PropertyId = d.PropertyId,
                    LandlordId = d.LandlordId,
                    TenantId = d.TenantId,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Description = d.Description,
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified,
                    DocumentIdentifier = d.DocumentIdentifier
                }).ToList();
                return Result<IEnumerable<DocumentDto>>.Success(docsDto);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Upload failed: {ex.Message}");
            }
        }

        public async Task<Result<(byte[] fileBytes, string fileName, string contentType)>> DownloadDocument(long documentId)
        {
            try
            {
                var document = await _context.Document.FindAsync(documentId);
                if (document == null)
                    return Result<(byte[], string, string)>.Failure("Document not found");

                var filePath = Path.Combine("wwwroot", document.Url?.TrimStart('/') ?? "");
                if (!File.Exists(filePath))
                    return Result<(byte[], string, string)>.Failure("File not found on disk");

                var fileBytes = await File.ReadAllBytesAsync(filePath);

                var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
                var contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };

                var fileName = Path.GetFileName(filePath);

                return Result<(byte[], string, string)>.Success((fileBytes, fileName, contentType));
            }
            catch (Exception ex)
            {
                return Result<(byte[], string, string)>.Failure($"Failed to download document: {ex.Message}");
            }
        }


        public async Task<Result> DeleteDocument(long documentId)
        {
            try
            {
                var document = await _context.Document.FindAsync(documentId);
                if (document == null)
                    return Result.Failure("Document not found");

                // Delete physical file
                var filePath = Path.Combine("wwwroot", document.Url?.TrimStart('/') ?? "");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Delete from database
                _context.Document.Remove(document);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete document: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<DocumentDto>>> GetDocumentsByOwner(long ownerId, string ownerType)
        {
            try
            {
                var documents = await _context.Document
                    .Where(d => d.OwnerId == ownerId && d.OwnerType == ownerType)
                    .ToListAsync();

                var documentDtos = documents.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    LandlordId = d.LandlordId,
                    PropertyId = d.PropertyId,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Description = d.Description,
                    DocumentIdentifier = d.Id.ToString(),
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified
                }).ToList();

                return Result<IEnumerable<DocumentDto>>.Success(documentDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Failed to get documents: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<DocumentDto>>> GetPropertyImages(long landlordId, long propertyId)
        {
            try
            {
                var documents = await _context.Document
                    .Where(d => d.LandlordId == landlordId &&
                               d.PropertyId == propertyId &&
                               d.Category == Models.Enums.DocumentCategory.PropertyImages)
                    .ToListAsync();

                var documentDtos = documents.Select(d => new DocumentDto
                {
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    LandlordId = d.LandlordId,
                    PropertyId = d.PropertyId,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Description = d.Description,
                    DocumentIdentifier = d.Id.ToString(),
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified
                }).ToList();

                return Result<IEnumerable<DocumentDto>>.Success(documentDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Failed to get property images: {ex.Message}");
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, string ownerType, long ownerId)
        {
            var uploadPath = Path.Combine("wwwroot/uploads", ownerType, ownerId.ToString());
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{ownerType}/{ownerId}/{fileName}";
        }
    }
}
