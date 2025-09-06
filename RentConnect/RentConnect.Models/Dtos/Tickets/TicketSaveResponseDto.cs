namespace RentConnect.Models.Dtos.Tickets
{
    public class TicketSaveResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public TicketDto? Entity { get; set; }
        public List<string>? Errors { get; set; }
    }
}
