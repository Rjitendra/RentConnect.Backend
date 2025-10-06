using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.Tenants;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;

namespace RentConnect.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class TenantController : BaseController
    {
        private readonly ITenantService _tenantService;
        private readonly IDocumentService _documentService;

        public TenantController(ITenantService tenantService, IDocumentService documentService)
        {
            _tenantService = tenantService;
            _documentService = documentService;
        }

        /// <summary>
        /// Get all tenants
        /// </summary>
        /// <returns>List of all tenants</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTenants()
        {
            var result = await _tenantService.GetAllTenants();
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenant by ID
        /// </summary>
        /// <param name="id">Tenant ID</param>
        /// <returns>Tenant details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTenantById(long id)
        {
            if (id <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.GetTenantById(id);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenants by property ID
        /// SECURITY: This endpoint should verify that the requesting user has access to this property
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of tenants for the property</returns>
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetTenantsByProperty(long propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("Invalid property ID");

            // TODO: Add security check to verify the requesting user has access to this property
            // This could be either the landlord who owns the property or a tenant from the same property
            // var currentUserId = GetCurrentUserId();
            // var hasAccess = await _tenantService.UserHasAccessToProperty(currentUserId, propertyId);
            // if (!hasAccess) return Unauthorized();

            var result = await _tenantService.GetTenantsByProperty(propertyId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenants by landlord ID
        /// SECURITY: This endpoint should only be accessible by landlords, not tenants
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <returns>List of tenants for the landlord</returns>
        [HttpGet("landlord/{landlordId}")]
        // TODO: Add proper role-based authorization to ensure only landlords can access this
        // [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> GetTenantsByLandlord(long landlordId)
        {
            if (landlordId <= 0)
                return BadRequest("Invalid landlord ID");

            // TODO: Add additional security check to verify the requesting user is the landlord
            // var currentUserId = GetCurrentUserId();
            // if (currentUserId != landlordId) return Unauthorized();

            var result = await _tenantService.GetTenantsByLandlord(landlordId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Create new tenants
        /// </summary>
        /// <param name="request">Tenant creation request</param>
        /// <returns>Created tenant response</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTenants([FromForm] TenantCreateRequestDto request)
        {
            try
            {
                var result = await _tenantService.CreateTenants(request);
                return ProcessResult(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to process request: {ex.Message}");
            }
        }

        /// <summary>
        /// Update tenant information
        /// </summary>
        /// <param name="id">Tenant ID</param>
        /// <param name="tenantDto">Updated tenant data</param>
        /// <returns>Updated tenant</returns>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateTenant(long id, [FromForm] TenantCreateRequestDto tenantDto)
        {
            if (tenantDto.Tenants.Any() == false)
                return BadRequest("Invalid tenant");

            var result = await _tenantService.UpdateTenant(tenantDto);
            return ProcessResult(result);
        }

        /// <summary>
        /// Delete tenant (soft delete)
        /// </summary>
        /// <param name="id">Tenant ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(long id)
        {
            if (id <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.DeleteTenant(id);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get eligible tenants for onboarding (age > 18, has email, needs onboarding) for a specific property
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of eligible tenants</returns>
        [HttpGet("onboarding/eligible/{landlordId}/{propertyId}")]
        public async Task<IActionResult> GetEligibleTenantsForOnboarding(long landlordId, long propertyId)
        {
            if (landlordId <= 0)
                return BadRequest("Invalid landlord ID");

            if (propertyId <= 0)
                return BadRequest("Invalid property ID");

            var result = await _tenantService.GetEligibleTenantsForOnboarding(landlordId, propertyId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Send onboarding emails to eligible tenants (age > 18, has email, needs onboarding) for a specific property
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Number of emails sent</returns>
        [HttpPost("onboarding/email/{landlordId}/{propertyId}")]
        public async Task<IActionResult> SendOnboardingEmails(long landlordId, long propertyId)
        {
            if (landlordId <= 0)
                return BadRequest("Invalid landlord ID");

            if (propertyId <= 0)
                return BadRequest("Invalid property ID");

            var result = await _tenantService.SendOnboardingEmails(landlordId, propertyId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Send onboarding emails to specific tenants by their IDs
        /// </summary>
        /// <param name="tenantIds">List of tenant IDs</param>
        /// <returns>Number of emails sent</returns>
        [HttpPost("onboarding/email/by-ids")]
        public async Task<IActionResult> SendOnboardingEmailsByTenantIds([FromBody] List<long> tenantIds)
        {
            if (tenantIds == null || !tenantIds.Any())
                return BadRequest("Tenant IDs are required");

            var result = await _tenantService.SendOnboardingEmailsByTenantIds(tenantIds);
            return ProcessResult(result);
        }

        /// <summary>
        /// Create rental agreement for tenant
        /// </summary>
        /// <param name="request">Agreement creation request</param>
        /// <returns>Agreement URL</returns>
        [HttpPost("agreement/create")]
        public async Task<IActionResult> CreateAgreement([FromBody] AgreementCreateRequestDto request)
        {
            if (request.TenantId <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.CreateAgreement(request);
            return ProcessResult(result);
        }

        /// <summary>
        /// Accept rental agreement by primary tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Success status</returns>
        [HttpPost("agreement/accept/{tenantId}")]
        public async Task<IActionResult> AcceptAgreement(long tenantId)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.AcceptAgreement(tenantId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get agreement acceptance status for tenant group
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Agreement acceptance status</returns>
        [HttpGet("agreement/status/{tenantId}")]
        public async Task<IActionResult> GetAgreementStatus(long tenantId)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.GetAgreementStatus(tenantId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenant by email address
        /// </summary>
        /// <param name="email">Tenant email</param>
        /// <returns>Tenant details</returns>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetTenantByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            var result = await _tenantService.GetTenantByEmail(email);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get co-tenants for a specific tenant (same property only)
        /// This endpoint ensures tenants can only see other tenants from their own property
        /// </summary>
        /// <param name="tenantId">Current tenant ID</param>
        /// <returns>List of co-tenants from the same property</returns>
        [HttpGet("{tenantId}/co-tenants")]
        public async Task<IActionResult> GetCoTenants(long tenantId)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            try
            {
                // First, get the current tenant to find their property
                var currentTenantResult = await _tenantService.GetTenantById(tenantId);
                if (currentTenantResult.Status != ResultStatusType.Success || currentTenantResult.Entity == null)
                    return ProcessResult(currentTenantResult);

                var currentTenant = currentTenantResult.Entity;
                if (currentTenant.PropertyId == null)
                    return BadRequest("Tenant is not associated with any property");

                // Get all tenants from the same property
                var propertyTenantsResult = await _tenantService.GetTenantsByProperty(currentTenant.PropertyId.Value);
                if (propertyTenantsResult.Status != ResultStatusType.Success)
                    return ProcessResult(propertyTenantsResult);

                // Filter out the current tenant from the list
                var coTenants = propertyTenantsResult.Entity?.Where(t => t.Id != tenantId).ToList();

                return Ok(Result<List<TenantDto>>.Success(coTenants ?? new List<TenantDto>(), "Co-tenants retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get co-tenants: {ex.Message}");
            }
        }


        /// <summary>
        /// Upload document for tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="file">File to upload</param>
        /// <param name="category">Document category</param>
        /// <param name="description">Document description</param>
        /// <returns>Success status</returns>
        [HttpPost("{tenantId}/documents")]
        public async Task<IActionResult> UploadTenantDocument(
            long tenantId,
            IFormFile file,
            [FromForm] string category,
            [FromForm] string description = "")
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            var result = await _tenantService.UploadTenantDocument(tenantId, file, category, description);
            return ProcessResult(result);
        }

        /// <summary>
        /// Upload multiple documents for tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="request">Document upload request</param>
        /// <returns>Success status</returns>
        [HttpPost("{tenantId}/documents/upload")]
        public async Task<IActionResult> UploadTenantDocuments(
            long tenantId,
            [FromForm] DocumentUploadRequestDto request)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            if (request.Documents == null || !request.Documents.Any())
                return BadRequest("No files provided");

            // Set tenant information for all documents
            foreach (var doc in request.Documents)
            {
                doc.OwnerId = tenantId;
                doc.OwnerType = "Tenant";
                doc.TenantId = tenantId;
            }

            var result = await _documentService.UploadDocuments(request);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            if (result?.Entity != null)
            {

                if (result.Entity.Any())
                {
                    foreach (var doc in result.Entity)
                    {
                        doc.Url = $"{baseUrl}{doc.Url}";  // Full URL for Angular
                        doc.DownloadUrl = null;          // Reset download link if needed
                    }
                }

            }
            return ProcessResult(result);
        }

        /// <summary>
        /// Get documents for tenant
        /// SECURITY: Should verify that the requesting user is the tenant or has access to view their documents
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>List of tenant documents</returns>
        [HttpGet("{tenantId}/documents")]
        public async Task<IActionResult> GetTenantDocuments(long tenantId)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            // TODO: Add security check to verify the requesting user has access to this tenant's documents
            // This should only allow the tenant themselves or their landlord to access documents
            // var currentUserId = GetCurrentUserId();
            // var hasAccess = await _tenantService.UserCanAccessTenantDocuments(currentUserId, tenantId);
            // if (!hasAccess) return Unauthorized();

            var result = await _documentService.GetDocumentsByOwner(tenantId, "Tenant");
            return ProcessResult(result);
        }

        /// <summary>
        /// Delete tenant document
        /// </summary>
        /// <param name="documentId">Document ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteTenantDocument(long documentId)
        {
            if (documentId <= 0)
                return BadRequest("Invalid document ID");

            var result = await _documentService.DeleteDocument(documentId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenant statistics for landlord
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <returns>Tenant statistics</returns>
        [HttpGet("statistics/{landlordId}")]
        public async Task<IActionResult> GetTenantStatistics(long landlordId)
        {
            if (landlordId <= 0)
                return BadRequest("Invalid landlord ID");

            var result = await _tenantService.GetTenantStatistics(landlordId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Search tenants with filters
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <param name="propertyId">Optional property filter</param>
        /// <param name="isActive">Optional active status filter</param>
        /// <param name="needsOnboarding">Optional onboarding status filter</param>
        /// <returns>Filtered tenants</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchTenants(
            [FromQuery] long? landlordId = null,
            [FromQuery] long? propertyId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? needsOnboarding = null)
        {
            try
            {
                if (!landlordId.HasValue)
                    return BadRequest("Landlord ID is required for tenant search");

                var result = await _tenantService.GetTenantsByLandlord(landlordId.Value);
                if (result.Status != ResultStatusType.Success)
                    return ProcessResult(result);

                var tenants = result.Entity.AsQueryable();

                // Apply filters
                if (propertyId.HasValue)
                    tenants = tenants.Where(t => t.PropertyId == propertyId.Value);

                if (isActive.HasValue)
                    tenants = tenants.Where(t => t.IsActive == isActive.Value);

                //if (needsOnboarding.HasValue)
                //    tenants = tenants.Where(t => t.NeedsOnboarding == needsOnboarding.Value);

                return Ok(tenants.ToList());
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to search tenants: {ex.Message}");
            }
        }
    }
}
