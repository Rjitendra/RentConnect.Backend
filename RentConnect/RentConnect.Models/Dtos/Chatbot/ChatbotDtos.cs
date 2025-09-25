using System.ComponentModel.DataAnnotations;

namespace RentConnect.Models.Dtos.Chatbot
{
    public class ChatbotRequestDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public ChatbotContextDto Context { get; set; } = new();
    }

    public class ChatbotResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public List<QuickReplyDto>? QuickReplies { get; set; }
        public List<ChatActionDto>? Actions { get; set; }
        public List<string>? FollowUpQuestions { get; set; }
        public IssueCreationDataDto? IssueCreation { get; set; }
        public string? Intent { get; set; }
        public double? Confidence { get; set; }
    }

    public class ChatbotContextDto
    {
        public string UserType { get; set; } = string.Empty; // "tenant" or "landlord"
        public long UserId { get; set; }
        public long? PropertyId { get; set; }
        public long? TenantId { get; set; }
        public long? LandlordId { get; set; }
        public string? CurrentTopic { get; set; }
        public int? PropertyCount { get; set; }
        public List<ChatMessageDto> ConversationHistory { get; set; } = new();

        // Context-specific information
        public TenantInfoDto? TenantInfo { get; set; }
        public LandlordInfoDto? LandlordInfo { get; set; }
    }

    public class ChatMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty; // "user" or "bot"
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "text";
    }

    public class QuickReplyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    public class ChatActionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class IssueCreationDataDto
    {
        public string SuggestedTitle { get; set; } = string.Empty;
        public string SuggestedDescription { get; set; } = string.Empty;
        public string SuggestedCategory { get; set; } = string.Empty;
        public string SuggestedPriority { get; set; } = string.Empty;
    }

    // Tenant-specific DTOs
    public class TenantInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public decimal? RentAmount { get; set; }
        public int? RentDueDate { get; set; }
        public bool AgreementAccepted { get; set; }
    }

    public class TenantChatInfoDto : TenantInfoDto
    {
        public DateTime? TenancyStartDate { get; set; }
        public DateTime? TenancyEndDate { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public decimal? MaintenanceCharges { get; set; }
        public bool IsPrimaryTenant { get; set; }
    }

    // Landlord-specific DTOs
    public class LandlordInfoDto
    {
        public int PropertyCount { get; set; }
        public List<PropertySummaryDto> Properties { get; set; } = new();
    }

    public class PropertySummaryDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public int TenantCount { get; set; }
    }

    public class LandlordInsightsDto
    {
        public int TotalProperties { get; set; }
        public int OccupiedProperties { get; set; }
        public int TotalTenants { get; set; }
        public int PendingRentPayments { get; set; }
        public int OpenMaintenanceRequests { get; set; }
        public decimal MonthlyRentCollection { get; set; }
        public List<string> RecentActivities { get; set; } = new();
    }

    // Issue creation from chatbot
    public class ChatbotIssueCreationDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Priority { get; set; } = string.Empty;

        [Required]
        public long TenantId { get; set; }

        public List<string>? Tags { get; set; }
        public string? Location { get; set; }
    }

    // AI Training Data
    public class ChatbotTrainingDataDto
    {
        public string Intent { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
        public List<string> Responses { get; set; } = new();
        public string UserType { get; set; } = string.Empty;
    }

    // Analytics
    public class ChatbotAnalyticsDto
    {
        public int TotalConversations { get; set; }
        public int SuccessfulResolutions { get; set; }
        public double AverageResponseTime { get; set; }
        public List<string> TopIntents { get; set; } = new();
        public List<string> CommonIssues { get; set; } = new();
        public int IssuesCreatedFromChat { get; set; }
    }
}
