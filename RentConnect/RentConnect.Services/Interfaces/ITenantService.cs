namespace RentConnect.Services.Interfaces
{
    using Microsoft.AspNetCore.Http;
    using RentConnect.Models.Dtos.Tenants;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Services.Utility;

    public interface ITenantService
    {
        // Core CRUD Operations
        Task<Result<IEnumerable<TenantDto>>> GetAllTenants();
        Task<Result<TenantDto>> GetTenantById(long id);
        Task<Result<IEnumerable<TenantDto>>> GetTenantsByProperty(long propertyId);
        Task<Result<IEnumerable<TenantDto>>> GetTenantsByLandlord(long landlordId);
        Task<Result<TenantSaveResponseDto>> CreateTenants(TenantCreateRequestDto request);
        Task<Result<TenantSaveResponseDto>> UpdateTenant(TenantCreateRequestDto tenantDto);
        Task<Result<bool>> DeleteTenant(long id);

        // Onboarding Operations
        Task<Result<IEnumerable<TenantDto>>> GetEligibleTenantsForOnboarding(long landlordId, long propertyId);
        Task<Result<int>> SendOnboardingEmails(long landlordId, long propertyId);
        Task<Result<string>> CreateAgreement(AgreementCreateRequestDto request);
        Task<Result<bool>> AcceptAgreement(long tenantId);
        Task<Result<AgreementStatusDto>> GetAgreementStatus(long tenantId);

        // Children/Family Management
        Task<Result<TenantChildren>> AddTenantChild(long tenantId, TenantChildren child);
        Task<Result<bool>> UpdateTenantChild(long tenantId, long childId, TenantChildren childData);
        Task<Result<bool>> DeleteTenantChild(long tenantId, long childId);

        // Document Management
        Task<Result<bool>> UploadTenantDocument(long tenantId, IFormFile file, string category, string description);

        // Statistics & Reports
        Task<Result<TenantStatisticsDto>> GetTenantStatistics(long landlordId);

        // Validation
        List<TenantValidationErrorDto> ValidateTenant(TenantDto tenant);
        List<TenantValidationErrorDto> ValidateTenantGroup(List<TenantDto> tenants, bool isSingleTenant);
    }
}
