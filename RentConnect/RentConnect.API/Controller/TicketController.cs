namespace RentConnect.API.Controller
{
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.Models.Dtos.Tickets;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;

    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : BaseController
    {
        private readonly ITicketService _ticketService;
        private readonly IDocumentService _documentService;

        public TicketController(ITicketService ticketService, IDocumentService documentService)
        {
            _ticketService = ticketService;
            _documentService = documentService;
        }

        /// <summary>
        /// Get all tickets for a landlord
        /// </summary>
        /// <param name="landlordId">The landlord ID</param>
        /// <returns>List of tickets for the landlord</returns>
        [HttpGet("landlord/{landlordId}")]
        public async Task<IActionResult> GetLandlordTickets(long landlordId)
        {
            var result = await _ticketService.GetLandlordTicketsAsync(landlordId);

            // Convert relative paths to full URLs following PropertyController pattern
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            if (result?.Entity != null)
            {
                foreach (var ticket in result.Entity)
                {
                    if (ticket.Comments != null)
                    {
                        foreach (var comment in ticket.Comments)
                        {
                            if (comment.Attachments != null)
                            {
                                foreach (var attachment in comment.Attachments)
                                {
                                    if (!string.IsNullOrEmpty(attachment.FileUrl))
                                    {
                                        attachment.FileUrl = $"{baseUrl}{attachment.FileUrl}";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return ProcessResult(result);
        }

        /// <summary>
        /// Get all tickets for a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>List of tickets for the tenant</returns>
        [HttpGet("tenant/{tenantId}")]
        public async Task<IActionResult> GetTenantTickets(long tenantId)
        {
            var result = await _ticketService.GetTenantTicketsAsync(tenantId);
            ConvertAttachmentUrlsToFullUrls(result?.Entity);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get tickets by property
        /// </summary>
        /// <param name="propertyId">The property ID</param>
        /// <returns>List of tickets for the property</returns>
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetPropertyTickets(long propertyId)
        {
            var result = await _ticketService.GetPropertyTicketsAsync(propertyId);
            ConvertAttachmentUrlsToFullUrls(result?.Entity);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get ticket by ID with full details including comments
        /// </summary>
        /// <param name="ticketId">The ticket ID</param>
        /// <returns>Ticket details with comments and attachments</returns>
        [HttpGet("{ticketId}")]
        public async Task<IActionResult> GetTicketById(long ticketId)
        {
            var result = await _ticketService.GetTicketByIdAsync(ticketId);
            if (result?.Entity != null)
            {
                ConvertAttachmentUrlsToFullUrls(new List<TicketDto> { result.Entity });
            }
            return ProcessResult(result);
        }

        /// <summary>
        /// Create a new ticket
        /// </summary>
        /// <param name="request">Ticket creation request with form data</param>
        /// <returns>Created ticket details</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTicket([FromForm] TicketCreateFormRequest request)
        {
            try
            {
                var createRequest = new TicketCreateRequestDto
                {
                    LandlordId = request.LandlordId,
                    TenantGroupId = request.TenantGroupId,
                    PropertyId = request.PropertyId,
                    Category = request.Category,
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    CreatedBy = request.CreatedBy,
                    CreatedByType = request.CreatedByType,
                    AssignedTo = request.AssignedTo,
                    TenantId = request.TenantId,
                    Attachments = request.Attachments?.ToList()
                };

                var result = await _ticketService.CreateTicketAsync(createRequest);

                // Convert URLs to full URLs following PropertyController pattern
                if (result.Success && result.Entity != null)
                {
                    ConvertAttachmentUrlsToFullUrls(new List<TicketDto> { result.Entity });
                }

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create ticket: {ex.Message}");
            }
        }

        /// <summary>
        /// Update ticket status
        /// </summary>
        /// <param name="ticketId">The ticket ID</param>
        /// <param name="request">Status update request</param>
        /// <returns>Updated ticket details</returns>
        [HttpPut("{ticketId}/status")]
        public async Task<IActionResult> UpdateTicketStatus(long ticketId, [FromBody] TicketUpdateStatusRequest request)
        {
            var updateRequest = new TicketUpdateStatusRequestDto
            {
                TicketId = ticketId,
                Status = request.Status,
                Comment = request.Comment,
                UpdatedBy = request.UpdatedBy,
                UpdatedByType = request.UpdatedByType,
                UpdatedByName = request.UpdatedByName
            };

            var result = await _ticketService.UpdateTicketStatusAsync(updateRequest);
            return ProcessResult(result);
        }

        /// <summary>
        /// Add comment to ticket
        /// </summary>
        /// <param name="ticketId">The ticket ID</param>
        /// <param name="request">Comment request with form data</param>
        /// <returns>Added comment details</returns>
        [HttpPost("{ticketId}/comment")]
        public async Task<IActionResult> AddComment(long ticketId, [FromForm] TicketAddCommentFormRequest request)
        {
            var commentRequest = new TicketAddCommentRequestDto
            {
                TicketId = ticketId,
                Comment = request.Comment,
                AddedBy = request.AddedBy,
                AddedByName = request.AddedByName,
                AddedByType = request.AddedByType,
                Attachments = request.Attachments?.ToList()
            };

            var result = await _ticketService.AddCommentAsync(commentRequest);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get ticket comments
        /// </summary>
        /// <param name="ticketId">The ticket ID</param>
        /// <returns>List of comments for the ticket</returns>
        [HttpGet("{ticketId}/comments")]
        public async Task<IActionResult> GetTicketComments(long ticketId)
        {
            var result = await _ticketService.GetTicketCommentsAsync(ticketId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Delete ticket (soft delete)
        /// </summary>
        /// <param name="ticketId">The ticket ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{ticketId}")]
        public async Task<IActionResult> DeleteTicket(long ticketId)
        {
            var result = await _ticketService.DeleteTicketAsync(ticketId);
            return ProcessResult(result);
        }

        /// <summary>
        /// Get ticket comment attachments from Document table
        /// </summary>
        /// <param name="commentId">The comment ID</param>
        /// <returns>List of document attachments</returns>
        [HttpGet("comment/{commentId}/attachments")]
        public async Task<IActionResult> GetTicketAttachments(long commentId)
        {
            try
            {
                var result = await _ticketService.GetTicketAttachmentsAsync(commentId);
                if (!result.IsSuccess)
                    return ProcessResult(result);

                // Convert relative paths to full URLs following PropertyController pattern
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var attachmentList = result.Entity.Select(doc =>
                {
                    doc.Url = $"{baseUrl}{doc.Url}"; // For Angular display
                    doc.DownloadUrl = null;          // Reset download link if needed
                    return doc;
                }).ToList();

                return Ok(new
                {
                    Status = result.Status,
                    Message = result.Message,
                    Success = true,
                    Entity = attachmentList
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get ticket attachments: {ex.Message}");
            }
        }

        /// <summary>
        /// Download a specific ticket attachment
        /// </summary>
        /// <param name="attachmentId">Attachment document ID</param>
        /// <returns>File download</returns>
        [HttpGet("attachment/{attachmentId}/download")]
        public async Task<IActionResult> DownloadTicketAttachment(long attachmentId)
        {
            try
            {
                // Download the specific attachment using DocumentService
                var attachmentResult = await _documentService.DownloadDocument(attachmentId);
                if (!attachmentResult.IsSuccess)
                    return ProcessResult(attachmentResult);

                var (fileBytes, fileName, contentType) = attachmentResult.Entity;
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to download ticket attachment: {ex.Message}");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Helper method to convert relative attachment URLs to full URLs
        /// Following the same pattern as PropertyController
        /// </summary>
        /// <param name="tickets">List of tickets to process</param>
        private void ConvertAttachmentUrlsToFullUrls(List<TicketDto>? tickets)
        {
            if (tickets == null) return;

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            foreach (var ticket in tickets)
            {
                if (ticket.Comments != null)
                {
                    foreach (var comment in ticket.Comments)
                    {
                        if (comment.Attachments != null)
                        {
                            foreach (var attachment in comment.Attachments)
                            {
                                if (!string.IsNullOrEmpty(attachment.FileUrl))
                                {
                                    attachment.FileUrl = $"{baseUrl}{attachment.FileUrl}";
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    // Form request models for handling multipart/form-data
    public class TicketCreateFormRequest
    {
        public long? LandlordId { get; set; }
        public string? TenantGroupId { get; set; }
        public long? PropertyId { get; set; }
        public TicketCategory? Category { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TicketPriority? Priority { get; set; }
        public long? CreatedBy { get; set; }
        public CreatedByType? CreatedByType { get; set; }
        public long? AssignedTo { get; set; }
        public long? TenantId { get; set; }
        public IFormFile[]? Attachments { get; set; }
    }

    public class TicketUpdateStatusRequest
    {
        public TicketStatusType? Status { get; set; }
        public string? Comment { get; set; }
        public long? UpdatedBy { get; set; }
        public CreatedByType? UpdatedByType { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class TicketAddCommentFormRequest
    {
        public string? Comment { get; set; }
        public long? AddedBy { get; set; }
        public string? AddedByName { get; set; }
        public CreatedByType? AddedByType { get; set; }
        public IFormFile[]? Attachments { get; set; }
    }
}
