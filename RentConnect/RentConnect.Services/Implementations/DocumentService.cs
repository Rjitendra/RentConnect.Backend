namespace RentConnect.Services.Implementations
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos.Document;
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

        public async Task<Result> UploadDocuments(DocumentUploadRequestDto request)
        {
            try
            {
                if (request.Documents == null || !request.Documents.Any())
                    return Result.Failure("No files received");

                var savedDocs = new List<Document>();
                foreach (var doc in request.Documents.Where(d => d.File != null && d.File.Length > 0))
                {
                    var fileUrl = await SaveFileAsync(doc.File, doc.OwnerType, doc.OwnerId.Value);
                    savedDocs.Add(new Document
                    {
                        OwnerId = doc.OwnerId,
                        OwnerType = doc.OwnerType,
                        PropertyId=doc.PropertyId,
                        LandlordId=doc.LandlordId,
                        TenantId=doc.TenantId,
                        Category = doc.Category,
                        Url = fileUrl,
                        Name = doc.File.FileName,
                        Size = doc.File.Length,
                        Type = doc.File.ContentType,
                        Description = doc.Description,
                        UploadedOn = DateTime.UtcNow.ToString("o"), // Keep consistent with current entity
                        IsVerified = true // Keep consistent with current defaults
                    });
                }

                if (!savedDocs.Any())
                    return Result.Failure("No valid files to upload");

                await _context.Document.AddRangeAsync(savedDocs);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Upload failed: {ex.Message}");
            }
        }

        public async Task<Result<byte[]>> DownloadDocument(long documentId)
        {
            try
            {
                var document = await _context.Document.FindAsync(documentId);
                if (document == null)
                    return Result<byte[]>.Failure("Document not found");

                var filePath = Path.Combine("wwwroot", document.Url?.TrimStart('/') ?? "");

                if (!File.Exists(filePath))
                    return Result<byte[]>.Failure("File not found on disk");

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                return Result<byte[]>.Success(fileBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Failed to download document: {ex.Message}");
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
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Description = d.Description,
                    DocumentIdentifier = d.Id.ToString()
                }).ToList();

                return Result<IEnumerable<DocumentDto>>.Success(documentDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Failed to get documents: {ex.Message}");
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
