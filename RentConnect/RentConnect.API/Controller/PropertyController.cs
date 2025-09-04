using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Properties;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;

namespace RentConnect.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController : BaseController
    {
        private readonly IPropertyService _propertyService;
        private readonly IDocumentService _documentService;

        public PropertyController(IPropertyService propertyService, IDocumentService documentService)
        {
            _propertyService = propertyService;
            _documentService = documentService;
        }

        /// <summary>
        /// Get all properties for a specific landlord
        /// </summary>
        /// <param name="landlordId">The landlord ID</param>
        /// <returns>List of properties</returns>
        [HttpGet("landlord/{landlordId}")]
        public async Task<IActionResult> GetPropertiesByLandlord(long landlordId)
        {
            var result = await _propertyService.GetPropertyList(landlordId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get a specific property by ID
        /// </summary>
        /// <param name="id">Property ID</param>
        /// <returns>Property details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProperty(long id)
        {
            var result = await _propertyService.GetProperty(id);
            return ProcessResult(result);
        }

        /// <summary>
        /// Create a new property with optional document uploads
        /// </summary>
        /// <param name="request">Property details with documents</param>
        /// <returns>Created property ID</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateProperty([FromForm] PropertyDto request)
        {
            try
            {
                // First, create the property
                var propertyResult = await _propertyService.AddPropertyDetail(request);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                var propertyId = propertyResult.Entity;

                // If documents are provided, upload them
                if (request.Documents != null && request.Documents.Any())
                {
                    var documentUploadRequest = new DocumentUploadRequestDto
                    {
                        Documents = request.Documents.Select(d => new DocumentDto
                        {
                            File = d.File,
                            OwnerId = request.LandlordId,
                            OwnerType = "Landlord",
                            Category = d.Category,
                            Description = d.Description,
                            LandlordId = request.LandlordId,
                            PropertyId = propertyId,
                            TenantId = d.TenantId,
                            Url = null,
                            Name = d.File.FileName,
                            Size = d.File.Length,
                            Type = d.File.ContentType,
                            UploadedOn = DateTime.UtcNow.ToString("o"), // Keep consistent with current entity
                            IsVerified = true,
                            DocumentIdentifier=null

                        }).ToList()
                    };

                    // Call document controller to upload files
                    var documentResult = await _documentService.UploadDocuments(documentUploadRequest);

                    // Note: Document upload failure won't fail the property creation
                    // You might want to log this or handle it differently based on business requirements
                }

                return ProcessResult(propertyResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create property: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing property with optional document uploads
        /// </summary>
        /// <param name="request">Updated property details with documents</param>
        /// <returns>Updated property details</returns>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProperty([FromForm] PropertyDto request)
        {
            try
            {
                // First, update the property
                var propertyResult = await _propertyService.UpdatePropertyDetail(request);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                // If new documents are provided, upload them
                if (request.Documents != null && request.Documents.Any(d => d.File != null))
                {
                    var documentUploadRequest = new DocumentUploadRequestDto
                    {
                        Documents = request.Documents.Select(d => new DocumentDto
                        {
                            File = d.File,
                            OwnerId = request.LandlordId,
                            OwnerType = "Landlord",
                            Category = d.Category,
                            Description = d.Description,
                            LandlordId = request.LandlordId,
                            PropertyId = request.Id,
                            TenantId = d.TenantId,
                            Url = null,
                            Name = d.File.FileName,
                            Size = d.File.Length,
                            Type = d.File.ContentType,
                            UploadedOn = DateTime.UtcNow.ToString("o"), // Keep consistent with current entity
                            IsVerified = true,
                            DocumentIdentifier = null

                        }).ToList()
                    };

                    // Call document controller to upload new files
                    var documentResult = await _documentService.UploadDocuments(documentUploadRequest);
                }

                return ProcessResult(propertyResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to update property: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a property
        /// </summary>
        /// <param name="id">Property ID to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(long id)
        {
            var result = await _propertyService.DeleteProperty(id);
            return ProcessResult(result);
        }

        /// <summary>
        /// Download property documents by category
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="category">Document category</param>
        /// <returns>File download</returns>
        [HttpGet("{propertyId}/documents/download")]
        public async Task<IActionResult> DownloadPropertyDocuments(long propertyId, [FromQuery] DocumentCategory category)
        {
            var result = await _propertyService.DownloadPropertyFiles(category, propertyId);

            if (!result.IsSuccess)
                return ProcessResult(result);

            var fileName = $"Property_{propertyId}_{category}_Documents.pdf";
            return File(result.Entity, "application/pdf", fileName);
        }

        /// <summary>
        /// Upload additional documents to an existing property
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="request">Document upload request</param>
        /// <returns>Upload result</returns>
        [HttpPost("{propertyId}/documents/upload")]
        public async Task<IActionResult> UploadPropertyDocuments(long propertyId, [FromForm] DocumentUploadRequestDto request)
        {
            try
            {
                // Verify property exists
                var propertyResult = await _propertyService.GetProperty(propertyId);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                // Set the owner information for all documents
                foreach (var doc in request.Documents)
                {
                    doc.OwnerId = propertyId;
                    doc.OwnerType = "Landlord";
                }

                // Upload documents using the document controller
                var uploadResult = await _documentService.UploadDocuments(request);
                return this.ProcessResult(uploadResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to upload documents: {ex.Message}");
            }
        }

        /// <summary>
        /// Update property status (e.g., Draft to Listed, Listed to Rented)
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="status">New status</param>
        /// <returns>Update result</returns>
        [HttpPatch("{propertyId}/status")]
        public async Task<IActionResult> UpdatePropertyStatus(long propertyId, [FromBody] PropertyStatus status)
        {
            try
            {
                var propertyResult = await _propertyService.GetProperty(propertyId);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                var property = propertyResult.Entity;
                property.Status = status;

                var updateResult = await _propertyService.UpdatePropertyDetail(property);
                return ProcessResult(updateResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to update property status: {ex.Message}");
            }
        }

        /// <summary>
        /// Search properties with filters
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="city">Optional city filter</param>
        /// <param name="propertyType">Optional property type filter</param>
        /// <param name="minRent">Optional minimum rent filter</param>
        /// <param name="maxRent">Optional maximum rent filter</param>
        /// <returns>Filtered properties</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProperties(
            [FromQuery] int? landlordId = null,
            [FromQuery] string? city = null,
            [FromQuery] PropertyType? propertyType = null,
            [FromQuery] decimal? minRent = null,
            [FromQuery] decimal? maxRent = null)
        {
            try
            {
                if (!landlordId.HasValue)
                    return BadRequest("Landlord ID is required for property search");

                var result = await _propertyService.GetPropertyList(landlordId.Value);
                if (!result.IsSuccess)
                    return ProcessResult(result);

                var properties = result.Entity.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(city))
                    properties = properties.Where(p => p.City != null && p.City.Contains(city, StringComparison.OrdinalIgnoreCase));

                if (propertyType.HasValue)
                    properties = properties.Where(p => p.PropertyType == propertyType);

                if (minRent.HasValue)
                    properties = properties.Where(p => p.MonthlyRent >= minRent);

                if (maxRent.HasValue)
                    properties = properties.Where(p => p.MonthlyRent <= maxRent);

                return Ok(properties.ToList());
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to search properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Get property images by landlord ID and property ID
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of property images with accessible URLs</returns>
        [HttpGet("landlord/{landlordId}/{propertyId}/images")]
        public async Task<IActionResult> GetPropertyImages(long landlordId, long propertyId)
        {
            try
            {
                // First verify that the property exists and belongs to the landlord
                var propertyResult = await _propertyService.GetProperty(propertyId);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                var property = propertyResult.Entity;
                if (property.LandlordId != landlordId)
                    return BadRequest("Property does not belong to the specified landlord");

                // Get property images
                var imagesResult = await _documentService.GetPropertyImages(landlordId, propertyId);
                if (!imagesResult.IsSuccess)
                    return ProcessResult(imagesResult);

                // Convert relative paths to full URLs that can be accessed
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var imageList = imagesResult.Entity.Select(img =>
                {
                    img.Url = $"{baseUrl}{img.Url}"; // For Angular display
                    img.DownloadUrl = $"{baseUrl}/api/Property/landlord/{landlordId}/property/{propertyId}/image/{img.DocumentIdentifier}/download";
                    return img;
                }).ToList();




                return Ok(new
                {
                    Status = imagesResult.Status,
                    Message = imagesResult.Message,
                    Success = true,
                    Entity = imageList
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get property images: {ex.Message}");
            }
        }

        /// <summary>
        /// Download a specific property image
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <param name="imageId">Image document ID</param>
        /// <returns>Image file download</returns>
        [HttpGet("landlord/{landlordId}/property/{propertyId}/image/{imageId}/download")]
        public async Task<IActionResult> DownloadPropertyImage(long landlordId, long propertyId, long imageId)
        {
            try
            {
                // First verify that the property exists and belongs to the landlord
                var propertyResult = await _propertyService.GetProperty(propertyId);
                if (!propertyResult.IsSuccess)
                    return ProcessResult(propertyResult);

                var property = propertyResult.Entity;
                if (property.LandlordId != landlordId)
                    return BadRequest("Property does not belong to the specified landlord");

                // Download the specific image
                var imageResult = await _documentService.DownloadDocument(imageId);
                if (!imageResult.IsSuccess)
                    return ProcessResult(imageResult);

                // Get image metadata to determine content type
                var imagesResult = await _documentService.GetPropertyImages(landlordId, propertyId);
                if (!imagesResult.IsSuccess)
                    return NotFound("Image not found");

                var imageMetadata = imagesResult.Entity.FirstOrDefault(img => img.DocumentIdentifier == imageId.ToString());
                if (imageMetadata == null)
                    return NotFound("Image not found in property images");

                var contentType = imageMetadata.Type ?? "application/octet-stream";
                var fileName = imageMetadata.Name ?? $"property_image_{imageId}";

                return File(imageResult.Entity, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to download property image: {ex.Message}");
            }
        }
    }
}
