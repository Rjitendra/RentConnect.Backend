using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentConnect.Models.Dtos.Chatbot;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;
using System.Text.Json;

namespace RentConnect.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatbotController : BaseController
    {
        private readonly IChatbotService _chatbotService;
        private readonly ITenantService _tenantService;
        private readonly IPropertyService _propertyService;
        private readonly ITicketService _ticketService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotService chatbotService,
            ITenantService tenantService,
            IPropertyService propertyService,
            ITicketService ticketService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _tenantService = tenantService;
            _propertyService = propertyService;
            _ticketService = ticketService;
            _logger = logger;
        }

        /// <summary>
        /// Process chatbot message and generate AI response
        /// </summary>
        /// <param name="request">Chatbot request with message and context</param>
        /// <returns>AI-generated response with actions and quick replies</returns>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessMessage([FromBody] ChatbotRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message cannot be empty");

                // Validate and enrich context
                var enrichedContext = await EnrichContext(request.Context);

                // Process message with AI service
                var response = await _chatbotService.ProcessMessageAsync(request.Message, enrichedContext);

                return Ok(Result<ChatbotResponseDto>.Success(response, "Message processed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot message: {Message}", request.Message);

                // Return fallback response
                var fallbackResponse = new ChatbotResponseDto
                {
                    Message = "I apologize, but I'm having trouble processing your request right now. Please try again or contact support if the issue persists.",
                    QuickReplies = GetFallbackQuickReplies(request.Context.UserType)
                };

                return Ok(Result<ChatbotResponseDto>.Success(fallbackResponse));
            }
        }

        /// <summary>
        /// Get contextual information for the chatbot
        /// </summary>
        /// <param name="userType">User type (tenant or landlord)</param>
        /// <param name="userId">User ID</param>
        /// <returns>Contextual information for better AI responses</returns>
        [HttpGet("context/{userType}/{userId}")]
        public async Task<IActionResult> GetContext(string userType, long userId)
        {
            try
            {
                var context = new ChatbotContextDto
                {
                    UserType = userType.ToLower(),
                    UserId = userId
                };

                if (userType.ToLower() == "tenant")
                {
                    var tenant = await _tenantService.GetTenantById(userId);
                    if (tenant.Status == ResultStatusType.Success && tenant.Entity != null)
                    {
                        context.TenantId = tenant.Entity.Id;
                        context.PropertyId = tenant.Entity.PropertyId;
                        context.TenantInfo = new TenantInfoDto
                        {
                            Name = tenant.Entity.Name ?? string.Empty,
                            Email = tenant.Entity.Email ?? string.Empty,
                            RentAmount = tenant.Entity.RentAmount,
                            RentDueDate = tenant.Entity.RentDueDate?.Day,
                            PropertyName = tenant.Entity.PropertyName,
                            AgreementAccepted = tenant.Entity.AgreementAccepted == true
                        };
                    }
                }
                else if (userType.ToLower() == "landlord")
                {
                    // Get landlord properties and tenant summary
                    var properties = await _propertyService.GetPropertyList(userId);
                    if (properties.Status == ResultStatusType.Success && properties.Entity != null)
                    {
                        context.LandlordId = userId;
                        context.PropertyCount = properties.Entity.Count();
                        context.LandlordInfo = new LandlordInfoDto
                        {
                            PropertyCount = properties.Entity.Count(),
                            Properties = properties.Entity.Select(p => new PropertySummaryDto
                            {
                                Id = p.Id ?? 0,
                                Title = p.Title ?? string.Empty,
                                Address = p.AddressLine1,
                                City = p.City,
                                TenantCount = p.Tenants?.Count ?? 0
                            }).ToList()
                        };
                    }
                }

                return Ok(Result<ChatbotContextDto>.Success(context));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatbot context for user {UserId}", userId);
                return BadRequest($"Failed to get context: {ex.Message}");
            }
        }

        /// <summary>
        /// Create issue from chatbot conversation
        /// </summary>
        /// <param name="request">Issue creation request from chatbot</param>
        /// <returns>Created issue details</returns>
        [HttpPost("create-issue")]
        public async Task<IActionResult> CreateIssueFromChat([FromBody] ChatbotIssueCreationDto request)
        {
            try
            {
                if (request.TenantId <= 0)
                    return BadRequest("Valid tenant ID is required");

                // Convert chatbot issue to ticket
                var ticketRequest = new RentConnect.Models.Dtos.Tickets.TicketCreateRequestDto
                {
                    Title = request.Title,
                    Description = request.Description,
                    Category = (TicketCategory?)MapCategoryFromString(request.Category),
                    Priority = (TicketPriority?)MapPriorityFromString(request.Priority),
                    TenantId = request.TenantId,
                    CreatedByType = CreatedByType.Tenant,
                    CreatedBy = request.TenantId
                };

                // Create the ticket
                var result = await _ticketService.CreateTicketAsync(ticketRequest);

                if (result.Success)
                {
                    return Ok(Result<object>.Success(new { TicketId = result.Entity?.Id, TicketNumber = result.Entity?.TicketNumber }, "Issue created successfully from chatbot"));
                }

                return BadRequest("Failed to create issue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue from chatbot for tenant {TenantId}", request.TenantId);
                return BadRequest($"Failed to create issue: {ex.Message}");
            }
        }

        /// <summary>
        /// Get quick insights for landlords
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <returns>Quick insights and statistics</returns>
        [HttpGet("insights/landlord/{landlordId}")]
        public async Task<IActionResult> GetLandlordInsights(long landlordId)
        {
            try
            {
                // Get property statistics
                var properties = await _propertyService.GetPropertyList(landlordId);
                var tenantStats = await _tenantService.GetTenantStatistics(landlordId);
                var maintenanceRequests = await _ticketService.GetLandlordTicketsAsync(landlordId);

                var insights = new LandlordInsightsDto
                {
                    TotalProperties = properties.Entity?.Count() ?? 0,
                    OccupiedProperties = properties.Entity?.Count(p => p.Tenants?.Count > 0) ?? 0,
                    TotalTenants = tenantStats.Entity?.Total ?? 0,
                    PendingRentPayments = 0, // Calculate from payment service
                    OpenMaintenanceRequests = maintenanceRequests.Entity?.Count(t => t.CurrentStatus != TicketStatusType.Closed) ?? 0,
                    MonthlyRentCollection = 0, // Calculate from payment service
                    RecentActivities = new List<string>
                    {
                        "New tenant moved in at Property A",
                        "Maintenance request completed at Property B",
                        "Rent payment received from Tenant C"
                    }
                };

                return Ok(Result<LandlordInsightsDto>.Success(insights));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting landlord insights for {LandlordId}", landlordId);
                return BadRequest($"Failed to get insights: {ex.Message}");
            }
        }

        /// <summary>
        /// Get tenant-specific information for chatbot context
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Tenant information and related data</returns>
        [HttpGet("tenant-info/{tenantId}")]
        public async Task<IActionResult> GetTenantInfo(long tenantId)
        {
            try
            {
                var tenant = await _tenantService.GetTenantById(tenantId);
                if (tenant.Status != ResultStatusType.Success || tenant.Entity == null)
                    return NotFound("Tenant not found");

                var tenantInfo = new TenantChatInfoDto
                {
                    Name = tenant.Entity.Name ?? string.Empty,
                    Email = tenant.Entity.Email ?? string.Empty,
                    PropertyName = tenant.Entity.PropertyName,
                    RentAmount = tenant.Entity.RentAmount,
                    RentDueDate = tenant.Entity.RentDueDate?.Day,
                    TenancyStartDate = tenant.Entity.TenancyStartDate,
                    TenancyEndDate = tenant.Entity.TenancyEndDate,
                    AgreementAccepted = tenant.Entity.AgreementAccepted == true,
                    SecurityDeposit = tenant.Entity.SecurityDeposit,
                    MaintenanceCharges = tenant.Entity.MaintenanceCharges,
                    IsPrimaryTenant = tenant.Entity.IsPrimary == true
                };

                return Ok(Result<TenantChatInfoDto>.Success(tenantInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant info for chatbot {TenantId}", tenantId);
                return BadRequest($"Failed to get tenant info: {ex.Message}");
            }
        }

        #region Private Methods

        private async Task<ChatbotContextDto> EnrichContext(ChatbotContextDto context)
        {
            try
            {
                if (context.UserType == "tenant" && context.TenantId.HasValue)
                {
                    var tenant = await _tenantService.GetTenantById(context.TenantId.Value);
                    if (tenant.Status == ResultStatusType.Success && tenant.Entity != null)
                    {
                        context.PropertyId = tenant.Entity.PropertyId;
                        context.TenantInfo = new TenantInfoDto
                        {
                            Name = tenant.Entity.Name ?? string.Empty,
                            Email = tenant.Entity.Email ?? string.Empty,
                            RentAmount = tenant.Entity.RentAmount,
                            RentDueDate = tenant.Entity.RentDueDate?.Day,
                            PropertyName = tenant.Entity.PropertyName,
                            AgreementAccepted = tenant.Entity.AgreementAccepted == true
                        };
                    }
                }
                else if (context.UserType == "landlord" && context.LandlordId.HasValue)
                {
                    var properties = await _propertyService.GetPropertyList(context.LandlordId.Value);
                    if (properties.Status == ResultStatusType.Success && properties.Entity != null)
                    {
                        context.PropertyCount = properties.Entity.Count();
                        context.LandlordInfo = new LandlordInfoDto
                        {
                            PropertyCount = properties.Entity.Count(),
                            Properties = properties.Entity.Select(p => new PropertySummaryDto
                            {
                                Id = p.Id ?? 0,
                                Title = p.Title ?? string.Empty,
                                Address = p.AddressLine1,
                                City = p.City,
                                TenantCount = p.Tenants?.Count ?? 0
                            }).ToList()
                        };
                    }
                }

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich chatbot context");
                return context;
            }
        }

        private List<QuickReplyDto> GetFallbackQuickReplies(string userType)
        {
            if (userType == "tenant")
            {
                return new List<QuickReplyDto>
                {
                    new QuickReplyDto { Id = "help", Text = "Help", Payload = "help" },
                    new QuickReplyDto { Id = "property", Text = "Property Info", Payload = "property_info" },
                    new QuickReplyDto { Id = "contact", Text = "Contact Support", Payload = "contact_support" }
                };
            }
            else
            {
                return new List<QuickReplyDto>
                {
                    new QuickReplyDto { Id = "help", Text = "Help", Payload = "help" },
                    new QuickReplyDto { Id = "properties", Text = "My Properties", Payload = "properties" },
                    new QuickReplyDto { Id = "tenants", Text = "Tenant Overview", Payload = "tenants" }
                };
            }
        }

        private int MapCategoryFromString(string category)
        {
            return category?.ToLower() switch
            {
                "plumbing" => 0,
                "electrical" => 1,
                "ac_heating" => 2,
                "general" => 3,
                "cleaning" => 4,
                "security" => 5,
                _ => 3 // Default to general
            };
        }

        private int MapPriorityFromString(string priority)
        {
            return priority?.ToLower() switch
            {
                "low" => 0,
                "medium" => 1,
                "high" => 2,
                "urgent" => 3,
                _ => 1 // Default to medium
            };
        }

        #endregion
    }
}
