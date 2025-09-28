
using Microsoft.AspNetCore.Http;
using RentConnect.Models.Dtos;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.Tenants;
using RentConnect.Models.Enums;
using RentConnect.Services.Utility;
using System.Text.RegularExpressions;

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
    public class TenantService : ITenantService
    {
        private readonly ApiContext _context;
        private readonly IDocumentService _documentService;
        private readonly IMailService _mailService;
        private readonly ServerSettings _serverSettings;
        private readonly IUserService _userService;

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

        public async Task<Result<IEnumerable<TenantDto>>> GetAllTenants()
        {
            try
            {
                var tenants = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .ToListAsync();

                var tenantDtos = new List<TenantDto>();
                foreach (var tenant in tenants)
                {
                    var dto = await MapToDto(tenant);
                    tenantDtos.Add(dto);
                }

                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants: {ex.Message}");
            }
        }

        public async Task<Result<TenantDto>> GetTenantById(long id)
        {
            try
            {
                var tenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                    return Result<TenantDto>.NotFound();

                var tenantDto = await MapToDto(tenant);
                return Result<TenantDto>.Success(tenantDto);
            }
            catch (Exception ex)
            {
                return Result<TenantDto>.Failure($"Failed to get tenant: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TenantDto>>> GetTenantsByProperty(long propertyId)
        {
            try
            {
                var tenants = await _context.Tenant
                    .Where(t => t.PropertyId == propertyId)
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .ToListAsync();

                var tenantDtos = new List<TenantDto>();
                foreach (var tenant in tenants)
                {
                    var dto = await MapToDto(tenant);
                    tenantDtos.Add(dto);
                }

                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants by property: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TenantDto>>> GetTenantsByLandlord(long landlordId)
        {
            try
            {
                var tenants = await _context.Tenant
                    .Where(t => t.LandlordId == landlordId && t.IsActive == true && t.IsDeleted == false)
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .ToListAsync();

                var tenantDtos = new List<TenantDto>();
                foreach (var tenant in tenants)
                {
                    var dto = await MapToDto(tenant);
                    tenantDtos.Add(dto);
                }

                return Result<IEnumerable<TenantDto>>.Success(tenantDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TenantDto>>.Failure($"Failed to get tenants by landlord: {ex.Message}");
            }
        }

        public async Task<Result<TenantSaveResponseDto>> CreateTenants(TenantCreateRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var tenant in request.Tenants)
                {
                    tenant.PropertyId = request.PropertyId;
                    tenant.RentAmount = request.RentAmount;
                    tenant.SecurityDeposit = request.SecurityDeposit;
                    tenant.MaintenanceCharges = request.MaintenanceCharges;
                    tenant.TenancyStartDate = request.TenancyStartDate;
                    tenant.TenancyEndDate = request.TenancyEndDate; // optional if provided
                    tenant.RentDueDate = request.RentDueDate;
                    tenant.LeaseDuration = request.LeaseDuration > 0 ? request.LeaseDuration : 12;
                    tenant.NoticePeriod = request.NoticePeriod > 0 ? request.NoticePeriod : 30;
                    tenant.LandlordId = request.LandlordId;
                }

                // Validate the request
                var validationErrors = ValidateTenantGroup(request.Tenants);
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
                        await SaveTenantDocuments(tenant.Id, tenantDto.Documents);
                    }

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

        public async Task<Result<TenantSaveResponseDto>> UpdateTenant(TenantCreateRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Apply common request-level values to all tenants
                foreach (var tenant in request.Tenants)
                {
                    tenant.PropertyId = request.PropertyId;
                    tenant.RentAmount = request.RentAmount;
                    tenant.SecurityDeposit = request.SecurityDeposit;
                    tenant.MaintenanceCharges = request.MaintenanceCharges;
                    tenant.TenancyStartDate = request.TenancyStartDate;
                    tenant.TenancyEndDate = request.TenancyEndDate; // optional
                    tenant.RentDueDate = request.RentDueDate;
                    tenant.LeaseDuration = request.LeaseDuration > 0 ? request.LeaseDuration : 12;
                    tenant.NoticePeriod = request.NoticePeriod > 0 ? request.NoticePeriod : 30;
                    tenant.LandlordId = request.LandlordId;
                }

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
                        await SaveTenantDocuments(existingTenant.Id, tenantDto.Documents);
                    }

                    // Map updated DTO
                    var updatedDto = await MapToDto(existingTenant);
                    updatedTenants.Add(updatedDto);
                }

                // Update AspNetUsers records for all updated tenants
                try
                {
                    var updatedUserIds = await UpdateTenantUsers(updatedTenants);

                    // Log successful user updates
                    foreach (var userUpdate in updatedUserIds)
                    {
                        Console.WriteLine($"Updated AspNetUser for tenant {userUpdate.Key} with ID: {userUpdate.Value}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire tenant update
                    Console.WriteLine($"Warning: Failed to update some AspNetUsers records: {ex.Message}");
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

        public async Task<Result<bool>> DeleteTenant(long id)
        {
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
                        return Result<bool>.Failure("HARD_DELETE_REQUIRED|Tenancy already started. Hard delete required to remove this tenant.");
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

        public async Task<Result<IEnumerable<TenantDto>>> GetEligibleTenantsForOnboarding(long landlordId, long propertyId)
        {
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

        public async Task<Result<int>> SendOnboardingEmails(long landlordId, long propertyId)
        {
            try
            {
                // Get all tenants for the specific property and landlord where:
                // - Age > 18 (calculated from DOB)
                // - Has email address
                // - Needs onboarding (hasn't been sent email yet)
                // - Is active
                var today = DateTime.Today;
                var cutoffDate = today.AddYears(-18); // Date 18 years ago

                // Get all tenants for the property with basic filters
                var allTenants = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .Where(t => t.LandlordId == landlordId
                                && t.PropertyId == propertyId // Specific property
                                && t.DOB.HasValue && t.DOB.Value <= cutoffDate // Age >= 18
                                && !string.IsNullOrEmpty(t.Email) // Has email
                                && t.IsActive.HasValue && t.IsActive.Value // Must be true
                                && (!t.OnboardingEmailSent.HasValue || !t.OnboardingEmailSent.Value)) // Not sent yet
                    .ToListAsync();

                // Filter to only include tenants whose group has accepted agreement
                var eligibleTenants = new List<Tenant>();
                foreach (var tenant in allTenants)
                {
                    // Check if primary tenant in this group has accepted agreement
                    var primaryTenantAccepted = await _context.Tenant
                        .AnyAsync(t => t.TenantGroup == tenant.TenantGroup
                                      && t.IsPrimary.HasValue && t.IsPrimary.Value
                                      && t.AgreementAccepted.HasValue && t.AgreementAccepted.Value);

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
                            emailsSent++;
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log individual email failures but continue with others
                        Console.WriteLine($"Failed to send email to tenant {tenant.Id}: {emailEx.Message}");
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

                // Get tenants by IDs where they have email and are active
                var tenants = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .Where(t => tenantIds.Contains(t.Id)
                                && !string.IsNullOrEmpty(t.Email) // Has email
                                && t.IsActive.HasValue && t.IsActive.Value) // Must be active
                    .ToListAsync();

                // Filter tenants to only those whose group has an accepted agreement
                var validTenants = new List<Tenant>();
                foreach (var tenant in tenants)
                {
                    // Check if primary tenant in this group has accepted agreement
                    var primaryTenantAccepted = await _context.Tenant
                        .AnyAsync(t => t.TenantGroup == tenant.TenantGroup
                                      && t.IsPrimary.HasValue && t.IsPrimary.Value
                                      && t.AgreementAccepted.HasValue && t.AgreementAccepted.Value);

                    if (primaryTenantAccepted)
                    {
                        validTenants.Add(tenant);
                    }
                }

                tenants = validTenants;

                if (!tenants.Any())
                    return Result<int>.Success(0);

                int emailsSent = 0;

                foreach (var tenant in tenants)
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
                            emailsSent++;
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log individual email failures but continue with others
                        Console.WriteLine($"Failed to send email to tenant {tenant.Id}: {emailEx.Message}");
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

        public async Task<Result<string>> CreateAgreement(AgreementCreateRequestDto request)
        {
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
                    Console.WriteLine($"Created AspNetUser for tenant email {userCreation.Key} with ID: {userCreation.Value}");
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

        public async Task<Result<bool>> AcceptAgreement(long tenantId)
        {
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

        public async Task<Result<bool>> UploadTenantDocument(long tenantId, IFormFile file, string category, string description)
        {
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
                            Category = Enum.Parse<DocumentCategory>(category),
                            Description = description
                        }
                    }
                };

                // Use your existing document service
                var uploadResult = await _documentService.UploadDocuments(documentUploadRequest);

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

        public async Task<Result<TenantStatisticsDto>> GetTenantStatistics(long landlordId)
        {
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



        public List<TenantValidationErrorDto> ValidateTenant(TenantDto tenant)
        {
            var errors = new List<TenantValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(tenant.Name) || tenant.Name.Length < 2)
                errors.Add(new TenantValidationErrorDto { Field = "name", Message = "Name must be at least 2 characters long" });

            if (string.IsNullOrWhiteSpace(tenant.Email))
                errors.Add(new TenantValidationErrorDto { Field = "email", Message = "Valid email address is required" });

            if (string.IsNullOrWhiteSpace(tenant.PhoneNumber))
                errors.Add(new TenantValidationErrorDto { Field = "phoneNumber", Message = "Valid phone number is required" });

            if (tenant.DOB == default)
                errors.Add(new TenantValidationErrorDto { Field = "dob", Message = "Date of birth is required" });

            if (string.IsNullOrWhiteSpace(tenant.Occupation) || tenant.Occupation.Length < 2)
                errors.Add(new TenantValidationErrorDto { Field = "occupation", Message = "Occupation is required" });

            //if (string.IsNullOrWhiteSpace(tenant.AadhaarNumber) || !IsValidAadhaar(tenant.AadhaarNumber))
            //    errors.Add(new TenantValidationErrorDto { Field = "aadhaarNumber", Message = "Valid 12-digit Aadhaar number is required" });

            //if (string.IsNullOrWhiteSpace(tenant.PanNumber) || !IsValidPAN(tenant.PanNumber))
            //    errors.Add(new TenantValidationErrorDto { Field = "PanNumber", Message = "Valid PAN number is required (e.g., ABCDE1234F)" });

            if (tenant.PropertyId <= 0)
                errors.Add(new TenantValidationErrorDto { Field = "propertyId", Message = "Property selection is required" });

            if (tenant.RentAmount <= 0)
                errors.Add(new TenantValidationErrorDto { Field = "rentAmount", Message = "Valid rent amount is required" });

            if (tenant.TenancyStartDate == default)
                errors.Add(new TenantValidationErrorDto { Field = "tenancyStartDate", Message = "Tenancy start date is required" });

            if (tenant.RentDueDate == default)
                errors.Add(new TenantValidationErrorDto { Field = "rentDueDate", Message = "Rent due date is required" });

            return errors;
        }

        public List<TenantValidationErrorDto> ValidateTenantGroup(List<TenantDto> tenants, bool isSingleTenant = false)
        {
            var errors = new List<TenantValidationErrorDto>();

            if (!tenants.Any())
            {
                errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "At least one tenant is required" });
                return errors;
            }

            // Check that exactly one tenant is marked as primary
            if (!isSingleTenant)
            {
                var primaryCount = tenants.Count(t => t.IsPrimary == true);
                if (primaryCount == 0)
                    errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "One tenant must be marked as primary" });
                else if (primaryCount > 1)
                    errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "Only one tenant can be marked as primary" });
            }
            // Validate each tenant
            for (int i = 0; i < tenants.Count; i++)
            {
                var tenantErrors = ValidateTenant(tenants[i]);
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
                errors.Add(new TenantValidationErrorDto { Field = "email", Message = $"Duplicate email address: {email}" });
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

        #region Agreement Email Methods

        private async Task SendAgreementEmail(Tenant tenant)
        {
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
                Console.WriteLine($"Failed to send agreement email to tenant {tenant.Id}: {ex.Message}");
            }
        }

        private string GenerateAgreementEmailBody(Tenant tenant)
        {
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
                <a href='#' class='button'>Login to Tenant Portal</a>
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
                Console.WriteLine($"Failed to send landlord notification for tenant {tenant.Id}: {ex.Message}");
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

        private async Task<TenantDto> MapToDto(Tenant tenant)
        {
            // Get documents for this tenant using your existing document service
            var documentsResult = await _documentService.GetDocumentsByOwner(tenant.Id, "Tenant");
            var documents = documentsResult.IsSuccess ? documentsResult.Entity : new List<DocumentDto>();


            return new TenantDto
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
                Documents = documents.Select(d => new DocumentDto
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
                Relationship = dto.Relationship ?? "Adult", // Default to Adult if not specified
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
                Relationship = dto.Relationship ?? "Adult", // Default to Adult if not specified
                IncludeInEmail = dto.IncludeInEmail ?? true // Default to true if not specified
            }).ToList();
        }

        private async Task SaveTenantDocuments(long tenantId, List<DocumentDto> documents)
        {
            if (!documents.Any()) return;

            var tenant = await _context.Tenant.FindAsync(tenantId);
            if (tenant == null) return;

            // Convert to your existing document upload structure
            var documentsWithFiles = documents.Where(d => d.File != null).ToList();

            if (documentsWithFiles.Any())
            {
                // Set additional tenant-specific information
                foreach (var doc in documentsWithFiles)
                {
                    doc.OwnerId = tenantId;
                    doc.OwnerType = "Tenant";
                    doc.TenantId = tenantId;
                    doc.LandlordId = tenant.LandlordId;
                    doc.PropertyId = tenant.PropertyId;
                    if (string.IsNullOrEmpty(doc.Description))
                        doc.Description = $"Tenant document - {doc.Category}";
                }

                var documentUploadRequest = new DocumentUploadRequestDto
                {
                    Documents = documentsWithFiles
                };

                // Use your existing document service
                await _documentService.UploadDocuments(documentUploadRequest);
            }
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
            var aadhaarRegex = new Regex(@"^[0-9]{12}$");
            return aadhaarRegex.IsMatch(aadhaar.Replace(" ", ""));
        }

        private bool IsValidPAN(string pan)
        {
            var panRegex = new Regex(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
            return panRegex.IsMatch(pan.ToUpper());
        }

        #region Onboarding Email Helper Methods

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
                Console.WriteLine($"Error creating attachments for tenant {tenant.Id}: {ex.Message}");
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
                        Console.WriteLine($"✅ Created AspNetUser for tenant: {tenant.Email} with Tenant role (ID: {userId})");
                    }
                    else if (userId == -1)
                    {
                        // User already exists - this is acceptable for tenant updates
                        Console.WriteLine($"ℹ️ AspNetUser already exists for tenant: {tenant.Email}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create AspNetUser for tenant: {tenant.Email}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Exception creating AspNetUser for tenant {tenant.Email}: {ex.Message}");
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
                        Name = "Tenant"
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
            return new string(Enumerable.Repeat(chars, 12)
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
                    Console.WriteLine($"Failed to update user for tenant {tenant.Email}: {ex.Message}");
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
    }
}
