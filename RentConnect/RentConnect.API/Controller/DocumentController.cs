namespace RentConnect.API.Controller
{
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Models.Entities.Documents;
    using RentConnect.Services.Utility;

    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : BaseController
    {
        private readonly ApiContext Db;

        public DocumentController(ApiContext db)
        {
            this.Db = db;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocuments([FromForm] DocumentUploadRequestDto request)
        {
            try
            {
                if (request.Documents == null || !request.Documents.Any())
                    return BadRequest("No files received");

                var savedDocs = new List<Document>();
                foreach (var doc in request.Documents.Where(d => d.File != null && d.File.Length > 0))
                {
                    var fileUrl = await SaveFileAsync(doc.File, doc.OwnerType, doc.OwnerId.Value);
                    savedDocs.Add(new Document
                    {
                        OwnerId = doc.OwnerId,
                        OwnerType = doc.OwnerType,
                        Category = doc.Category,
                        Url = fileUrl,
                        Name = doc.File.FileName,
                        Size = doc.File.Length,
                        Type = doc.File.ContentType,
                        Description = doc.Description,

                        // UploadedOn = DateTime.UtcNow,
                        // IsVerified = false,
                        // VerifiedBy = "jitendra"
                    });
                }

                if (!savedDocs.Any())
                    return ProcessResult(Result.Failure("No valid files to upload"));

                await Db.Document.AddRangeAsync(savedDocs);
                await Db.SaveChangesAsync();

                return ProcessResult(Result.Success());
            }
            catch (Exception ex)
            {
                // Log the exception if you have a logger, e.g., _logger.LogError(ex, "Upload failed");
                return this.ProcessResult(Result.Failure($"Upload failed: {ex.Message}"));
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