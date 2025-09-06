using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RentConnect.Models.Context;
using RentConnect.Models.Dtos.Tickets;
using RentConnect.Models.Dtos.Properties;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.TicketTracking;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;

namespace RentConnect.Services.Implementations
{
    public class TicketService : ITicketService
    {
        private readonly ApiContext _context;
        private readonly IDocumentService _documentService;

        public TicketService(ApiContext context, IDocumentService documentService)
        {
            _context = context;
            _documentService = documentService;
        }

        public async Task<Result<List<TicketDto>>> GetLandlordTicketsAsync(long landlordId)
        {
            try
            {
                var tickets = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .Where(t => t.LandlordId == landlordId && !t.IsDeleted)
                    .OrderByDescending(t => t.DateCreated)
                    .ToListAsync();

                var ticketDtos = tickets.Select(MapToDto).ToList();
                return Result<List<TicketDto>>.Success(ticketDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TicketDto>>.Failure($"Error retrieving landlord tickets: {ex.Message}");
            }
        }

        public async Task<Result<List<TicketDto>>> GetTenantTicketsAsync(long tenantId)
        {
            try
            {
                var tickets = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                    .OrderByDescending(t => t.DateCreated)
                    .ToListAsync();

                var ticketDtos = tickets.Select(MapToDto).ToList();
                return Result<List<TicketDto>>.Success(ticketDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TicketDto>>.Failure($"Error retrieving tenant tickets: {ex.Message}");
            }
        }

        public async Task<Result<List<TicketDto>>> GetPropertyTicketsAsync(long propertyId)
        {
            try
            {
                var tickets = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .Where(t => t.PropertyId == propertyId && !t.IsDeleted)
                    .OrderByDescending(t => t.DateCreated)
                    .ToListAsync();

                var ticketDtos = tickets.Select(MapToDto).ToList();
                return Result<List<TicketDto>>.Success(ticketDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TicketDto>>.Failure($"Error retrieving property tickets: {ex.Message}");
            }
        }

        public async Task<Result<TicketDto>> GetTicketByIdAsync(long ticketId)
        {
            try
            {
                var ticket = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted);

                if (ticket == null)
                {
                    return Result<TicketDto>.NotFound("Ticket not found");
                }

                var ticketDto = MapToDto(ticket);
                return Result<TicketDto>.Success(ticketDto);
            }
            catch (Exception ex)
            {
                return Result<TicketDto>.Failure($"Error retrieving ticket: {ex.Message}");
            }
        }

        public async Task<TicketSaveResponseDto> CreateTicketAsync(TicketCreateRequestDto request)
        {
            try
            {
                var ticket = new Ticket
                {
                    TicketNumber = GenerateTicketNumber(),
                    LandlordId = request.LandlordId,
                    TenantGroupId = request.TenantGroupId,
                    PropertyId = request.PropertyId,
                    Category = request.Category,
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    CurrentStatus = TicketStatusType.Open,
                    CreatedBy = request.CreatedBy,
                    CreatedByType = request.CreatedByType,
                    AssignedTo = request.AssignedTo,
                    TenantId = request.TenantId,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };

                _context.Ticket.Add(ticket);
                await _context.SaveChangesAsync();

                // Add initial status
                var initialStatus = new TicketStatus
                {
                    TicketId = ticket.Id,
                    Status = TicketStatusType.Open,
                    Comment = "Ticket created",
                    AddedBy = request.CreatedBy,
                    AddedByName = await GetUserNameAsync(request.CreatedBy ?? 0),
                    AddedByType = request.CreatedByType,
                    DateCreated = DateTime.UtcNow
                };

                _context.TicketStatus.Add(initialStatus);

                // Handle attachments if any
                if (request.Attachments != null && request.Attachments.Any())
                {
                    // Create initial comment with attachments
                    var initialComment = new TicketComment
                    {
                        TicketId = ticket.Id,
                        Comment = "Initial ticket attachments",
                        AddedBy = request.CreatedBy,
                        AddedByName = await GetUserNameAsync(request.CreatedBy ?? 0),
                        AddedByType = request.CreatedByType,
                        DateCreated = DateTime.UtcNow
                    };

                    _context.TicketComment.Add(initialComment);
                    await _context.SaveChangesAsync();

                    // Save attachments using DocumentService
                    await SaveTicketAttachmentsAsync(initialComment.Id, request.Attachments, request.CreatedBy ?? 0, "ticket");
                }

                await _context.SaveChangesAsync();

                // Reload ticket with all related data
                var createdTicket = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == ticket.Id);

                return new TicketSaveResponseDto
                {
                    Success = true,
                    Message = "Ticket created successfully",
                    Entity = MapToDto(createdTicket!)
                };
            }
            catch (Exception ex)
            {
                return new TicketSaveResponseDto
                {
                    Success = false,
                    Message = "Error creating ticket",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<Result<TicketDto>> UpdateTicketStatusAsync(TicketUpdateStatusRequestDto request)
        {
            try
            {
                var ticket = await _context.Ticket
                    .Include(t => t.StatusHistory)
                    .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted);

                if (ticket == null)
                {
                    return Result<TicketDto>.NotFound("Ticket not found");
                }

                // Update ticket status
                ticket.CurrentStatus = request.Status;
                ticket.DateModified = DateTime.UtcNow;

                if (request.Status == TicketStatusType.Resolved || request.Status == TicketStatusType.Closed)
                {
                    ticket.DateResolved = DateTime.UtcNow;
                }

                // Add status history
                var statusHistory = new TicketStatus
                {
                    TicketId = request.TicketId,
                    Status = request.Status,
                    Comment = request.Comment,
                    AddedBy = request.UpdatedBy,
                    AddedByName = request.UpdatedByName,
                    AddedByType = request.UpdatedByType,
                    DateCreated = DateTime.UtcNow
                };

                _context.TicketStatus.Add(statusHistory);
                await _context.SaveChangesAsync();

                // Reload ticket with all related data
                var updatedTicket = await _context.Ticket
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.StatusHistory)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == request.TicketId);

                return Result<TicketDto>.Success(MapToDto(updatedTicket!));
            }
            catch (Exception ex)
            {
                return Result<TicketDto>.Failure($"Error updating ticket status: {ex.Message}");
            }
        }

        public async Task<Result<TicketCommentDto>> AddCommentAsync(TicketAddCommentRequestDto request)
        {
            try
            {
                var ticket = await _context.Ticket
                    .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted);

                if (ticket == null)
                {
                    return Result<TicketCommentDto>.NotFound("Ticket not found");
                }

                var comment = new TicketComment
                {
                    TicketId = request.TicketId,
                    Comment = request.Comment,
                    AddedBy = request.AddedBy,
                    AddedByName = request.AddedByName,
                    AddedByType = request.AddedByType,
                    DateCreated = DateTime.UtcNow
                };

                _context.TicketComment.Add(comment);
                await _context.SaveChangesAsync();

                // Handle attachments if any
                if (request.Attachments != null && request.Attachments.Any())
                {
                    // Save attachments using DocumentService
                    await SaveTicketAttachmentsAsync(comment.Id, request.Attachments, request.AddedBy ?? 0, "ticket");
                    await _context.SaveChangesAsync();
                }

                // Reload comment with attachments
                var savedComment = await _context.TicketComment
                    .Include(c => c.Attachments)
                    .FirstOrDefaultAsync(c => c.Id == comment.Id);

                return Result<TicketCommentDto>.Success(MapCommentToDto(savedComment!));
            }
            catch (Exception ex)
            {
                return Result<TicketCommentDto>.Failure($"Error adding comment: {ex.Message}");
            }
        }

        public async Task<Result<List<TicketCommentDto>>> GetTicketCommentsAsync(long ticketId)
        {
            try
            {
                var comments = await _context.TicketComment
                    .Include(c => c.Attachments)
                    .Where(c => c.TicketId == ticketId && !c.IsDeleted)
                    .OrderBy(c => c.DateCreated)
                    .ToListAsync();

                var commentDtos = comments.Select(MapCommentToDto).ToList();
                return Result<List<TicketCommentDto>>.Success(commentDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TicketCommentDto>>.Failure($"Error retrieving comments: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteTicketAsync(long ticketId)
        {
            try
            {
                var ticket = await _context.Ticket
                    .FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted);

                if (ticket == null)
                {
                    return Result<bool>.NotFound("Ticket not found");
                }

                ticket.IsDeleted = true;
                ticket.DateModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, "Ticket deleted successfully");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error deleting ticket: {ex.Message}");
            }
        }

        public async Task<Result<List<Models.Dtos.Document.DocumentDto>>> GetTicketAttachmentsAsync(long commentId)
        {
            try
            {
                var documents = await _context.Document
                    .Where(d => d.OwnerId == commentId && d.OwnerType == "TicketComment" && !d.IsDeleted)
                    .ToListAsync();

                var documentDtos = documents.Select(d => new Models.Dtos.Document.DocumentDto
                {
                    Id = d.Id,
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    Category = d.Category,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Url = d.Url,
                    DocumentIdentifier = d.DocumentIdentifier,
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified,
                    Description = d.Description,
                    DownloadUrl = d.Url
                }).ToList();

                return Result<List<Models.Dtos.Document.DocumentDto>>.Success(documentDtos);
            }
            catch (Exception ex)
            {
                return Result<List<Models.Dtos.Document.DocumentDto>>.Failure($"Error retrieving ticket attachments: {ex.Message}");
            }
        }

        public string GenerateTicketNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"TKT-{timestamp}-{random}";
        }

        #region Private Helper Methods

        private TicketDto MapToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                LandlordId = ticket.LandlordId,
                TenantGroupId = ticket.TenantGroupId,
                PropertyId = ticket.PropertyId,
                Category = ticket.Category,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                CurrentStatus = ticket.CurrentStatus,
                CreatedBy = ticket.CreatedBy,
                CreatedByType = ticket.CreatedByType,
                AssignedTo = ticket.AssignedTo,
                DateCreated = ticket.DateCreated,
                DateModified = ticket.DateModified,
                DateResolved = ticket.DateResolved,
                TenantId = ticket.TenantId,
                StatusHistory = ticket.StatusHistory?.Select(MapStatusToDto).ToList(),
                Tenant = ticket.Tenant != null ? MapTenantToDto(ticket.Tenant) : null,
                Property = ticket.Property != null ? MapPropertyToDto(ticket.Property) : null,
                Comments = ticket.Comments?.Select(MapCommentToDto).ToList()
            };
        }

        private TicketStatusDto MapStatusToDto(TicketStatus status)
        {
            return new TicketStatusDto
            {
                Id = status.Id,
                TicketId = status.TicketId,
                Status = status.Status,
                Comment = status.Comment,
                AddedBy = status.AddedBy,
                AddedByName = status.AddedByName,
                AddedByType = status.AddedByType,
                DateCreated = status.DateCreated
            };
        }

        private TicketCommentDto MapCommentToDto(TicketComment comment)
        {
            return new TicketCommentDto
            {
                Id = comment.Id,
                TicketId = comment.TicketId,
                Comment = comment.Comment,
                AddedBy = comment.AddedBy,
                AddedByName = comment.AddedByName,
                AddedByType = comment.AddedByType,
                DateCreated = comment.DateCreated,
                Attachments = comment.Attachments?.Select(MapAttachmentToDto).ToList()
            };
        }

        private TicketAttachmentDto MapAttachmentToDto(TicketAttachment attachment)
        {
            return new TicketAttachmentDto
            {
                Id = attachment.Id,
                CommentId = attachment.CommentId,
                FileName = attachment.FileName,
                FileUrl = attachment.FileUrl,
                FileSize = attachment.FileSize,
                FileType = attachment.FileType,
                UploadedBy = attachment.UploadedBy,
                DateUploaded = attachment.DateUploaded
            };
        }

        private TenantDto MapTenantToDto(Models.Entities.Tenants.Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name ?? string.Empty,
                Email = tenant.Email,
                PhoneNumber = tenant.PhoneNumber ?? string.Empty,
                LandlordId = tenant.LandlordId,
                PropertyId = tenant.PropertyId
                // Add other properties as needed
            };
        }

        private PropertyDto MapPropertyToDto(Models.Entities.Properties.Property property)
        {
            return new PropertyDto
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                City = property.City,
                State = property.State,
                PinCode = property.PinCode,
                LandlordId = property.LandlordId
                // Add other properties as needed
            };
        }

        private async Task<string> GetUserNameAsync(long userId)
        {
            // This is a simplified implementation
            // You might want to get the actual user name from ApplicationUser table
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.UserName ?? "Unknown User";
        }

        private async Task SaveTicketAttachmentsAsync(long commentId, List<IFormFile> attachments, long uploadedBy, string ownerType)
        {
            // Use DocumentService following PropertyController pattern
            var documentUploadRequest = new Models.Dtos.Document.DocumentUploadRequestDto
            {
                Documents = attachments.Select(file => new Models.Dtos.Document.DocumentDto
                {
                    File = file,
                    OwnerId = commentId, // Using commentId as ownerId for ticket attachments
                    OwnerType = "TicketComment", // Identifying this as a ticket comment attachment
                    Category = Models.Enums.DocumentCategory.Other, // Using Other category for ticket attachments
                    Description = "Ticket attachment",
                    LandlordId = null,
                    PropertyId = null,
                    TenantId = null,
                    Name = file.FileName,
                    Size = file.Length,
                    Type = file.ContentType,
                    UploadedOn = DateTime.UtcNow.ToString("o"),
                    IsVerified = true,
                    DocumentIdentifier = $"ticket_attachment_{commentId}_{Guid.NewGuid()}"
                }).ToList()
            };

            // Upload documents using DocumentService
            var documentResult = await _documentService.UploadDocuments(documentUploadRequest);

            // Also create TicketAttachment records for backward compatibility
            foreach (var file in attachments)
            {
                var fileUrl = await SaveFileToWwwrootAsync(file, ownerType, commentId);
                var attachment = new TicketAttachment
                {
                    CommentId = commentId,
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    UploadedBy = uploadedBy,
                    DateUploaded = DateTime.UtcNow
                };

                _context.TicketAttachment.Add(attachment);
            }
        }

        private async Task<string> SaveFileToWwwrootAsync(IFormFile file, string ownerType, long ownerId)
        {
            // Save files in wwwroot following the same pattern as DocumentService
            var uploadPath = Path.Combine("wwwroot/uploads", ownerType, ownerId.ToString());
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{ownerType}/{ownerId}/{fileName}";
        }

        #endregion
    }
}
