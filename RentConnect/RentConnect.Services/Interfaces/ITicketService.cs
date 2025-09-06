namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos.Tickets;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Utility;

    public interface ITicketService
    {
        Task<Result<List<TicketDto>>> GetLandlordTicketsAsync(long landlordId);
        Task<Result<List<TicketDto>>> GetTenantTicketsAsync(long tenantId);
        Task<Result<List<TicketDto>>> GetPropertyTicketsAsync(long propertyId);
        Task<Result<TicketDto>> GetTicketByIdAsync(long ticketId);
        Task<TicketSaveResponseDto> CreateTicketAsync(TicketCreateRequestDto request);
        Task<Result<TicketDto>> UpdateTicketStatusAsync(TicketUpdateStatusRequestDto request);
        Task<Result<TicketCommentDto>> AddCommentAsync(TicketAddCommentRequestDto request);
        Task<Result<List<TicketCommentDto>>> GetTicketCommentsAsync(long ticketId);
        Task<Result<bool>> DeleteTicketAsync(long ticketId);
        Task<Result<List<Models.Dtos.Document.DocumentDto>>> GetTicketAttachmentsAsync(long commentId);
        string GenerateTicketNumber();
    }
}
