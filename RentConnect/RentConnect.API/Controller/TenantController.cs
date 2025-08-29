using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.Tenants;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;

namespace RentConnect.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of tenants for the property</returns>
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetTenantsByProperty(long propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("Invalid property ID");

            var result = await _tenantService.GetTenantsByProperty(propertyId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tenants by landlord ID
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <returns>List of tenants for the landlord</returns>
        [HttpGet("landlord/{landlordId}")]
        public async Task<IActionResult> GetTenantsByLandlord(long landlordId)
        {
            if (landlordId <= 0)
                return BadRequest("Invalid landlord ID");

            var result = await _tenantService.GetTenantsByLandlord(landlordId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Create new tenants
        /// </summary>
        /// <param name="request">Tenant creation request</param>
        /// <returns>Created tenant response</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTenants([FromBody] TenantCreateRequestDto request)
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
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(long id, [FromBody] TenantDto tenantDto)
        {
            if (id <= 0)
                return BadRequest("Invalid tenant ID");

            if (tenantDto.Id != id)
                return BadRequest("Tenant ID mismatch");

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
        /// Add child/family member to tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="child">Child information</param>
        /// <returns>Created child</returns>
        [HttpPost("{tenantId}/children")]
        public async Task<IActionResult> AddTenantChild(long tenantId, [FromBody] TenantChildren child)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

            var result = await _tenantService.AddTenantChild(tenantId, child);
            return ProcessResult(result);
        }

        /// <summary>
        /// Update child/family member information
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="childId">Child ID</param>
        /// <param name="child">Updated child information</param>
        /// <returns>Success status</returns>
        [HttpPut("{tenantId}/children/{childId}")]
        public async Task<IActionResult> UpdateTenantChild(long tenantId, long childId, [FromBody] TenantChildren child)
        {
            if (tenantId <= 0 || childId <= 0)
                return BadRequest("Invalid tenant or child ID");

            var result = await _tenantService.UpdateTenantChild(tenantId, childId, child);
            return ProcessResult(result);
        }

        /// <summary>
        /// Delete child/family member
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="childId">Child ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{tenantId}/children/{childId}")]
        public async Task<IActionResult> DeleteTenantChild(long tenantId, long childId)
        {
            if (tenantId <= 0 || childId <= 0)
                return BadRequest("Invalid tenant or child ID");

            var result = await _tenantService.DeleteTenantChild(tenantId, childId);
            return ProcessResult(result);
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
        /// Get documents for tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>List of tenant documents</returns>
        [HttpGet("{tenantId}/documents")]
        public async Task<IActionResult> GetTenantDocuments(long tenantId)
        {
            if (tenantId <= 0)
                return BadRequest("Invalid tenant ID");

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

                if (needsOnboarding.HasValue)
                    tenants = tenants.Where(t => t.NeedsOnboarding == needsOnboarding.Value);

                return Ok(tenants.ToList());
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to search tenants: {ex.Message}");
            }
        }
    }
}
