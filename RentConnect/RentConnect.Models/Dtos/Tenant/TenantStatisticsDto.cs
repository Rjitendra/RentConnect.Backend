namespace RentConnect.Models.Dtos.Tenants
{
    public class TenantStatisticsDto
    {
        public int? Total { get; set; }
        public int? Active { get; set; }
        public int? Inactive { get; set; }
        public int? PendingOnboarding { get; set; }
        public decimal? TotalMonthlyRent { get; set; }
        public decimal? AverageRent { get; set; }
    }
}
