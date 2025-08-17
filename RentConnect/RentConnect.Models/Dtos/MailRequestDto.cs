namespace RentConnect.Models.Dtos
{
    public class MailRequestDto
    {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public IList<AttachmentsDto>? Attachments { get; set; }
    }

    public class AttachmentsDto
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public string ContentType { get; set; }
    }
}