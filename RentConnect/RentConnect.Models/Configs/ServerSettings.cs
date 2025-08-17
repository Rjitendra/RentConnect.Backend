namespace RentConnect.Models.Configs
{
    public class ServerSettings
    {
        public string[] AllowedOrigins { get; set; }

        public string ApiName { get; set; }

        public string ApiSecret { get; set; }

        public string BaseUrl { get; set; }

        public string ClientUrl { get; set; }
    }
}