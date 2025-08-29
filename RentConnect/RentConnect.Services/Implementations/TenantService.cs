using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RentConnect.Models.Context;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.Documents;
using RentConnect.Models.Entities.Tenants;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;
using System.Text.RegularExpressions;

namespace RentConnect.Services.Implementations
{
    public class TenantService : ITenantService
    {
        private readonly ApiContext _context;
        private readonly IDocumentService _documentService;
        private readonly IMailService _mailService;

        public TenantService(
            ApiContext context,
            IDocumentService documentService,
            IMailService mailService)
        {
            _context = context;
            _documentService = documentService;
            _mailService = mailService;
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
                    .Where(t => t.LandlordId == landlordId)
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
                if (!request.Tenants.Any(t => t.IsPrimary))
                {
                    request.Tenants.First().IsPrimary = true;
                }

                // Generate a unique tenant group ID
                var tenantGroupId = DateTime.UtcNow.Ticks;

                var createdTenants = new List<TenantDto>();

                foreach (var tenantDto in request.Tenants)
                {
                    // Map to entity
                    var tenant = MapToEntity(tenantDto, request);
                    tenant.TenantGroup = (int)tenantGroupId;
                    tenant.DateCreated = DateTime.UtcNow;
                    tenant.DateModified = DateTime.UtcNow;

                    // Age is calculated automatically in the DTO

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

        public async Task<Result<TenantDto>> UpdateTenant(TenantDto tenantDto)
        {
            try
            {
                var existingTenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Id == tenantDto.Id);

                if (existingTenant == null)
                    return Result<TenantDto>.NotFound();

                // Validate the updated tenant data
                var validationErrors = ValidateTenant(tenantDto);
                if (validationErrors.Any())
                {
                    return Result<TenantDto>.Failure($"Validation failed: {string.Join(", ", validationErrors.Select(e => e.Message))}");
                }

                // Update tenant properties
                UpdateEntityFromDto(existingTenant, tenantDto);
                existingTenant.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedDto = await MapToDto(existingTenant);
                return Result<TenantDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                return Result<TenantDto>.Failure($"Failed to update tenant: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteTenant(long id)
        {
            try
            {
                var tenant = await _context.Tenant.FindAsync(id);
                if (tenant == null)
                    return Result<bool>.NotFound();

                // Soft delete - mark as inactive instead of removing
                tenant.IsActive = false;
                tenant.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete tenant: {ex.Message}");
            }
        }

        #endregion

        #region Onboarding Operations

        public async Task<Result<IEnumerable<TenantDto>>> GetEligibleTenantsForOnboarding(long landlordId, long propertyId)
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

                var eligibleTenants = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .Where(t => t.LandlordId == landlordId
                                && t.PropertyId == propertyId  // Specific property
                                && t.DOB <= cutoffDate  // Age >= 18
                                && !string.IsNullOrEmpty(t.Email)  // Has email
                                && t.NeedsOnboarding.HasValue  // Needs onboarding
                                && t.IsActive)  // Is active
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

                var eligibleTenants = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .Where(t => t.LandlordId == landlordId
                                && t.PropertyId == propertyId  // Specific property
                                && t.DOB <= cutoffDate  // Age >= 18
                                && !string.IsNullOrEmpty(t.Email)  // Has email
                                && t.NeedsOnboarding.HasValue  // Needs onboarding
                                && t.IsActive)  // Is active
                    .ToListAsync();

                if (!eligibleTenants.Any())
                    return Result<int>.Success(0);

                int emailsSent = 0;

                foreach (var tenant in eligibleTenants)
                {
                    try
                    {
                        // TODO: Implement actual email sending logic using IMailService
                        // For now, we'll just mark them as sent

                        // Update tenant onboarding status
                        tenant.OnboardingEmailSent = true;
                        tenant.OnboardingEmailDate = DateTime.UtcNow;
                        tenant.NeedsOnboarding = false;
                        tenant.DateModified = DateTime.UtcNow;

                        emailsSent++;
                    }
                    catch (Exception emailEx)
                    {
                        // Log individual email failures but continue with others
                        // TODO: Add proper logging
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

        public async Task<Result<string>> CreateAgreement(AgreementCreateRequestDto request)
        {
            try
            {
                var tenant = await _context.Tenant
                    .Include(t => t.Property)
                    .Include(t => t.Landlord)
                    .FirstOrDefaultAsync(t => t.Id == request.TenantId);

                if (tenant == null)
                    return Result<string>.NotFound();

                // Update tenant agreement details
                tenant.AgreementSigned = true;
                tenant.AgreementDate = DateTime.UtcNow;
                tenant.TenancyStartDate = DateTime.Parse(request.StartDate);
                tenant.TenancyEndDate = DateTime.Parse(request.EndDate);
                tenant.RentAmount = request.RentAmount;
                tenant.SecurityDeposit = request.SecurityDeposit;
                tenant.DateModified = DateTime.UtcNow;

                // Generate agreement URL (placeholder)
                var agreementUrl = $"/documents/agreement_{tenant.Id}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                tenant.AgreementUrl = agreementUrl;

                await _context.SaveChangesAsync();

                return Result<string>.Success(agreementUrl);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to create agreement: {ex.Message}");
            }
        }

        #endregion

        #region Children/Family Management

        public async Task<Result<TenantChildren>> AddTenantChild(long tenantId, TenantChildren child)
        {
            try
            {
                var tenant = await _context.Tenant.FindAsync(tenantId);
                if (tenant == null)
                    return Result<TenantChildren>.NotFound();

                child.TenantGroupId = tenant.TenantGroup;

                _context.TenantChildren.Add(child);
                await _context.SaveChangesAsync();

                return Result<TenantChildren>.Success(child);
            }
            catch (Exception ex)
            {
                return Result<TenantChildren>.Failure($"Failed to add tenant child: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UpdateTenantChild(long tenantId, long childId, TenantChildren childData)
        {
            try
            {
                var tenant = await _context.Tenant.FindAsync(tenantId);
                if (tenant == null)
                    return Result<bool>.NotFound();

                var existingChild = await _context.TenantChildren
                    .FirstOrDefaultAsync(c => c.Id == childId && c.TenantGroupId == tenant.TenantGroup);

                if (existingChild == null)
                    return Result<bool>.NotFound();

                // Update child properties
                existingChild.Name = childData.Name;
                existingChild.Email = childData.Email;
                existingChild.DOB = childData.DOB;
                existingChild.Occupation = childData.Occupation;

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to update tenant child: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteTenantChild(long tenantId, long childId)
        {
            try
            {
                var tenant = await _context.Tenant.FindAsync(tenantId);
                if (tenant == null)
                    return Result<bool>.NotFound();

                var child = await _context.TenantChildren
                    .FirstOrDefaultAsync(c => c.Id == childId && c.TenantGroupId == tenant.TenantGroup);

                if (child == null)
                    return Result<bool>.NotFound();

                _context.TenantChildren.Remove(child);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete tenant child: {ex.Message}");
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

                var active = tenants.Where(t => t.IsActive).ToList();
                var inactive = tenants.Where(t => !t.IsActive).ToList();
                var pendingOnboarding = tenants.Where(t => t.NeedsOnboarding.HasValue).ToList();
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

            if (string.IsNullOrWhiteSpace(tenant.Email) || !IsValidEmail(tenant.Email))
                errors.Add(new TenantValidationErrorDto { Field = "email", Message = "Valid email address is required" });

            if (string.IsNullOrWhiteSpace(tenant.PhoneNumber) || !IsValidPhone(tenant.PhoneNumber))
                errors.Add(new TenantValidationErrorDto { Field = "phoneNumber", Message = "Valid phone number is required" });

            if (tenant.DOB == default)
                errors.Add(new TenantValidationErrorDto { Field = "dob", Message = "Date of birth is required" });

            if (string.IsNullOrWhiteSpace(tenant.Occupation) || tenant.Occupation.Length < 2)
                errors.Add(new TenantValidationErrorDto { Field = "occupation", Message = "Occupation is required" });

            if (string.IsNullOrWhiteSpace(tenant.AadhaarNumber) || !IsValidAadhaar(tenant.AadhaarNumber))
                errors.Add(new TenantValidationErrorDto { Field = "aadhaarNumber", Message = "Valid 12-digit Aadhaar number is required" });

            if (string.IsNullOrWhiteSpace(tenant.PANNumber) || !IsValidPAN(tenant.PANNumber))
                errors.Add(new TenantValidationErrorDto { Field = "panNumber", Message = "Valid PAN number is required (e.g., ABCDE1234F)" });

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

        public List<TenantValidationErrorDto> ValidateTenantGroup(List<TenantDto> tenants)
        {
            var errors = new List<TenantValidationErrorDto>();

            if (!tenants.Any())
            {
                errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "At least one tenant is required" });
                return errors;
            }

            // Check that exactly one tenant is marked as primary
            var primaryCount = tenants.Count(t => t.IsPrimary);
            if (primaryCount == 0)
                errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "One tenant must be marked as primary" });
            else if (primaryCount > 1)
                errors.Add(new TenantValidationErrorDto { Field = "tenants", Message = "Only one tenant can be marked as primary" });

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
            var emails = tenants.Where(t => !string.IsNullOrWhiteSpace(t.Email)).Select(t => t.Email.ToLower()).ToList();
            var duplicateEmails = emails.GroupBy(e => e).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var email in duplicateEmails)
            {
                errors.Add(new TenantValidationErrorDto { Field = "email", Message = $"Duplicate email address: {email}" });
            }

            // Check for duplicate phone numbers
            var phones = tenants.Where(t => !string.IsNullOrWhiteSpace(t.PhoneNumber)).Select(t => t.PhoneNumber).ToList();
            var duplicatePhones = phones.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var phone in duplicatePhones)
            {
                errors.Add(new TenantValidationErrorDto { Field = "phoneNumber", Message = $"Duplicate phone number: {phone}" });
            }

            return errors;
        }

        #endregion

        #region Private Helper Methods

        private async Task<TenantDto> MapToDto(Tenant tenant)
        {
            // Get documents for this tenant using your existing document service
            var documentsResult = await _documentService.GetDocumentsByOwner(tenant.Id, "Tenant");
            var documents = documentsResult.IsSuccess ? documentsResult.Entity : new List<DocumentDto>();

            // Get children for this tenant
            var children = await _context.TenantChildren
                .Where(c => c.TenantGroupId == tenant.TenantGroup)
                .ToListAsync();

            return new TenantDto
            {
                Id = tenant.Id,
                LandlordId = tenant.LandlordId,
                PropertyId = tenant.PropertyId,
                Name = tenant.Name,
                Email = tenant.Email,
                PhoneNumber = tenant.PhoneNumber,
                DOB = tenant.DOB,
                Occupation = tenant.Occupation,
                AadhaarNumber = tenant.AadhaarNumber,
                PANNumber = tenant.PANNumber,
                TenancyStartDate = tenant.TenancyStartDate,
                TenancyEndDate = tenant.TenancyEndDate,
                RentDueDate = tenant.RentDueDate,
                RentAmount = tenant.RentAmount,
                SecurityDeposit = tenant.SecurityDeposit,
                BackgroundCheckFileUrl = tenant.BackgroundCheckFileUrl,
                RentGuideFileUrl = tenant.RentGuideFileUrl,
                DepositReceiptUrl = tenant.DepositReceiptUrl,
                IsAcknowledge = tenant.IsAcknowledge,
                AcknowledgeDate = tenant.AcknowledgeDate,
                IsVerified = tenant.IsVerified,
                IsNewTenant = tenant.IsNewTenant,
                IsPrimary = tenant.IsPrimary,
                IsActive = tenant.IsActive,
                TenantGroup = tenant.TenantGroup,
                IpAddress = tenant.IpAddress,
                DateCreated = tenant.DateCreated,
                DateModified = tenant.DateModified,
                PropertyName = tenant.Property != null ? $"{tenant.Property.Title} - {tenant.Property.Locality}, {tenant.Property.City}" : "",
                Documents = documents.Select(d => new DocumentDto
                {
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    Category = d.Category,
                    Name = d.Name,
                    Url = d.Url,
                    Description = d.Description,
                    DocumentIdentifier = d.DocumentIdentifier
                }).ToList(),
                Children = children.Select(c => new TenantChildrenDto
                {
                    Id = c.Id,
                    TenantGroupId = c.TenantGroupId,
                    Name = c.Name,
                    Email = c.Email,
                    DOB = c.DOB,
                    Occupation = c.Occupation
                }).ToList()
            };
        }

        private Tenant MapToEntity(TenantDto dto, TenantCreateRequestDto request)
        {
            return new Tenant
            {
                LandlordId = request.LandlordId,
                PropertyId = request.PropertyId,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DOB = dto.DOB,
                Occupation = dto.Occupation,
                AadhaarNumber = dto.AadhaarNumber,
                PANNumber = dto.PANNumber,
                TenancyStartDate = request.TenancyStartDate,
                TenancyEndDate = request.TenancyEndDate,
                RentDueDate = request.RentDueDate,
                RentAmount = request.RentAmount,
                SecurityDeposit = request.SecurityDeposit,
                IsAcknowledge = false,
                IsVerified = false,
                IsNewTenant = true,
                IsPrimary = dto.IsPrimary,
                IsActive = true,
                NeedsOnboarding = true,
                OnboardingEmailSent = false,
                OnboardingCompleted = false,
                AgreementSigned = false
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
            entity.PANNumber = dto.PANNumber;
            entity.TenancyStartDate = dto.TenancyStartDate;
            entity.TenancyEndDate = dto.TenancyEndDate;
            entity.RentDueDate = dto.RentDueDate;
            entity.RentAmount = dto.RentAmount;
            entity.SecurityDeposit = dto.SecurityDeposit;
            entity.IsAcknowledge = dto.IsAcknowledge;
            entity.IsVerified = dto.IsVerified;
            entity.IsNewTenant = dto.IsNewTenant;
            entity.IsPrimary = dto.IsPrimary;
            entity.IsActive = dto.IsActive;
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

        #endregion
    }
}
