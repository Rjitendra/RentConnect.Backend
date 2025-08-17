namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos;
    using RentConnect.Services.Utility;

    public interface IMailService
    {
        Task<Result<long>> SendEmailAsync(MailRequestDto mailRequest);

        Task<Result<long>> SendMulipleEmails(IList<MailRequestDto> mailRequest);
    }
}