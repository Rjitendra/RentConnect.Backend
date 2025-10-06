using RentConnect.Models.Dtos.Chatbot;

namespace RentConnect.Services.Interfaces
{
    public interface IChatbotService
    {
        /// <summary>
        /// Process user message and generate AI response
        /// </summary>
        /// <param name="message">User message</param>
        /// <param name="context">Conversation context</param>
        /// <returns>AI-generated response</returns>
        Task<ChatbotResponseDto> ProcessMessageAsync(string message, ChatbotContextDto context);

        /// <summary>
        /// Analyze message intent
        /// </summary>
        /// <param name="message">User message</param>
        /// <param name="context">Conversation context</param>
        /// <returns>Detected intent and confidence</returns>
        Task<(string intent, double confidence)> AnalyzeIntentAsync(string message, ChatbotContextDto context);

        /// <summary>
        /// Generate contextual quick replies
        /// </summary>
        /// <param name="context">Conversation context</param>
        /// <param name="intent">Current intent</param>
        /// <returns>List of quick replies</returns>
        List<QuickReplyDto> GenerateQuickReplies(ChatbotContextDto context, string? intent = null);

        /// <summary>
        /// Generate contextual actions
        /// </summary>
        /// <param name="context">Conversation context</param>
        /// <param name="intent">Current intent</param>
        /// <returns>List of actions</returns>
        List<ChatActionDto> GenerateActions(ChatbotContextDto context, string? intent = null);

        /// <summary>
        /// Extract issue creation data from conversation
        /// </summary>
        /// <param name="message">User message</param>
        /// <param name="context">Conversation context</param>
        /// <returns>Issue creation data if applicable</returns>
        Task<IssueCreationDataDto?> ExtractIssueDataAsync(string message, ChatbotContextDto context);

        /// <summary>
        /// Get property information for tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Property information response</returns>
        Task<string> GetPropertyInformationAsync(long tenantId);

        /// <summary>
        /// Get payment information for tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Payment information response</returns>
        Task<string> GetPaymentInformationAsync(long tenantId);

        /// <summary>
        /// Get landlord insights and statistics
        /// </summary>
        /// <param name="landlordId">Landlord ID</param>
        /// <returns>Landlord insights response</returns>
        Task<string> GetLandlordInsightsAsync(long landlordId);

        /// <summary>
        /// Train the chatbot with new data
        /// </summary>
        /// <param name="trainingData">Training data</param>
        /// <returns>Training result</returns>
        Task<bool> TrainChatbotAsync(List<ChatbotTrainingDataDto> trainingData);

        /// <summary>
        /// Get chatbot analytics
        /// </summary>
        /// <param name="startDate">Start date for analytics</param>
        /// <param name="endDate">End date for analytics</param>
        /// <returns>Analytics data</returns>
        Task<ChatbotAnalyticsDto> GetAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Process AI chat with conversation history
        /// </summary>
        /// <param name="request">AI chat request with message and context</param>
        /// <returns>AI-generated response</returns>
        Task<AIChatResponseDto> ProcessAIChatAsync(AIChatRequestDto request);
    }
}
