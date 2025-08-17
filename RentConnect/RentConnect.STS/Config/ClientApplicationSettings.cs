namespace RentConnect.STS.Config
{
    public class ClientApplicationSettings
    {
        public string AngularBaseUrl { get; set; }

        /// <summary>
        /// Secret that Lordhood's API uses.
        /// </summary>
        public string ApiSecret { get; set; }

        public string BaseUrl { get; set; }
    }
}