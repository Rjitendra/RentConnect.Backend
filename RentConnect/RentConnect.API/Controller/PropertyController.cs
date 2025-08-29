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
                            OwnerId = propertyId,
                            OwnerType = "Landlord",
                            Category = d.Category,
                            Description = d.Description
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
                        Documents = request.Documents
                            .Where(d => d.File != null)
                            .Select(d => new DocumentDto
                            {
                                File = d.File,
                                OwnerId = request.Id,
                                OwnerType = "Property",
                                Category = d.Category,
                                Description = d.Description
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
    }
}
