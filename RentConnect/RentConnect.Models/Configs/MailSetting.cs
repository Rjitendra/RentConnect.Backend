namespace RentConnect.Models.Configs
{
    public class MailSetting
    {
        // <summary>
        /// Recipients of emails when the app is hosted in a development environment.
        /// </summary>
        public string[] DebugRecipients { get; set; }

        /// <summary>
        /// The from address for each email sent via the MailService.
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        /// SendGrid API key.
        /// </summary>

        public string Password { get; set; }
    }
}