namespace RentConnect.Models.Dtos
{
    public class AccountLinkResponse
    {
        public string Object { get; set; }
        public DateTime Created { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Url { get; set; }
    }
}