using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Properties;
using RentConnect.Models.Entities.Landlords;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;

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
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            if (result?.Entity != null)
            {
                foreach (var prop in result.Entity)
                {
                    if (prop.Documents != null && prop.Documents.Any())
                    {
                        foreach (var doc in prop.Documents)
                        {
                            doc.Url = $"{baseUrl}{doc.Url}";  // Full URL for Angular
                            doc.DownloadUrl = null;          // Reset download link if needed
                        }
                    }
                }
            }

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

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            if (result?.Entity != null)
            {

                if (result.Entity.Documents != null && result.Entity.Documents.Any())
                {
                    foreach (var doc in result.Entity.Documents)
                    {
                        doc.Url = $"{baseUrl}{doc.Url}";  // Full URL for Angular
                        doc.DownloadUrl = null;          // Reset download link if needed
                    }
                }

            }
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
                            Name = d?.File?.FileName ?? "",
                            Size = d?.File?.Length ?? 0,
                            Type = d?.File?.ContentType ?? "",
                            UploadedOn = DateTime.UtcNow.ToString("o"), // Keep consistent with current entity
                            IsVerified = true,
                            DocumentIdentifier = null

                        }).ToList()
                    };

                    // Call document controller to upload files
                    var documentResult = await _documentService.UploadDocuments(documentUploadRequest);

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

                // If new documents are provided (Id == null && has file)
                var newDocuments = request.Documents?
                    .Where(d => d.Id == null && d.File != null)
                    .ToList();

                // If new documents are provided, upload them
                if (newDocuments != null && newDocuments.Any())
                {
                    var documentUploadRequest = new DocumentUploadRequestDto
                    {
                        Documents = newDocuments.Select(d => new DocumentDto
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
                            Name = d?.File?.FileName ?? "",
                            Size = d?.File?.Length ?? 0,
                            Type = d?.File?.ContentType ?? "",
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
        /// Get all documents for a property (for chatbot and document viewing)
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of property documents</returns>
        [HttpGet("{propertyId}/documents")]
        public async Task<IActionResult> GetPropertyDocuments(long propertyId)
        {
            try
            {
                if (propertyId <= 0)
                    return BadRequest("Invalid property ID");

                var result = await _documentService.GetDocumentsByProperty(propertyId);

                if (!result.IsSuccess)
                    return ProcessResult(result);

                // Convert relative paths to full URLs
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                if (result.Entity != null && result.Entity.Any())
                {
                    foreach (var doc in result.Entity)
                    {
                        doc.Url = $"{baseUrl}{doc.Url}";
                    }
                }

                return ProcessResult(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get documents: {ex.Message}");
            }
        }

        /// <summary>
        /// Download property documents by category
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="category">Document category</param>
        /// <returns>File download</returns>
        /// moved to document controller
        [HttpGet("{propertyId}/documents/{category}")]
        public async Task<IActionResult> PropertyDocuments(long propertyId, DocumentCategory category)
        {
            var result = await _propertyService.DownloadPropertyFiles(category, propertyId);

            if (!result.IsSuccess)
                return ProcessResult(result);


            // Convert relative paths to full URLs that can be accessed
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var docList = result.Entity.Select(doc =>
            {
                doc.Url = $"{baseUrl}{doc.Url}"; // For Angular display
                doc.DownloadUrl = null;
                return doc;
            }).ToList();

            return Ok(new
            {
                Status = result.Status,
                Message = result.Message,
                Success = true,
                Entity = docList
            });
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

                // Filter: only documents with Id == null (new docs)
                var newDocuments = request.Documents?
                    .Where(x => x.Id == null)
                    .ToList();


                // Set the owner information for all documents
                if (newDocuments != null && newDocuments.Any())
                {
                    var documentUploadRequest = new DocumentUploadRequestDto
                    {
                        Documents = newDocuments.Select(d => new DocumentDto
                        {
                            File = d.File,
                            OwnerId = d.LandlordId,
                            OwnerType = "Landlord",
                            Category = d.Category,
                            Description = d.Description,
                            LandlordId = d.LandlordId,
                            PropertyId = propertyId,
                            TenantId = d.TenantId,
                            Url = null,
                            Name = d?.File?.FileName,
                            Size = d?.File?.Length,
                            Type = d?.File?.ContentType,
                            UploadedOn = DateTime.UtcNow.ToString("o"),
                            IsVerified = true,
                            DocumentIdentifier = null
                        }).ToList()
                    };

                    // Call document controller to upload files
                    var documentResult = await _documentService.UploadDocuments(documentUploadRequest);

                    // Convert relative paths to full URLs that can be accessed
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    if (documentResult.Entity.Any())
                    {
                        var imageList = documentResult.Entity.Select(img =>
                        {
                            img.Url = $"{baseUrl}{img.Url}"; // For Angular display
                            img.DownloadUrl = null;
                            return img;
                        }).ToList();
                    }

                    // Upload documents using the document controller
                    return this.ProcessResult(documentResult);
                }

                // If no documents provided, return a success response or appropriate message
                return BadRequest(new
                {
                    Status = "NoDocuments",
                    Message = "No documents were provided for upload.",
                    Success = true
                });
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
                var imagesResult = await _documentService.GetPropertyImages(landlordId, propertyId, null);
                if (!imagesResult.IsSuccess)
                    return ProcessResult(imagesResult);



                // Convert relative paths to full URLs that can be accessed
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var imageList = imagesResult.Entity.Where(d => d.OwnerType == "Landlord").Select(img =>
                {
                    img.Url = $"{baseUrl}{img.Url}"; // For Angular display
                    img.DownloadUrl = null;
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
        /// <param name="imageId">Image document ID</param>
        /// <returns>Image file download</returns>
        [HttpGet("image/{imageId}/download")]
        public async Task<IActionResult> DownloadDoc(long imageId)
        {
            try
            {
                // Download the specific image
                var imageResult = await _documentService.DownloadDocument(imageId);
                if (!imageResult.IsSuccess)
                    return ProcessResult(imageResult);


                var (fileBytes, fileName, contentType) = imageResult.Entity;

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to download property image: {ex.Message}");
            }
        }


        [HttpDelete("delete/{id:long}")]
        public async Task<IActionResult> DeleteDocument(long id)
        {
            var result = await _documentService.DeleteDocument(id);
            // if this method is inside the same controller, just call DeleteDocument(id) directly.

            return ProcessResult(result);
        }

    }
}
