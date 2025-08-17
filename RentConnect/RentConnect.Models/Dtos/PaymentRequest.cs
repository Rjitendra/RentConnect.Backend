namespace RentConnect.Models.Dtos
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public int landlorId { get; set; }
        public int TenantGroup { get; set; }
    }
}