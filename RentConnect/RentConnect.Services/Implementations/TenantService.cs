namespace RentConnect.Services.Implementations
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos;
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Models.Dtos.Tenants;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Enums;
    using RentConnect.Models.Configs;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;
    using System.Text.RegularExpressions;
    using System.Web;
    using RentConnect.Models.Entities.Documents;

    /// <summary>
    /// Service class responsible for managing tenant operations in the RentConnect system
    /// </summary>
    /// <remarks>
    /// This service provides comprehensive tenant management functionality including:
    /// 
    /// <para><strong>Core CRUD Operations:</strong></para>
    /// - Create, read, update, and delete tenant records
    /// - Support for both individual and group tenant management
    /// - Soft delete functionality with business rule enforcement
    /// 
    /// <para><strong>Onboarding Operations:</strong></para>
    /// - Email-based tenant onboarding workflow
    /// - Age-based eligibility filtering (18+ years)
    /// - Agreement acceptance requirement validation
    /// 
    /// <para><strong>Agreement Management:</strong></para>
    /// - Rental agreement creation and management
    /// - Primary tenant agreement acceptance workflow
    /// - Email notifications for agreement status changes
    /// 
    /// <para><strong>Document Management:</strong></para>
    /// - Tenant document upload and storage
    /// - Document categorization and organization
    /// - Integration with document service for file handling
    /// 
    /// <para><strong>Statistics & Reporting:</strong></para>
    /// - Comprehensive tenant statistics for landlords
    /// - Financial reporting (rent totals, averages)
    /// - Activity tracking and status reporting
    /// 
    /// <para><strong>Key Features:</strong></para>
    /// - Transaction-based operations for data consistency
    /// - Comprehensive input validation and error handling
    /// - Centralized logging for debugging and monitoring
    /// - Email integration for notifications and onboarding
    /// - User account creation for tenant portal access
    /// 
    /// <para><strong>Business Rules:</strong></para>
    /// - Primary tenant concept for group management
    /// - Agreement acceptance restrictions and workflows
    /// - Age-based onboarding eligibility
    /// - Soft delete with agreement status considerations
    /// </remarks>
    public class TenantService : ITenantService
    {
        #region Constants
        private const int DEFAULT_LEASE_DURATION = 12;
        private const int DEFAULT_NOTICE_PERIOD = 30;
        private const int MINIMUM_AGE_FOR_ONBOARDING = 18;
        private const int MINIMUM_NAME_LENGTH = 2;
        private const int AADHAAR_NUMBER_LENGTH = 12;
        private const int TEMPORARY_PASSWORD_LENGTH = 12;
        private const string DEFAULT_RELATIONSHIP = "Adult";
        private const string TENANT_ROLE_NAME = "Tenant";
        private const string HARD_DELETE_REQUIRED_PREFIX = "HARD_DELETE_REQUIRED|";
        #endregion

        #region Fields
        private readonly ApiContext _context;
        private readonly IDocumentService _documentService;
        private readonly IMailService _mailService;
        private readonly ServerSettings _serverSettings;
        private readonly IUserService _userService;
        #endregion

        public TenantService(
            ApiContext context,
            IDocumentService documentService,
            IMailService mailService,
            ServerSettings serverSettings,
            IUserService userService)
        {
            _context = context;
            _documentService = documentService;
            _mailService = mailService;
            _serverSettings = serverSettings;
            _userService = userService;
        }

        #region Core CRUD Operations

        /// <summary>
        /// Retrieves all tenants from the database with their associated property and landlord information
        /// </summary>
        /// <returns>A result containing a collection of tenant DTOs or an error message</returns>
        /// <remarks>
        /// This method loads all tenants with their documents and maps them to DTOs.
        /// Consider using pagination for large datasets to improve performance.
        /// </remarks>
        public async Task<Result<IEnumerable<TenantDto>>> GetAllTenants()
        {
            try
            {
                var tenants = await GetTenantsWithIncludes().ToListAsync();
                var tenantDtos = await MapTenantsWithDocuments(tenants);
                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a specific tenant by their unique identifier
        /// </summary>
        /// <param name="id">The unique identifier of the tenant</param>
        /// <returns>A result containing the tenant DTO or an error message if not found</returns>
        /// <remarks>
        /// This method includes property and landlord information along with associated documents.
        /// Returns NotFound result if the tenant doesn't exist.
        /// </remarks>
        public async Task<Result<TenantDto>> GetTenantById(long id)
        {
            try
            {
                var tenant = await GetTenantsWithIncludes()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                    return Result<TenantDto>.NotFound();

                await LoadTenantDocuments(tenant);
                var tenantDto = await MapToDto(tenant);
                return Result<TenantDto>.Success(tenantDto);
            }
            catch (Exception ex)
            {
                return Result<TenantDto>.Failure($"Failed to get tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all tenants associated with a specific property
        /// </summary>
        /// <param name="propertyId">The unique identifier of the property</param>
        /// <returns>A result containing a collection of tenant DTOs for the specified property</returns>
        /// <remarks>
        /// This method is useful for property management operations where you need to see all tenants in a property.
        /// Includes both active and inactive tenants.
        /// </remarks>
        public async Task<Result<IEnumerable<TenantDto>>> GetTenantsByProperty(long propertyId)
        {
            try
            {
                var tenants = await GetTenantsWithIncludes()
                    .Where(t => t.PropertyId == propertyId)
                    .ToListAsync();

                var tenantDtos = await MapTenantsWithDocuments(tenants);
                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants by property: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all active tenants associated with a specific landlord
        /// </summary>
        /// <param name="landlordId">The unique identifier of the landlord</param>
        /// <returns>A result containing a collection of active tenant DTOs for the specified landlord</returns>
        /// <remarks>
        /// This method filters for active and non-deleted tenants only.
        /// Used primarily for landlord dashboard and management operations.
        /// </remarks>
        public async Task<Result<IEnumerable<TenantDto>>> GetTenantsByLandlord(long landlordId)
        {
            try
            {
                var tenants = await GetTenantsWithIncludes()
                    .Where(t => t.LandlordId == landlordId && t.IsActive == true && t.IsDeleted == false)
                    .ToListAsync();

                var tenantDtos = await MapTenantsWithDocuments(tenants);
                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants by landlord: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates multiple tenants as a group with shared tenancy details
        /// </summary>
        /// <param name="request">The tenant creation request containing tenant details and property information</param>
        /// <returns>A result containing the created tenant DTOs or validation errors</returns>
        /// <remarks>
        /// This method:
        /// - Validates all tenants in the group before creation
        /// - Ensures exactly one primary tenant is designated
        /// - Creates a unique tenant group ID for related tenants
        /// - Handles document uploads for each tenant
        /// - Updates property status to Listed
        /// - Uses database transaction for data consistency
        /// </remarks>
        public async Task<Result<TenantSaveResponseDto>> CreateTenants(TenantCreateRequestDto request)
        {
            // Input validation
            if (request == null)
                return Result<TenantSaveResponseDto>.Failure("Request cannot be null");

            if (request.Tenants == null || !request.Tenants.Any())
                return Result<TenantSaveResponseDto>.Failure("At least one tenant is required");

            if (!request.PropertyId.HasValue || request.PropertyId <= 0)
                return Result<TenantSaveResponseDto>.Failure("Valid property ID is required");

            if (!request.LandlordId.HasValue || request.LandlordId <= 0)
                return Result<TenantSaveResponseDto>.Failure("Valid landlord ID is required");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Apply common request-level values to all tenants
                ApplyRequestValuesToTenants(request);

                // Validate the request
                var validationErrors = ValidateTenantGroup(request.Tenants, true);
                if (validationErrors.Any())
                {
                    return Result<TenantSaveResponseDto>.Failure(new TenantSaveResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = validationErrors.Select(e => e.Message).ToList()
                    });
                }

                // Ensure at least one tenant is marked as primary
                if (!request.Tenants.Any(t => t.IsPrimary == true))
                {
                    request.Tenants.First().IsPrimary = true;
                }

                // Generate a unique tenant group ID
                var tenantGroupId = Guid.NewGuid().ToString();

                var createdTenants = new List<TenantDto>();

                foreach (var tenantDto in request.Tenants)
                {
                    // Map to entity
                    var tenant = MapToEntity(tenantDto, request);
                    tenant.TenantGroup = tenantGroupId;
                    tenant.DateCreated = DateTime.UtcNow;
                    tenant.DateModified = DateTime.UtcNow;

                    // Add tenant to context
                    _context.Tenant.Add(tenant);
                    await _context.SaveChangesAsync();

                    // Handle document uploads
                    if (tenantDto.Documents?.Any() == true)
                    {
                        var docDtos = await SaveTenantDocuments(tenant.Id, tenantDto.Documents, DocumentUploadContext.TenantCreation);

                    }
                    var docs = this._context.Document.Where(x => x.LandlordId == tenantDto.LandlordId && x.PropertyId == tenantDto.PropertyId && x.TenantId == tenantDto.Id && x.UploadContext == DocumentUploadContext.TenantCreation).ToList();
                    tenant.Documents = docs;
                    // Map back to DTO for response
                    var createdDto = await MapToDto(tenant);
                    createdTenants.Add(createdDto);
                }


                var existingProperty = await _context.Property
                   .FirstOrDefaultAsync(p => p.Id == request.PropertyId);
                if (existingProperty != null)
                {
                    existingProperty.UpdatedOn = DateTime.UtcNow;
                    existingProperty.Status = PropertyStatus.Listed;
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<TenantSaveResponseDto>.Success(new TenantSaveResponseDto
                {
                    Success = true,
                    Message = $"Successfully created {createdTenants.Count} tenant(s)",
                    Tenants = createdTenants
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<TenantSaveResponseDto>.Failure($"Failed to create tenants: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates existing tenants in a group with new information
        /// </summary>
        /// <param name="request">The tenant update request containing modified tenant details</param>
        /// <returns>A result containing the updated tenant DTOs or validation errors</returns>
        /// <remarks>
        /// This method:
        /// - Validates all tenants before updating
        /// - Restricts updates if primary tenant has accepted agreement (only email updates allowed)
        /// - Handles document updates for each tenant
        /// - Updates property status to Rented
        /// - Uses database transaction for data consistency
        /// </remarks>
        public async Task<Result<TenantSaveResponseDto>> UpdateTenant(TenantCreateRequestDto request)
        {
            // Input validation
            if (request == null)
                return Result<TenantSaveResponseDto>.Failure("Request cannot be null");

            if (request.Tenants == null || !request.Tenants.Any())
                return Result<TenantSaveResponseDto>.Failure("At least one tenant is required");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Apply common request-level values to all tenants
                ApplyRequestValuesToTenants(request);

                // Validate the whole tenant group
                var groupValidationErrors = ValidateTenantGroup(request.Tenants, request.IsSingleTenant ?? false);
                if (groupValidationErrors.Any())
                {
                    return Result<TenantSaveResponseDto>.Failure(new TenantSaveResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = groupValidationErrors.Select(e => e.Message).ToList()
                    });
                }

                var updatedTenants = new List<TenantDto>();
                var restrictedUpdateCount = 0;

                foreach (var tenantDto in request.Tenants)
                {
                    var existingTenant = await _context.Tenant
                        .Include(t => t.Property)
                        .Include(t => t.Landlord)
                        .FirstOrDefaultAsync(t => t.Id == tenantDto.Id);

                    if (existingTenant == null)
                    {
                        return Result<TenantSaveResponseDto>.Failure(
                            $"Tenant with Id {tenantDto.Id} not found."
                        );
                    }

                    // Check if primary tenant has accepted agreement - if so, only allow email updates
                    bool isAgreementAcceptedByPrimary = IsAgreementStarted(existingTenant);

                    if (isAgreementAcceptedByPrimary)
                    {
                        // Only allow email updates when primary tenant has accepted agreement
                        UpdateEmailOnlyFromDto(existingTenant, tenantDto);
                        existingTenant.DateModified = DateTime.UtcNow;
                        restrictedUpdateCount++;
                    }
                    else
                    {
                        // Validate individual tenant (only if primary tenant hasn't accepted agreement)
                        var validationErrors = ValidateTenant(tenantDto);
                        if (validationErrors.Any())
                        {
                            return Result<TenantSaveResponseDto>.Failure(
                                $"Validation failed for tenant {tenantDto.Id}: " +
                                string.Join(", ", validationErrors.Select(e => e.Message))
                            );
                        }

                        // Update all fields when primary tenant hasn't accepted agreement
                        UpdateEntityFromDto(existingTenant, tenantDto);
                        existingTenant.DateModified = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    // Handle document updates
                    if (tenantDto.Documents?.Any() == true)
                    {
                        await SaveTenantDocuments(existingTenant.Id, tenantDto.Documents, DocumentUploadContext.TenantCreation);
                    }
                    var docs = this._context.Document.Where(x => x.LandlordId == tenantDto.LandlordId && x.PropertyId == tenantDto.PropertyId && x.TenantId == tenantDto.Id && x.UploadContext == DocumentUploadContext.TenantCreation).ToList();
                    existingTenant.Documents = docs;
                    // Map updated DTO
                    var updatedDto = await MapToDto(existingTenant);
                    updatedTenants.Add(updatedDto);
                }

                // Update property metadata
                var existingProperty = await _context.Property
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

                if (existingProperty != null)
                {
                    existingProperty.UpdatedOn = DateTime.UtcNow;
                    existingProperty.Status = PropertyStatus.Rented;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                string message = $"Successfully updated {updatedTenants.Count} tenant(s)";
                if (restrictedUpdateCount > 0)
                {
                    message += $". Note: {restrictedUpdateCount} tenant(s) had agreement accepted by primary tenant - only email updates were allowed.";
                }

                return Result<TenantSaveResponseDto>.Success(new TenantSaveResponseDto
                {
                    Success = true,
                    Message = message,
                    Tenants = updatedTenants
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<TenantSaveResponseDto>.Failure($"Failed to update tenants: {ex.Message}");
            }
        }

        /// <summary>
        /// Soft deletes a tenant by marking them as inactive and deleted
        /// </summary>
        /// <param name="id">The unique identifier of the tenant to delete</param>
        /// <returns>A result indicating success or failure with appropriate error messages</returns>
        /// <remarks>
        /// This method:
        /// - Performs soft delete (marks as inactive/deleted rather than removing from database)
        /// - Prevents deletion of primary tenant if agreement has started
        /// - Requires hard delete confirmation for non-primary tenants after agreement starts
        /// - Returns specific error codes for UI handling (HARD_DELETE_REQUIRED)
        /// </remarks>
        public async Task<Result<bool>> DeleteTenant(long id)
        {
            // Input validation
            if (id <= 0)
                return Result<bool>.Failure("Valid tenant ID is required");

            try
            {
                var tenant = await _context.Tenant.FindAsync(id);
                if (tenant == null)
                    return Result<bool>.NotFound();

                // Check if agreement has started (primary tenant accepted)
                bool isAgreementAcceptedByPrimary = IsAgreementStarted(tenant);

                if (isAgreementAcceptedByPrimary)
                {
                    // Check if this tenant is the primary tenant
                    bool isPrimaryTenant = tenant.IsPrimary.HasValue && tenant.IsPrimary.Value;

                    if (isPrimaryTenant)
                    {
                        // Primary tenant cannot be deleted when agreement has started
                        return Result<bool>.Failure("Primary tenant cannot be deleted after tenancy has started.");
                    }
                    else
                    {
                        // Non-primary tenant - require hard delete confirmation
                        return Result<bool>.Failure($"{HARD_DELETE_REQUIRED_PREFIX}Tenancy already started. Hard delete required to remove this tenant.");
                    }
                }

                // Normal soft delete when agreement hasn't started
                tenant.IsActive = false;
                tenant.IsDeleted = true;
                tenant.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Hard delete a tenant - forces deletion even when agreement has started
        /// Should only be called after user confirmation
        /// </summary>
        /// <param name="id">Tenant ID to hard delete</param>
        /// <returns>Result indicating success or failure</returns>
        public async Task<Result<bool>> HardDeleteTenant(long id)
        {
            try
            {
                var tenant = await _context.Tenant.FindAsync(id);
                if (tenant == null)
                    return Result<bool>.NotFound();

                // Check if this tenant is the primary tenant
                bool isPrimaryTenant = tenant.IsPrimary.HasValue && tenant.IsPrimary.Value;

                if (isPrimaryTenant)
                {
                    // Primary tenant cannot be hard deleted either
                    return Result<bool>.Failure("Primary tenant cannot be deleted after tenancy has started, even with hard delete.");
                }

                // Force delete - mark as inactive and deleted
                tenant.IsActive = false;
                tenant.IsDeleted = true;
                tenant.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to hard delete tenant: {ex.Message}");
            }
        }

        #endregion

        #region Onboarding Operations

        /// <summary>
        /// Retrieves tenants eligible for onboarding email notifications
        /// </summary>
        /// <param name="landlordId">The unique identifier of the landlord</param>
        /// <param name="propertyId">The unique identifier of the property</param>
        /// <returns>A result containing eligible tenants or an error message</returns>
        /// <remarks>
        /// Eligible tenants are those who:
        /// - Are not primary users (primary users handle their own onboarding)
        /// - Have valid email addresses
        /// - Have IncludeInEmail flag set to true OR are not children/kids
        /// - Are currently active
        /// </remarks>
        public async Task<Result<IEnumerable<TenantDto>>> GetEligibleTenantsForOnboarding(long landlordId, long propertyId)
        {
            // Input validation
            if (landlordId <= 0)
                return Result<IEnumerable<TenantDto>>.Failure("Valid landlord ID is required");

            if (propertyId <= 0)
                return Result<IEnumerable<TenantDto>>.Failure("Valid property ID is required");

            try
            {
                // Get all tenants for the specific property and landlord where:
                // - Not primary user (primary user handles their own onboarding)
                // - Has email address
                // - IncludeInEmail flag is true (overrides relationship restrictions) OR
                //   IncludeInEmail is null for backward compatibility AND relationship is not 'Child' or 'Kid'
                // - Is active

                var eligibleTenants = await _context.Tenant
                     .Include(t => t.Property)
                     .Include(t => t.Landlord)
                     .Where(t => t.LandlordId == landlordId
                                 && t.PropertyId == propertyId // Specific property
                                 && (!t.IsPrimary.HasValue || !t.IsPrimary.Value) // Not primary user
                                 && !string.IsNullOrEmpty(t.Email) // Has email
                                 && (t.IncludeInEmail == true ||
                                     (t.IncludeInEmail == null && (string.IsNullOrEmpty(t.Relationship) ||
                                     (t.Relationship != "Child" && t.Relationship != "Kid")))) // Include in email OR (null for backward compatibility AND not child/kid)
                                 && t.IsActive.HasValue && t.IsActive.Value) // Is active = true
                     .ToListAsync();


                var tenantDtos = new List<TenantDto>();
                foreach (var tenant in eligibleTenants)
                {
                    var dto = await MapToDto(tenant);
                    tenantDtos.Add(dto);
                }

                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get eligible tenants: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends onboarding emails to all eligible tenants for a specific property
        /// </summary>
        /// <param name="landlordId">The unique identifier of the landlord</param>
        /// <param name="propertyId">The unique identifier of the property</param>
        /// <returns>A result containing the number of emails sent successfully</returns>
        /// <remarks>
        /// This method:
        /// - Filters tenants by age (18+ years old)
        /// - Only sends to tenants with valid email addresses
        /// - Requires primary tenant to have accepted agreement first
        /// - Skips tenants who have already received onboarding emails
        /// - Updates tenant records with email sent status and timestamp
        /// </remarks>
        public async Task<Result<int>> SendOnboardingEmails(long landlordId, long propertyId)
        {
            // Input validation
            if (landlordId <= 0)
                return Result<int>.Failure("Valid landlord ID is required");

            if (propertyId <= 0)
                return Result<int>.Failure("Valid property ID is required");

            try
            {
                // Get all tenants for the property with basic filters
                var allTenants = await GetTenantsWithIncludes()
                    .Where(t => t.LandlordId == landlordId
                                && t.PropertyId == propertyId // Specific property
                                && (!t.OnboardingEmailSent.HasValue || !t.OnboardingEmailSent.Value)) // Not sent yet
                    .ToListAsync();

                // Apply age and email filters
                var ageFilteredTenants = FilterTenantsByAge(allTenants);
                var emailFilteredTenants = FilterTenantsWithValidEmails(ageFilteredTenants);

                // Filter to only include tenants whose group has accepted agreement
                var eligibleTenants = new List<Tenant>();
                foreach (var tenant in emailFilteredTenants)
                {
                    var primaryTenantAccepted = await IsPrimaryTenantAgreementAccepted(tenant.TenantGroup ?? string.Empty);
                    if (primaryTenantAccepted)
                    {
                        eligibleTenants.Add(tenant);
                    }
                }

                if (!eligibleTenants.Any())
                    return Result<int>.Success(0);

                int emailsSent = 0;

                foreach (var tenant in eligibleTenants)
                {
                    try
                    {
                        var success = await SendOnboardingEmailToTenant(tenant);
                        if (success)
                        {
                            emailsSent++;
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log individual email failures but continue with others
                        LogError($"SendOnboardingEmail to tenant {tenant.Id}", emailEx);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                return Result<int>.Success(emailsSent);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to send onboarding emails: {ex.Message}");
            }
        }

        public async Task<Result<int>> SendOnboardingEmailsByTenantIds(List<long> tenantIds)
        {
            try
            {
                if (tenantIds == null || !tenantIds.Any())
                    return Result<int>.Success(0);

                // Get tenants by IDs
                var allTenants = await GetTenantsWithIncludes()
                    .Where(t => tenantIds.Contains(t.Id))
                    .ToListAsync();

                // Filter tenants with valid emails
                var emailFilteredTenants = FilterTenantsWithValidEmails(allTenants);

                // Filter tenants to only those whose group has an accepted agreement
                var validTenants = new List<Tenant>();
                foreach (var tenant in emailFilteredTenants)
                {
                    var primaryTenantAccepted = await IsPrimaryTenantAgreementAccepted(tenant.TenantGroup ?? string.Empty);
                    if (primaryTenantAccepted)
                    {
                        validTenants.Add(tenant);
                    }
                }

                var tenants = validTenants;

                if (!tenants.Any())
                    return Result<int>.Success(0);

                int emailsSent = 0;

                foreach (var tenant in tenants)
                {
                    try
                    {
                        var success = await SendOnboardingEmailToTenant(tenant);
                        if (success)
                        {
                            emailsSent++;
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log individual email failures but continue with others
                        LogError($"SendOnboardingEmail to tenant {tenant.Id}", emailEx);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                return Result<int>.Success(emailsSent);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to send onboarding emails by tenant IDs: {ex.Message}");
            }
        }

        #endregion

        #region Agreement Management

        /// <summary>
        /// Creates a rental agreement for a tenant and initiates the agreement process
        /// </summary>
        /// <param name="request">The agreement creation request containing tenant information</param>
        /// <returns>A result containing the agreement URL or an error message</returns>
        /// <remarks>
        /// This method:
        /// - Creates agreement record with generated URL
        /// - Sets up AspNetUser accounts for all tenants in the group
        /// - Sends agreement email to the primary tenant
        /// - Only primary tenant can accept agreements
        /// - Updates tenant agreement status and timestamps
        /// </remarks>
        public async Task<Result<string>> CreateAgreement(AgreementCreateRequestDto request)
        {
            // Input validation
            if (request == null)
                return Result<string>.Failure("Request cannot be null");

            if (request.TenantId <= 0)
                return Result<string>.Failure("Valid tenant ID is required");

            try
            {
                var tenant = await _context.Tenant
                   .FirstOrDefaultAsync(t => t.Id == request.TenantId);

                if (tenant == null)
                    return Result<string>.NotFound();

                var tenantGroupTenants = await _context.Tenant.Where(x => x.TenantGroup == tenant.TenantGroup).ToListAsync();

                // Update tenant agreement details
                tenant.AgreementSigned = false;
                tenant.AgreementDate = DateTime.UtcNow;
                tenant.TenancyStartDate = tenant.TenancyStartDate ?? DateTime.UtcNow;
                tenant.TenancyEndDate = tenant.TenancyEndDate ?? DateTime.UtcNow;
                tenant.RentAmount = tenant.RentAmount;
                tenant.SecurityDeposit = tenant.SecurityDeposit;
                tenant.DateModified = DateTime.UtcNow;

                // Generate agreement URL (placeholder)
                var agreementUrl = $"/documents/agreement_{tenant.Id}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                tenant.AgreementUrl = agreementUrl;

                await _context.SaveChangesAsync();

                // Create AspNetUsers records for all tenants in the group

                var tenantDtos = new List<TenantDto>();
                foreach (var groupTenant in tenantGroupTenants)
                {
                    var tenantDto = await MapToDto(groupTenant);
                    tenantDtos.Add(tenantDto);
                }

                var createdUserIds = await CreateTenantUsers(tenantDtos);

                // Log successful user creations
                foreach (var userCreation in createdUserIds)
                {
                    LogInfo($"Created AspNetUser for tenant email {userCreation.Key} with ID: {userCreation.Value}");
                }


                // Send agreement email to tenant
                await SendAgreementEmail(tenant);

                return Result<string>.Success(agreementUrl);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to create agreement: {ex.Message}");
            }
        }

        /// <summary>
        /// Accepts a rental agreement on behalf of the primary tenant
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant accepting the agreement</param>
        /// <returns>A result indicating success or failure of the agreement acceptance</returns>
        /// <remarks>
        /// This method:
        /// - Only allows primary tenants to accept agreements
        /// - Prevents duplicate acceptance of the same agreement
        /// - Updates agreement status and acceptance timestamp
        /// - Sends notification email to the landlord
        /// - Enables access for all family members in the tenant group
        /// </remarks>
        public async Task<Result<bool>> AcceptAgreement(long tenantId)
        {
            // Input validation
            if (tenantId <= 0)
                return Result<bool>.Failure("Valid tenant ID is required");

            try
            {
                var tenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                    return Result<bool>.NotFound();

                // Check if tenant is primary tenant
                if (!tenant.IsPrimary.HasValue || !tenant.IsPrimary.Value)
                    return Result<bool>.Failure("Only primary tenant can accept the agreement");

                // // Check if agreement exists (check if agreement was created, regardless of signed status)
                // if (!tenant.AgreementDate.HasValue || string.IsNullOrEmpty(tenant.AgreementUrl))
                //     return Result<bool>.Failure("No agreement found for this tenant");

                // Check if already accepted
                if (tenant.AgreementAccepted.HasValue && tenant.AgreementAccepted.Value)
                    return Result<bool>.Failure("Agreement has already been accepted");

                // Accept the agreement
                tenant.AgreementSigned = true; // Now the agreement is signed by tenant
                tenant.AgreementAccepted = true;
                tenant.AgreementAcceptedDate = DateTime.UtcNow;
                tenant.AgreementAcceptedBy = tenant.Name;
                tenant.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification to landlord about agreement acceptance
                await SendAgreementAcceptanceNotificationToLandlord(tenant);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to accept agreement: {ex.Message}");
            }
        }

        public async Task<Result<AgreementStatusDto>> GetAgreementStatus(long tenantId)
        {
            try
            {
                var tenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                    return Result<AgreementStatusDto>.NotFound();

                // Find the primary tenant in the same group
                var primaryTenant = await _context.Tenant
                    .FirstOrDefaultAsync(t => t.TenantGroup == tenant.TenantGroup &&
                                              t.IsPrimary.HasValue && t.IsPrimary.Value);

                var status = new AgreementStatusDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    TenantGroup = tenant.TenantGroup,
                    IsPrimaryTenant = tenant.IsPrimary.HasValue && tenant.IsPrimary.Value,
                    AgreementCreated = tenant.AgreementSigned.HasValue && tenant.AgreementSigned.Value,
                    AgreementDate = tenant.AgreementDate,
                    AgreementEmailSent = tenant.AgreementEmailSent.HasValue && tenant.AgreementEmailSent.Value,
                    AgreementEmailDate = tenant.AgreementEmailDate,
                    AgreementAccepted = primaryTenant?.AgreementAccepted.HasValue == true && primaryTenant.AgreementAccepted.Value,
                    AgreementAcceptedDate = primaryTenant?.AgreementAcceptedDate,
                    AgreementAcceptedBy = primaryTenant?.AgreementAcceptedBy
                };

                // Determine if this tenant can accept agreement (only primary tenant can)
                status.CanAcceptAgreement = status.IsPrimaryTenant &&
                                            status.AgreementCreated &&
                                            !status.AgreementAccepted;

                // Determine if this tenant can login
                status.CanLogin = status.AgreementAccepted;

                // Set appropriate message
                if (!status.AgreementCreated)
                {
                    status.Message = "Agreement not yet created by landlord.";
                }
                else if (status.IsPrimaryTenant && !status.AgreementAccepted)
                {
                    status.Message = "Please accept the agreement to enable access for all family members.";
                }
                else if (!status.IsPrimaryTenant && !status.AgreementAccepted)
                {
                    status.Message = "Waiting for primary tenant to accept the agreement.";
                }
                else
                {
                    status.Message = "Agreement accepted. Full access granted.";
                }

                return Result<AgreementStatusDto>.Success(status);
            }
            catch (Exception ex)
            {
                return Result<AgreementStatusDto>.Failure($"Failed to get agreement status: {ex.Message}");
            }
        }
        public async Task<Result<TenantDto>> GetTenantByEmail(string email)
        {
            try
            {
                var tenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Email == email &&
                                              t.IsActive.HasValue && t.IsActive.Value);

                if (tenant == null)
                    return Result<TenantDto>.NotFound();

                var tenantDto = await MapToDto(tenant);
                return Result<TenantDto>.Success(tenantDto);
            }
            catch (Exception ex)
            {
                return Result<TenantDto>.Failure($"Failed to get tenant by email: {ex.Message}");
            }
        }

        #endregion

        #region Document Management

        /// <summary>
        /// Uploads a document for a specific tenant
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant</param>
        /// <param name="file">The file to upload</param>
        /// <param name="category">The document category (e.g., "IdProof", "AddressProof")</param>
        /// <param name="description">Optional description for the document</param>
        /// <returns>A result indicating success or failure of the document upload</returns>
        /// <remarks>
        /// This method:
        /// - Validates tenant existence before upload
        /// - Associates document with tenant, landlord, and property
        /// - Uses the document service for actual file storage
        /// - Supports various document categories for organization
        /// </remarks>
        public async Task<Result<bool>> UploadTenantDocument(long tenantId, IFormFile file, string category, string description)
        {
            // Input validation
            if (tenantId <= 0)
                return Result<bool>.Failure("Valid tenant ID is required");

            if (file == null || file.Length == 0)
                return Result<bool>.Failure("Valid file is required");

            if (string.IsNullOrWhiteSpace(category))
                return Result<bool>.Failure("Document category is required");

            try
            {
                var tenant = await _context.Tenant.FindAsync(tenantId);
                if (tenant == null)
                    return Result<bool>.NotFound();

                // Create document upload request using your existing structure
                var documentUploadRequest = new DocumentUploadRequestDto
                {
                    Documents = new List<DocumentDto>
                    {
                        new DocumentDto
                        {
                            File = file,
                            OwnerId = tenantId,
                            OwnerType = "Tenant",
                            TenantId = tenantId,
                            LandlordId = tenant.LandlordId,
                            PropertyId = tenant.PropertyId,
                            Category = Enum.Parse<DocumentCategory>(category ?? "Other"),
                            Description = description
                        }
                    }
                };

                // Use your existing document service
                var uploadResult = await UploadDocuments(documentUploadRequest);

                if (uploadResult.Status == ResultStatusType.Success)
                {
                    return Result<bool>.Success(true);
                }
                else if (uploadResult.Status == ResultStatusType.NotFound)
                {
                    return Result<bool>.NotFound();
                }
                else
                {
                    return Result<bool>.Failure(uploadResult.Message);
                }
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to upload document: {ex.Message}");
            }
        }

        #endregion

        #region Statistics & Reports

        /// <summary>
        /// Generates comprehensive statistics for tenants belonging to a specific landlord
        /// </summary>
        /// <param name="landlordId">The unique identifier of the landlord</param>
        /// <returns>A result containing tenant statistics or an error message</returns>
        /// <remarks>
        /// This method calculates:
        /// - Total number of tenants
        /// - Count of active vs inactive tenants
        /// - Number of tenants pending onboarding
        /// - Total monthly rent collection
        /// - Average rent per tenant
        /// Used for landlord dashboard and reporting features.
        /// </remarks>
        public async Task<Result<TenantStatisticsDto>> GetTenantStatistics(long landlordId)
        {
            // Input validation
            if (landlordId <= 0)
                return Result<TenantStatisticsDto>.Failure("Valid landlord ID is required");

            try
            {
                var tenants = await _context.Tenant
                    .Where(t => t.LandlordId == landlordId)
                    .ToListAsync();

                var active = tenants.Where(t => t.IsActive == true).ToList();
                var inactive = tenants.Where(t => t.IsActive == false).ToList();

                var pendingOnboarding = tenants.Where(t => t.OnboardingEmailDate.HasValue).ToList();
                var totalMonthlyRent = active.Sum(t => t.RentAmount);

                var statistics = new TenantStatisticsDto
                {
                    Total = tenants.Count,
                    Active = active.Count,
                    Inactive = inactive.Count,
                    PendingOnboarding = pendingOnboarding.Count,
                    TotalMonthlyRent = totalMonthlyRent,
                    AverageRent = active.Count > 0 ? totalMonthlyRent / active.Count : 0
                };

                return Result<TenantStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                return Result<TenantStatisticsDto>.Failure($"Failed to get tenant statistics: {ex.Message}");
            }
        }



        #endregion

        #region Validation

        /// <summary>
        /// Validates a single tenant's data for completeness and correctness
        /// </summary>
        /// <param name="tenant">The tenant DTO to validate</param>
        /// <param name="create">Whether this is for tenant creation (enables email uniqueness check)</param>
        /// <returns>A list of validation errors, empty if validation passes</returns>
        /// <remarks>
        /// This method validates:
        /// - Required fields (name, email, phone, DOB, occupation)
        /// - Data format (Aadhaar number, PAN number)
        /// - Email uniqueness (only during creation)
        /// - Business rules (rent amount, property ID)
        /// </remarks>
        public List<ValidationErrorDto> ValidateTenant(TenantDto tenant, bool create = false)
        {
            var errors = new List<ValidationErrorDto>();

            // Input validation
            if (tenant == null)
            {
                errors.Add(new ValidationErrorDto { Field = "tenant", Message = "Tenant data is required" });
                return errors;
            }

            if (string.IsNullOrWhiteSpace(tenant.Name) || tenant.Name.Length < MINIMUM_NAME_LENGTH)
                errors.Add(new ValidationErrorDto { Field = "name", Message = $"Name must be at least {MINIMUM_NAME_LENGTH} characters long" });
            if (create == true && !string.IsNullOrWhiteSpace(tenant.Email))
            {
                var trimmedEmail = tenant.Email.Trim();
                var email = this._context.Tenant.Where(x => x.Email == trimmedEmail).ToList();
                if (email.Any())
                {
                    errors.Add(new ValidationErrorDto { Field = "email", Message = "Emails already used" });
                }
            }

            if (string.IsNullOrWhiteSpace(tenant.Email))
                errors.Add(new ValidationErrorDto { Field = "email", Message = "Valid email address is required" });

            if (string.IsNullOrWhiteSpace(tenant.PhoneNumber))
                errors.Add(new ValidationErrorDto { Field = "phoneNumber", Message = "Valid phone number is required" });

            if (tenant.DOB == default)
                errors.Add(new ValidationErrorDto { Field = "dob", Message = "Date of birth is required" });

            if (string.IsNullOrWhiteSpace(tenant.Occupation) || tenant.Occupation.Length < MINIMUM_NAME_LENGTH)
                errors.Add(new ValidationErrorDto { Field = "occupation", Message = "Occupation is required" });

            if (string.IsNullOrWhiteSpace(tenant.AadhaarNumber) || !IsValidAadhaar(tenant.AadhaarNumber))
                errors.Add(new ValidationErrorDto { Field = "aadhaarNumber", Message = $"Valid {AADHAAR_NUMBER_LENGTH}-digit Aadhaar number is required" });

            if (string.IsNullOrWhiteSpace(tenant.PanNumber) || !IsValidPAN(tenant.PanNumber))
                errors.Add(new ValidationErrorDto { Field = "PanNumber", Message = "Valid PAN number is required (e.g., ABCDE1234F)" });

            if (tenant.PropertyId <= 0)
                errors.Add(new ValidationErrorDto { Field = "propertyId", Message = "Property selection is required" });

            if (tenant.RentAmount <= 0)
                errors.Add(new ValidationErrorDto { Field = "rentAmount", Message = "Valid rent amount is required" });

            if (tenant.TenancyStartDate == default)
                errors.Add(new ValidationErrorDto { Field = "tenancyStartDate", Message = "Tenancy start date is required" });

            if (tenant.RentDueDate == default)
                errors.Add(new ValidationErrorDto { Field = "rentDueDate", Message = "Rent due date is required" });

            return errors;
        }

        /// <summary>
        /// Validates a group of tenants for consistency and business rules
        /// </summary>
        /// <param name="tenants">The list of tenants to validate as a group</param>
        /// <param name="isSingleTenant">Whether this is a single tenant scenario</param>
        /// <param name="isCreate">Whether this is for tenant creation</param>
        /// <returns>A list of validation errors, empty if validation passes</returns>
        /// <remarks>
        /// This method validates:
        /// - At least one tenant is provided
        /// - Exactly one primary tenant (unless single tenant)
        /// - Individual tenant validation for each tenant
        /// - No duplicate email addresses within the group
        /// - Group-level business rules
        /// </remarks>
        public List<ValidationErrorDto> ValidateTenantGroup(List<TenantDto> tenants, bool isSingleTenant = false, bool isCreate = false)
        {
            var errors = new List<ValidationErrorDto>();

            // Input validation
            if (tenants == null)
            {
                errors.Add(new ValidationErrorDto { Field = "tenants", Message = "Tenants list cannot be null" });
                return errors;
            }

            if (!tenants.Any())
            {
                errors.Add(new ValidationErrorDto { Field = "tenants", Message = "At least one tenant is required" });
                return errors;
            }

            // Check that exactly one tenant is marked as primary
            if (!isSingleTenant)
            {
                var primaryCount = tenants.Count(t => t.IsPrimary == true);
                if (primaryCount == 0)
                    errors.Add(new ValidationErrorDto { Field = "tenants", Message = "One tenant must be marked as primary" });
                else if (primaryCount > 1)
                    errors.Add(new ValidationErrorDto { Field = "tenants", Message = "Only one tenant can be marked as primary" });
            }
            // Validate each tenant
            for (int i = 0; i < tenants.Count; i++)
            {
                var tenantErrors = ValidateTenant(tenants[i], isCreate);
                foreach (var error in tenantErrors)
                {
                    error.Field = $"tenants[{i}].{error.Field}";
                    errors.Add(error);
                }
            }

            // Check for duplicate emails
            var emails = tenants.Where(t => !string.IsNullOrWhiteSpace(t.Email)).Select(t => t.Email!.ToLower()).ToList();
            var duplicateEmails = emails.GroupBy(e => e).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var email in duplicateEmails)
            {
                errors.Add(new ValidationErrorDto { Field = "email", Message = $"Duplicate email address: {email}" });
            }

            // Check for duplicate phone numbers
            //var phones = tenants.Where(t => !string.IsNullOrWhiteSpace(t.PhoneNumber)).Select(t => t.PhoneNumber).ToList();
            //var duplicatePhones = phones.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);
            //foreach (var phone in duplicatePhones)
            //{
            //    errors.Add(new TenantValidationErrorDto { Field = "phoneNumber", Message = $"Duplicate phone number: {phone}" });
            //}

            return errors;
        }

        #endregion

        #region Email Helper Methods

        #region Agreement Email Methods

        /// <summary>
        /// Sends agreement creation email to the primary tenant
        /// </summary>
        /// <param name="tenant">The tenant to send the agreement email to</param>
        /// <remarks>
        /// This method:
        /// - Generates agreement email with property details
        /// - Includes login URL for tenant portal access
        /// - Updates tenant record with email sent status
        /// - Handles email failures gracefully without breaking agreement creation
        /// </remarks>
        private async Task SendAgreementEmail(Tenant tenant)
        {
            // Input validation
            if (tenant == null)
            {
                LogError("SendAgreementEmail", new ArgumentNullException(nameof(tenant)));
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(tenant.Email))
                    return;

                var mailRequest = new MailRequestDto
                {
                    ToEmail = tenant.Email ?? string.Empty,
                    Subject = "Rental Agreement Created - Action Required",
                    Body = GenerateAgreementEmailBody(tenant)
                };

                var emailResult = await _mailService.SendEmailAsync(mailRequest);

                if (emailResult.IsSuccess)
                {
                    // Update tenant with email sent status
                    tenant.AgreementEmailSent = true;
                    tenant.AgreementEmailDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the agreement creation
                LogError($"SendAgreementEmail to tenant {tenant.Id}", ex);
            }
        }

        private string GenerateAgreementEmailBody(Tenant tenant)
        {
            var confirmationUrl = $"{_serverSettings.BaseUrl}/Account/ResetPasswordTenant?email={HttpUtility.UrlEncode(tenant.Email)}";
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .details {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Rental Agreement Created</h1>
        </div>
        <div class='content'>
            <p>Dear {tenant.Name},</p>
            
            <p>Your rental agreement has been created and is ready for your review and acceptance.</p>
            
            <div class='details'>
                <h3>Agreement Details:</h3>
                <p><strong>Property:</strong> {tenant.Property?.Title} - {tenant.Property?.Locality}, {tenant.Property?.City}</p>
                <p><strong>Tenancy Start Date:</strong> {tenant.TenancyStartDate:dd MMM yyyy}</p>
                <p><strong>Tenancy End Date:</strong> {tenant.TenancyEndDate:dd MMM yyyy}</p>
                <p><strong>Monthly Rent:</strong> ₹{tenant.RentAmount:N0}</p>
                <p><strong>Security Deposit:</strong> ₹{tenant.SecurityDeposit:N0}</p>
                <p><strong>Lease Duration:</strong> {tenant.LeaseDuration} months</p>
            </div>
            
            <div class='warning'>
                <h4>⚠️ Important Notice:</h4>
                <p>As the <strong>primary tenant</strong>, you must accept this agreement before any family members can access the tenant portal. Other family members will not be able to log in until you have accepted the agreement.</p>
            </div>
            
            <p>Please log in to your tenant portal to review and accept the agreement:</p>
            
            <div style='text-align: center;'>
                <a href='{confirmationUrl}' class='button'>Login to Tenant Portal</a>
            </div>
            
            <p>If you have any questions about the agreement terms, please contact your landlord or our support team.</p>
        </div>
        <div class='footer'>
            <p>Thank you for choosing RentConnect</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task SendAgreementAcceptanceNotificationToLandlord(Tenant tenant)
        {
            // Get landlord's email from AspNetUsers through proper relationship
            var landlord = await _context.Landlord.FirstOrDefaultAsync(l => l.Id == tenant.LandlordId);
            var email = landlord != null ?
                _context.Users.FirstOrDefault(x => x.Id == landlord.ApplicationUserId)?.Email :
                null;
            try
            {
                if (email == null)
                    return;

                var mailRequest = new MailRequestDto
                {
                    ToEmail = email,
                    Subject = $"Agreement Accepted - {tenant.Name} ({tenant.Property?.Title})",
                    Body = GenerateLandlordNotificationEmailBody(tenant)
                };

                await _mailService.SendEmailAsync(mailRequest);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the agreement acceptance
                LogError($"SendLandlordNotification for tenant {tenant.Id}", ex);
            }
        }

        private string GenerateLandlordNotificationEmailBody(Tenant tenant)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .details {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .success {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Agreement Accepted!</h1>
        </div>
        <div class='content'>
            <div class='success'>
                <h3>✅ Great News!</h3>
                <p>Your tenant <strong>{tenant.Name}</strong> has accepted the rental agreement. You can now proceed with the onboarding process.</p>
            </div>
            
            <div class='details'>
                <h3>Tenant Details:</h3>
                <p><strong>Primary Tenant:</strong> {tenant.Name}</p>
                <p><strong>Email:</strong> {tenant.Email}</p>
                <p><strong>Property:</strong> {tenant.Property?.Title} - {tenant.Property?.Locality}, {tenant.Property?.City}</p>
                <p><strong>Agreement Accepted:</strong> {tenant.AgreementAcceptedDate:dd MMM yyyy 'at' HH:mm}</p>
                <p><strong>Tenancy Start:</strong> {tenant.TenancyStartDate:dd MMM yyyy}</p>
                <p><strong>Monthly Rent:</strong> ₹{tenant.RentAmount:N0}</p>
            </div>
            
            <div class='success'>
                <h4>Next Steps:</h4>
                <p>1. You can now send onboarding emails to all eligible tenants</p>
                <p>2. All family members will now have access to the tenant portal</p>
                <p>3. The tenancy is ready to begin on {tenant.TenancyStartDate:dd MMM yyyy}</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='#' class='button'>View Tenant Dashboard</a>
            </div>
        </div>
        <div class='footer'>
            <p>RentConnect - Property Management System</p>
            <p>This is an automated notification.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets tenants with standard includes (Property and Landlord)
        /// </summary>
        private IQueryable<Tenant> GetTenantsWithIncludes()
        {
            return _context.Tenant
                .Include(t => t.Property)
                .Include(t => t.Landlord);
        }

        /// <summary>
        /// Loads documents for a single tenant
        /// </summary>
        private async Task LoadTenantDocuments(Tenant tenant)
        {
            var docs = await _context.Document
                .Where(x => x.LandlordId == tenant.LandlordId &&
                           x.PropertyId == tenant.PropertyId &&
                           x.TenantId == tenant.Id &&
                           x.UploadContext == DocumentUploadContext.TenantCreation)
                .ToListAsync();
            tenant.Documents = docs;
        }

        /// <summary>
        /// Maps a list of tenants to DTOs with their documents loaded
        /// </summary>
        private async Task<List<TenantDto>> MapTenantsWithDocuments(List<Tenant> tenants)
        {
            var tenantDtos = new List<TenantDto>();
            foreach (var tenant in tenants)
            {
                await LoadTenantDocuments(tenant);
                var dto = await MapToDto(tenant);
                tenantDtos.Add(dto);
            }
            return tenantDtos;
        }

        /// <summary>
        /// Applies common request-level values to all tenants in the request
        /// </summary>
        private void ApplyRequestValuesToTenants(TenantCreateRequestDto request)
        {
            if (request?.Tenants == null) return;

            foreach (var tenant in request.Tenants)
            {
                tenant.PropertyId = request.PropertyId;
                tenant.RentAmount = request.RentAmount;
                tenant.SecurityDeposit = request.SecurityDeposit;
                tenant.MaintenanceCharges = request.MaintenanceCharges;
                tenant.TenancyStartDate = request.TenancyStartDate;
                tenant.TenancyEndDate = request.TenancyEndDate;
                tenant.RentDueDate = request.RentDueDate;
                tenant.LeaseDuration = request.LeaseDuration > 0 ? request.LeaseDuration : DEFAULT_LEASE_DURATION;
                tenant.NoticePeriod = request.NoticePeriod > 0 ? request.NoticePeriod : DEFAULT_NOTICE_PERIOD;
                tenant.LandlordId = request.LandlordId;
            }
        }

        /// <summary>
        /// Filters tenants by age requirement for onboarding eligibility
        /// </summary>
        /// <param name="tenants">The tenants to filter</param>
        /// <returns>Tenants who meet the minimum age requirement</returns>
        private List<Tenant> FilterTenantsByAge(List<Tenant> tenants)
        {
            if (tenants == null) return new List<Tenant>();

            var today = DateTime.Today;
            var cutoffDate = today.AddYears(-MINIMUM_AGE_FOR_ONBOARDING);

            return tenants.Where(t => t.DOB.HasValue && t.DOB.Value <= cutoffDate).ToList();
        }

        /// <summary>
        /// Filters tenants who have valid email addresses and are active
        /// </summary>
        /// <param name="tenants">The tenants to filter</param>
        /// <returns>Tenants with valid emails who are active</returns>
        private List<Tenant> FilterTenantsWithValidEmails(List<Tenant> tenants)
        {
            if (tenants == null) return new List<Tenant>();

            return tenants.Where(t => !string.IsNullOrEmpty(t.Email) &&
                                     t.IsActive.HasValue && t.IsActive.Value).ToList();
        }

        /// <summary>
        /// Checks if the primary tenant in a group has accepted the agreement
        /// </summary>
        /// <param name="tenantGroup">The tenant group identifier</param>
        /// <returns>True if primary tenant has accepted agreement</returns>
        private async Task<bool> IsPrimaryTenantAgreementAccepted(string tenantGroup)
        {
            if (string.IsNullOrEmpty(tenantGroup)) return false;

            return await _context.Tenant
                .AnyAsync(t => t.TenantGroup == tenantGroup &&
                              t.IsPrimary.HasValue && t.IsPrimary.Value &&
                              t.AgreementAccepted.HasValue && t.AgreementAccepted.Value);
        }

        /// <summary>
        /// Centralized logging method for consistent error logging
        /// </summary>
        private void LogError(string operation, Exception ex, object? context = null)
        {
            var contextInfo = context != null ? $" Context: {System.Text.Json.JsonSerializer.Serialize(context)}" : "";
            Console.WriteLine($"[TenantService] {operation} failed: {ex.Message}{contextInfo}");
        }

        /// <summary>
        /// Centralized logging method for informational messages
        /// </summary>
        private void LogInfo(string message)
        {
            Console.WriteLine($"[TenantService] {message}");
        }
        private Task<TenantDto> MapToDto(Tenant tenant)
        {
            var dto = new TenantDto
            {
                Id = tenant.Id,
                LandlordId = tenant.LandlordId,
                PropertyId = tenant.PropertyId,

                Name = tenant.Name,
                Email = tenant.Email,
                PhoneNumber = tenant.PhoneNumber,
                AlternatePhoneNumber = tenant.AlternatePhoneNumber,
                DOB = tenant.DOB,
                Occupation = tenant.Occupation,
                Gender = tenant.Gender,
                MaritalStatus = tenant.MaritalStatus,

                CurrentAddress = tenant.CurrentAddress,
                PermanentAddress = tenant.PermanentAddress,

                EmergencyContactName = tenant.EmergencyContactName,
                EmergencyContactPhone = tenant.EmergencyContactPhone,
                EmergencyContactRelation = tenant.EmergencyContactRelation,

                AadhaarNumber = tenant.AadhaarNumber,
                PanNumber = tenant.PanNumber,
                DrivingLicenseNumber = tenant.DrivingLicenseNumber,
                VoterIdNumber = tenant.VoterIdNumber,

                EmployerName = tenant.EmployerName,
                EmployerAddress = tenant.EmployerAddress,
                EmployerPhone = tenant.EmployerPhone,
                MonthlyIncome = tenant.MonthlyIncome,
                WorkExperience = tenant.WorkExperience,

                TenancyStartDate = tenant.TenancyStartDate,
                TenancyEndDate = tenant.TenancyEndDate,
                RentDueDate = tenant.RentDueDate,
                RentAmount = tenant.RentAmount,
                SecurityDeposit = tenant.SecurityDeposit,
                MaintenanceCharges = tenant.MaintenanceCharges,
                LeaseDuration = tenant.LeaseDuration,
                NoticePeriod = tenant.NoticePeriod,

                AgreementSigned = tenant.AgreementSigned,
                AgreementDate = tenant.AgreementDate,
                AgreementUrl = tenant.AgreementUrl,
                AgreementEmailSent = tenant.AgreementEmailSent,
                AgreementEmailDate = tenant.AgreementEmailDate,
                AgreementAccepted = tenant.AgreementAccepted,
                AgreementAcceptedDate = tenant.AgreementAcceptedDate,
                AgreementAcceptedBy = tenant.AgreementAcceptedBy,
                OnboardingEmailSent = tenant.OnboardingEmailSent,
                OnboardingEmailDate = tenant.OnboardingEmailDate,
                OnboardingCompleted = tenant.OnboardingCompleted,

                IsAcknowledge = tenant.IsAcknowledge,
                AcknowledgeDate = tenant.AcknowledgeDate,
                IsVerified = tenant.IsVerified,
                VerificationNotes = tenant.VerificationNotes,

                IsNewTenant = tenant.IsNewTenant,
                IsPrimary = tenant.IsPrimary,
                IsActive = tenant.IsActive,

                // Relationship and Email preferences
                Relationship = tenant.Relationship,
                IncludeInEmail = tenant.IncludeInEmail,

                TenantGroup = tenant.TenantGroup,

                IpAddress = tenant.IpAddress,
                DateCreated = tenant.DateCreated,
                DateModified = tenant.DateModified,


                PropertyName = tenant.Property != null ? $"{tenant.Property.Title} - {tenant.Property.Locality}, {tenant.Property.City}" : "",
                Documents = tenant.Documents.Select(d => new DocumentDto
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
                }).ToList()
            };

            return Task.FromResult(dto);
        }

        private Tenant MapToEntity(TenantDto dto, TenantCreateRequestDto request)
        {
            return new Tenant
            {
                LandlordId = request.LandlordId ?? 0,
                PropertyId = request.PropertyId ?? 0,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DOB = dto.DOB,
                Occupation = dto.Occupation,
                AadhaarNumber = dto.AadhaarNumber,
                PanNumber = dto.PanNumber,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelation = dto.EmergencyContactRelation,
                TenancyStartDate = request.TenancyStartDate,
                TenancyEndDate = request.TenancyEndDate,
                RentDueDate = request.RentDueDate,
                RentAmount = request.RentAmount,
                LeaseDuration = request.LeaseDuration,
                MaintenanceCharges = request.MaintenanceCharges,
                SecurityDeposit = request.SecurityDeposit,
                // below properties need to implement in ui
                IsAcknowledge = false,
                IsVerified = false,
                IsNewTenant = true,
                IsPrimary = dto.IsPrimary,
                IsActive = true,
                IsDeleted = false,
                OnboardingEmailSent = false,
                OnboardingCompleted = false,
                AgreementSigned = false,

                // Relationship and Email preferences
                Relationship = dto.Relationship ?? DEFAULT_RELATIONSHIP, // Default to Adult if not specified
                IncludeInEmail = dto.IncludeInEmail ?? true // Default to true if not specified
            };
        }

        private void UpdateEntityFromDto(Tenant entity, TenantDto dto)
        {
            entity.Name = dto.Name;
            entity.Email = dto.Email;
            entity.PhoneNumber = dto.PhoneNumber;
            entity.DOB = dto.DOB;
            entity.Occupation = dto.Occupation;
            entity.AadhaarNumber = dto.AadhaarNumber;
            entity.PanNumber = dto.PanNumber;
            entity.EmergencyContactName = dto.EmergencyContactName;
            entity.EmergencyContactPhone = dto.EmergencyContactPhone;
            entity.EmergencyContactRelation = dto.EmergencyContactRelation;
            entity.TenancyStartDate = dto.TenancyStartDate;
            entity.TenancyEndDate = dto.TenancyEndDate;
            entity.RentDueDate = dto.RentDueDate;
            entity.RentAmount = dto.RentAmount;
            entity.LeaseDuration = dto.LeaseDuration;
            entity.MaintenanceCharges = dto.MaintenanceCharges;
            entity.SecurityDeposit = dto.SecurityDeposit;
            //entity.IsAcknowledge = dto.IsAcknowledge;
            //entity.IsVerified = dto.IsVerified;
            //entity.IsNewTenant = dto.IsNewTenant;
            //entity.IsPrimary = dto.IsPrimary;
            //entity.IsActive = dto.IsActive;

            // Update relationship and email preferences
            entity.Relationship = dto.Relationship;
            entity.IncludeInEmail = dto.IncludeInEmail;
        }

        private List<TenantDto> MapRequestToDto(TenantCreateRequestDto request)
        {
            return request.Tenants.Select(dto => new TenantDto
            {
                LandlordId = request.LandlordId ?? 0,
                PropertyId = request.PropertyId ?? 0,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DOB = dto.DOB,
                Occupation = dto.Occupation,
                AadhaarNumber = dto.AadhaarNumber,
                PanNumber = dto.PanNumber,
                TenancyStartDate = request.TenancyStartDate ?? DateTime.UtcNow,
                TenancyEndDate = request.TenancyEndDate,
                RentDueDate = request.RentDueDate ?? DateTime.UtcNow,
                RentAmount = request.RentAmount ?? 0,
                SecurityDeposit = request.SecurityDeposit ?? 0,
                IsAcknowledge = false,
                IsVerified = false,
                IsNewTenant = true,
                IsPrimary = dto.IsPrimary,
                IsActive = true,
                OnboardingEmailSent = false,
                OnboardingCompleted = false,
                AgreementSigned = false,

                // Relationship and Email preferences
                Relationship = dto.Relationship ?? DEFAULT_RELATIONSHIP, // Default to Adult if not specified
                IncludeInEmail = dto.IncludeInEmail ?? true // Default to true if not specified
            }).ToList();
        }

        private async Task<Result<IEnumerable<DocumentDto>>> SaveTenantDocuments(long tenantId, List<DocumentDto> documents, DocumentUploadContext uploadContext = DocumentUploadContext.None)
        {
            try
            {
                if (documents == null || !documents.Any())
                    return Result<IEnumerable<DocumentDto>>.Success(Enumerable.Empty<DocumentDto>());

                var tenant = await _context.Tenant.FindAsync(tenantId);
                if (tenant == null)
                    return Result<IEnumerable<DocumentDto>>.Failure("Tenant not found.");

                // Select only new documents with files
                var documentsWithFiles = documents
                    .Where(d => d.File != null && d.Id == null)
                    .ToList();

                if (!documentsWithFiles.Any())
                    return Result<IEnumerable<DocumentDto>>.Success(Enumerable.Empty<DocumentDto>());

                // Set tenant-specific metadata
                foreach (var doc in documentsWithFiles)
                {
                    doc.OwnerId = tenant.LandlordId;
                    doc.OwnerType = "Landlord";
                    doc.TenantId = tenantId;
                    doc.LandlordId = tenant.LandlordId;
                    doc.PropertyId = tenant.PropertyId;
                    doc.UploadContext = uploadContext;

                    if (string.IsNullOrEmpty(doc.Description))
                        doc.Description = $"Tenant document - {doc.Category}";
                }

                var documentUploadRequest = new DocumentUploadRequestDto
                {
                    Documents = documentsWithFiles
                };

                // Delegate to document service
                return await UploadDocuments(documentUploadRequest);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Failed to save tenant documents: {ex.Message}");
            }
        }

        private async Task<Result<IEnumerable<DocumentDto>>> UploadDocuments(DocumentUploadRequestDto request)
        {
            try
            {
                if (request.Documents == null || !request.Documents.Any())
                    return Result<IEnumerable<DocumentDto>>.Failure("Documents not found");

                var savedDocs = new List<Document>();
                foreach (var doc in request.Documents.Where(d => d.File != null && d.File.Length > 0))
                {
                    if (doc.File == null || doc.OwnerType == null || !doc.OwnerId.HasValue || !doc.Category.HasValue)
                        continue;

                    var fileUrl = await SaveFileAsync(doc.File, doc.OwnerType, doc.OwnerId.Value, doc.Category.Value);
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
                        UploadedOn = DateTime.UtcNow.ToString("o"),
                        IsVerified = true,
                        DocumentIdentifier = null,
                        UploadContext = doc.UploadContext
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

        private async Task<string> SaveFileAsync(IFormFile file, string ownerType, long ownerId, DocumentCategory category)
        {
            // Get the enum name as string
            var categoryType = category.ToString(); // "IdProof", "Other", etc.
            var uploadPath = Path.Combine("wwwroot/uploads", ownerType, ownerId.ToString(), categoryType);
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{ownerType}/{ownerId}/{categoryType}/{fileName}";
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            var phoneRegex = new Regex(@"^[+]?[0-9]{10,15}$");
            return phoneRegex.IsMatch(phone.Replace(" ", "").Replace("-", ""));
        }

        private bool IsValidAadhaar(string aadhaar)
        {
            var aadhaarRegex = new Regex($@"^[0-9]{{{AADHAAR_NUMBER_LENGTH}}}$");
            return aadhaarRegex.IsMatch(aadhaar.Replace(" ", ""));
        }

        private bool IsValidPAN(string pan)
        {
            var panRegex = new Regex(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
            return panRegex.IsMatch(pan.ToUpper());
        }

        #region Onboarding Email Helper Methods

        /// <summary>
        /// Sends onboarding email to a single tenant
        /// </summary>
        private async Task<bool> SendOnboardingEmailToTenant(Tenant tenant)
        {
            try
            {
                // Create password reset URL for tenant
                var confirmationUrl = $"{_serverSettings.BaseUrl}/Account/ResetPasswordTenant?email={HttpUtility.UrlEncode(tenant.Email ?? string.Empty)}";

                // Create attachments list (property documents)
                var attachmentsList = CreateAttachmentsList(tenant);

                // Create email request
                var mailObj = new MailRequestDto()
                {
                    ToEmail = tenant.Email ?? string.Empty,
                    Subject = "Welcome to RentConnect - Complete Your Onboarding",
                    Body = await CreateOnboardingEmailBodyAsync(tenant.Name ?? "Tenant", confirmationUrl, tenant),
                    Attachments = attachmentsList
                };

                // Send email
                var emailResult = await _mailService.SendEmailAsync(mailObj);

                if (emailResult.Status == ResultStatusType.Success)
                {
                    // Update tenant onboarding status
                    tenant.OnboardingEmailSent = true;
                    tenant.OnboardingEmailDate = DateTime.UtcNow;
                    tenant.DateModified = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogError($"SendOnboardingEmailToTenant {tenant.Id}", ex);
                return false;
            }
        }

        private List<AttachmentsDto> CreateAttachmentsList(Tenant tenant)
        {
            var attachments = new List<AttachmentsDto>();

            try
            {
                // Add property documents as attachments if available
                // Note: In a real implementation, you would add relevant property documents
                // For now, returning empty list - can be extended later as needed
            }
            catch (Exception ex)
            {
                LogError($"CreateAttachmentsList for tenant {tenant.Id}", ex);
            }

            return attachments;
        }

        private async Task<string> CreateOnboardingEmailBodyAsync(string tenantName, string confirmationUrl, Tenant tenant)
        {
            return await Task.FromResult($@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .details {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .welcome {{ background-color: #e8f5e8; border: 1px solid #4CAF50; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .important {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏠 Welcome to RentConnect!</h1>
            <p>Your Digital Tenant Portal</p>
        </div>
        <div class='content'>
            <div class='welcome'>
                <h2>Hello {tenantName}! 👋</h2>
                <p>Welcome to your new home and to the RentConnect platform. We're excited to have you as part of our community!</p>
            </div>
            
            <div class='details'>
                <h3>🔐 Complete Your Account Setup</h3>
                <p>To get started with your tenant portal, please complete your account setup by clicking the button below:</p>
                
                <div style='text-align: center; margin: 20px 0;'>
                    <a href='{confirmationUrl}' class='button'>Complete Account Setup</a>
                </div>
                
                <p><strong>What you can do in your tenant portal:</strong></p>
                <ul>
                    <li>📄 View and sign your rental agreement</li>
                    <li>💳 Make rent payments online</li>
                    <li>🔧 Submit maintenance requests</li>
                    <li>📋 Access important property documents</li>
                    <li>📞 Contact your landlord directly</li>
                    <li>📊 Track your payment history</li>
                </ul>
            </div>
            
            <div class='details'>
                <h3>🏡 Property Information</h3>
                <p><strong>Property:</strong> {tenant.Property?.Title}</p>
                <p><strong>Address:</strong> {tenant.Property?.Locality}, {tenant.Property?.City}</p>
                <p><strong>Tenancy Start:</strong> {tenant.TenancyStartDate:dd MMM yyyy}</p>
                <p><strong>Monthly Rent:</strong> ₹{tenant.RentAmount:N0}</p>
            </div>
            
            <div class='important'>
                <h4>⚠️ Important Next Steps:</h4>
                <ol>
                    <li><strong>Complete your account setup</strong> using the link above</li>
                    <li><strong>Review and sign your rental agreement</strong> (primary tenant only)</li>
                    <li><strong>Set up your payment method</strong> for easy rent payments</li>
                    <li><strong>Download the RentConnect mobile app</strong> for convenient access</li>
                </ol>
            </div>
            
            <div class='details'>
                <h3>📞 Need Help?</h3>
                <p>If you have any questions or need assistance:</p>
                <ul>
                    <li>📧 Email: support@rentconnect.com</li>
                    <li>📱 Phone: +91-XXXX-XXXXXX</li>
                    <li>💬 Use the chat feature in your tenant portal</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p><strong>Welcome to your new home! 🏠</strong></p>
            <p>RentConnect - Making Renting Simple</p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>");
        }

        #endregion

        #region AspNetUsers Integration

        /// <summary>
        /// Creates ApplicationUser records for tenants in AspNetUsers table
        /// </summary>
        /// <param name="tenants">List of tenants to create users for</param>
        /// <returns>Dictionary mapping tenant email to created user ID</returns>
        private Task<Dictionary<string, long>> CreateTenantUsers(List<TenantDto> tenants)
        {
            var createdUsers = new Dictionary<string, long>();

            // Filter out tenants that need user accounts (have email and don't already have AspNetUser)
            var tenantsNeedingUsers = tenants.Where(t => !string.IsNullOrEmpty(t.Email)).ToList();

            foreach (var tenant in tenantsNeedingUsers)
            {
                try
                {
                    var applicationUserDto = MapTenantToApplicationUser(tenant);

                    // Use UserService to create the user with proper role assignment
                    var userId = _userService.CreateUser(applicationUserDto);

                    if (userId > 0)
                    {
                        createdUsers[tenant.Email ?? string.Empty] = userId;
                        LogInfo($"✅ Created AspNetUser for tenant: {tenant.Email} with Tenant role (ID: {userId})");
                    }
                    else if (userId == -1)
                    {
                        // User already exists - this is acceptable for tenant updates
                        LogInfo($"ℹ️ AspNetUser already exists for tenant: {tenant.Email}");
                    }
                    else
                    {
                        LogInfo($"❌ Failed to create AspNetUser for tenant: {tenant.Email}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"CreateTenantUser for {tenant.Email}", ex);
                }
            }

            return Task.FromResult(createdUsers);
        }

        /// <summary>
        /// Maps tenant information to ApplicationUserDto for AspNetUsers creation
        /// </summary>
        /// <param name="tenant">Tenant information</param>
        /// <returns>ApplicationUserDto for user creation</returns>
        private ApplicationUserDto MapTenantToApplicationUser(TenantDto tenant)
        {
            // Extract first and last name from full name
            var nameParts = tenant.Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "Unknown" };
            var firstName = nameParts.FirstOrDefault() ?? "Unknown";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            return new ApplicationUserDto
            {
                Email = tenant.Email ?? string.Empty,
                UserName = tenant.Email ?? string.Empty, // Using email as username
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = tenant.PhoneNumber ?? "",
                Address = tenant.CurrentAddress ?? "",
                IsEnabled = true,
                IsResetPassword = true, // Tenant will need to set password on first login
                DateCreated = DateTime.UtcNow,
                UserRoles = new List<ApplicationUserRoleDto>
                {
                    new ApplicationUserRoleDto
                    {
                        Id = ApplicationUserRole.Tenant,
                        Name = TENANT_ROLE_NAME
                    }
                },
                // Generate a temporary password that tenant must change
                Password = GenerateTemporaryPassword()
            };
        }

        /// <summary>
        /// Generates a temporary password for new tenant users
        /// </summary>
        /// <returns>Temporary password string</returns>
        private string GenerateTemporaryPassword()
        {
            // Generate a secure temporary password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, TEMPORARY_PASSWORD_LENGTH)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Updates existing AspNetUser records for tenants
        /// </summary>
        /// <param name="tenants">List of tenants to update</param>
        /// <returns>Dictionary mapping tenant email to updated user ID</returns>
        private async Task<Dictionary<string, long>> UpdateTenantUsers(List<TenantDto> tenants)
        {
            var updatedUsers = new Dictionary<string, long>();

            foreach (var tenant in tenants)
            {
                try
                {
                    // Find existing user by email
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == tenant.Email);

                    if (existingUser != null)
                    {
                        var applicationUserDto = MapTenantToApplicationUser(tenant);
                        applicationUserDto.ApplicationUserId = existingUser.Id;

                        var userId = _userService.UpdateUser(applicationUserDto);
                        if (userId > 0)
                        {
                            updatedUsers[tenant.Email ?? string.Empty] = userId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"UpdateTenantUser for {tenant.Email}", ex);
                }
            }

            return updatedUsers;
        }

        /// <summary>
        /// Checks if the agreement has been accepted by the primary tenant in the group
        /// </summary>
        /// <param name="tenant">The tenant to check</param>
        /// <returns>True if primary tenant has accepted agreement, false otherwise</returns>
        private bool IsAgreementStarted(Tenant tenant)
        {
            // Agreement is considered "started" if the PRIMARY tenant in the group has accepted it
            // Only primary tenant can accept agreements, so we need to check the primary tenant's status

            // Find the primary tenant in the same group
            var primaryTenant = _context.Tenant
                .FirstOrDefault(t => t.TenantGroup == tenant.TenantGroup &&
                                    t.IsPrimary.HasValue && t.IsPrimary.Value);

            if (primaryTenant == null)
                return false;

            // Check if primary tenant has accepted the agreement
            bool agreementAccepted = primaryTenant.AgreementAccepted.HasValue && primaryTenant.AgreementAccepted.Value;

            return agreementAccepted;
        }

        /// <summary>
        /// Updates only the email field from DTO when agreement has started
        /// </summary>
        /// <param name="entity">The tenant entity to update</param>
        /// <param name="dto">The DTO containing new values</param>
        private void UpdateEmailOnlyFromDto(Tenant entity, TenantDto dto)
        {
            // Only allow email updates when agreement has started
            entity.Email = dto.Email;
        }


        #endregion

        #endregion

        #endregion
    }
}
