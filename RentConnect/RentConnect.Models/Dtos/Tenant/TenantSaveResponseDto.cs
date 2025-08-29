

namespace RentConnect.Models.Dtos.Tenants
{
    public class TenantSaveResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }
        public TenantDto? Tenant { get; set; }
        public List<TenantDto>? Tenants { get; set; }
    }
}
