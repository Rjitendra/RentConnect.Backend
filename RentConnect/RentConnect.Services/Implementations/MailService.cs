namespace RentConnect.Services.Implementations
{
    using MailKit.Net.Smtp;
    using MimeKit;
    using RentConnect.Models.Configs;
    using RentConnect.Models.Dtos;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;

    public class MailService : IMailService
    {
        private MailSetting _mailSettings { get; }

        public MailService(MailSetting mailSettings)
        {
            this._mailSettings = mailSettings;
        }

        public async Task<Result<long>> SendEmailAsync(MailRequestDto mailRequest)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("RentConnect Admin", _mailSettings.FromAddress));
                email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
                email.Subject = mailRequest.Subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = mailRequest.Body
                };

                if (mailRequest.Attachments != null && mailRequest.Attachments.Any())
                {
                    foreach (var file in mailRequest.Attachments)
                    {
                        builder.Attachments.Add(file.FileName, file.FileData, ContentType.Parse(file.ContentType));
                    }
                }

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_mailSettings.FromAddress, _mailSettings.Password); // App password
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return Result<long>.Success(1);
            }
            catch
            {
                return Result<long>.Failure(0);
            }
        }

        public async Task<Result<long>> SendMulipleEmails(IList<MailRequestDto> mailRequest)
        {
            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_mailSettings.FromAddress, _mailSettings.Password);

                foreach (var mr in mailRequest)
                {
                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress("RentConnect Admin", _mailSettings.FromAddress));
                    email.To.Add(MailboxAddress.Parse(mr.ToEmail));
                    email.Subject = mr.Subject;

                    var builder = new BodyBuilder
                    {
                        HtmlBody = mr.Body
                    };

                    if (mr.Attachments != null && mr.Attachments.Any())
                    {
                        foreach (var file in mr.Attachments)
                        {
                            builder.Attachments.Add(file.FileName, file.FileData, ContentType.Parse(file.ContentType));
                        }
                    }

                    email.Body = builder.ToMessageBody();
                    await smtp.SendAsync(email);
                }

                await smtp.DisconnectAsync(true);
                return Result<long>.Success(1);
            }
            catch
            {
                return Result<long>.Failure(0);
            }
        }
    }
}