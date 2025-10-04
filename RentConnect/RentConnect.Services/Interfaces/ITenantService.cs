namespace RentConnect.Services.Interfaces
{
    using Microsoft.AspNetCore.Http;
    using RentConnect.Models.Dtos;
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
        Task<Result<bool>> HardDeleteTenant(long id);

        // Onboarding Operations
        Task<Result<IEnumerable<TenantDto>>> GetEligibleTenantsForOnboarding(long landlordId, long propertyId);
        Task<Result<int>> SendOnboardingEmails(long landlordId, long propertyId);
        Task<Result<int>> SendOnboardingEmailsByTenantIds(List<long> tenantIds);
        Task<Result<TenantDto>> GetTenantByEmail(string email);
        Task<Result<string>> CreateAgreement(AgreementCreateRequestDto request);
        Task<Result<bool>> AcceptAgreement(long tenantId);
        Task<Result<AgreementStatusDto>> GetAgreementStatus(long tenantId);


        // Document Management
        Task<Result<bool>> UploadTenantDocument(long tenantId, IFormFile file, string category, string description);

        // Statistics & Reports
        Task<Result<TenantStatisticsDto>> GetTenantStatistics(long landlordId);

        // Validation
        List<ValidationErrorDto> ValidateTenant(TenantDto tenant, bool create = false);
        List<ValidationErrorDto> ValidateTenantGroup(List<TenantDto> tenants, bool isSingleTenant,bool isCreate= false);
    }
}
