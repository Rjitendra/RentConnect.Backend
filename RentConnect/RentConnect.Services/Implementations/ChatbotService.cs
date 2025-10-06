using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RentConnect.Models.Dtos.Chatbot;
using RentConnect.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RentConnect.Services.Implementations
{
    public class ChatbotService : IChatbotService
    {
        private readonly ILogger<ChatbotService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly IPropertyService _propertyService;
        private readonly ITicketService _ticketService;
        private readonly HttpClient _httpClient;

        // OpenAI Configuration
        private readonly string? _openAiApiKey;
        private readonly string _openAiModel;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly bool _openAiEnabled;

        // Intent patterns for local processing
        private readonly Dictionary<string, List<string>> _intentPatterns = new()
        {
            ["greeting"] = new() { "hello", "hi", "hey", "good morning", "good evening", "start" },
            ["tenancy_info"] = new() { "rent", "tenancy", "lease", "agreement", "contract" },
            ["property_info"] = new() { "property", "address", "location", "house", "apartment" },
            ["payment_info"] = new() { "payment", "pay", "money", "due", "bill", "invoice" },
            ["issue_creation"] = new() { "issue", "problem", "repair", "fix", "maintenance", "broken" },
            ["help"] = new() { "help", "what can you do", "commands", "options" },
            ["goodbye"] = new() { "bye", "goodbye", "see you", "exit", "quit" }
        };

        public ChatbotService(
            ILogger<ChatbotService> logger,
            IConfiguration configuration,
            ITenantService tenantService,
            IPropertyService propertyService,
            ITicketService ticketService,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _tenantService = tenantService;
            _propertyService = propertyService;
            _ticketService = ticketService;
            _httpClient = httpClient;

            // Load OpenAI configuration
            _openAiApiKey = _configuration["OpenAI:ApiKey"];
            _openAiModel = _configuration["OpenAI:Model"] ?? "gpt-4";
            _maxTokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "500");
            _temperature = double.Parse(_configuration["OpenAI:Temperature"] ?? "0.7");
            _openAiEnabled = bool.Parse(_configuration["OpenAI:Enabled"] ?? "false");
        }

        public async Task<ChatbotResponseDto> ProcessMessageAsync(string message, ChatbotContextDto context)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(message))
                {
                    return new ChatbotResponseDto
                    {
                        Message = "Please type a message to continue.",
                        QuickReplies = GenerateQuickReplies(context, "help")
                    };
                }

                if (context == null)
                {
                    context = new ChatbotContextDto { UserType = "tenant", UserId = 0 };
                }

                // Analyze intent
                var (intent, confidence) = await AnalyzeIntentAsync(message, context);

                // Generate response based on intent
                var response = await GenerateResponseAsync(message, intent, context);

                // Ensure response is not null
                if (response == null)
                {
                    response = new ChatbotResponseDto
                    {
                        Message = "I'm not sure how to respond to that. Could you please rephrase your question?",
                        QuickReplies = GenerateQuickReplies(context, "help")
                    };
                }

                // Add contextual quick replies and actions
                response.QuickReplies = GenerateQuickReplies(context, intent);
                response.Actions = GenerateActions(context, intent);
                response.Intent = intent;
                response.Confidence = confidence;

                // Check for issue creation intent
                if (intent == "issue_creation" || intent == "maintenance_request")
                {
                    response.IssueCreation = await ExtractIssueDataAsync(message, context);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot message: {Message}. Error: {Error}", message, ex.Message);
                return new ChatbotResponseDto
                {
                    Message = "I apologize, but I'm having trouble right now. The chatbot service is working with local responses (OpenAI is disabled). Please try asking about your property, rent, or maintenance issues.",
                    QuickReplies = GenerateQuickReplies(context ?? new ChatbotContextDto { UserType = "tenant" }, "help")
                };
            }
        }

        public async Task<(string intent, double confidence)> AnalyzeIntentAsync(string message, ChatbotContextDto context)
        {
            var lowerMessage = message.ToLower();

            // Check for exact matches first
            foreach (var intentPattern in _intentPatterns)
            {
                foreach (var pattern in intentPattern.Value)
                {
                    if (lowerMessage.Contains(pattern))
                    {
                        return (intentPattern.Key, 0.9);
                    }
                }
            }

            // Use AI service for complex intent detection
            try
            {
                var aiResponse = await CallAIServiceForIntent(message, context);
                if (aiResponse.HasValue)
                {
                    return (aiResponse.Value.Intent, aiResponse.Value.Confidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI service failed for intent analysis, using fallback");
            }

            // Fallback to contextual analysis
            return AnalyzeContextualIntent(message, context);
        }

        private async Task<ChatbotResponseDto> GenerateResponseAsync(string message, string intent, ChatbotContextDto context)
        {
            try
            {
                switch (intent)
                {
                    case "greeting":
                        return GenerateGreetingResponse(context);

                    case "tenancy_info":
                        return await GenerateTenancyInfoResponse(context);

                    case "property_info":
                        return await GeneratePropertyInfoResponse(context);

                    case "payment_info":
                        return await GeneratePaymentInfoResponse(context);

                    case "issue_creation":
                        return GenerateIssueCreationResponse(context);

                    case "help":
                        return GenerateHelpResponse(context);

                    case "goodbye":
                        return GenerateGoodbyeResponse(context);

                    default:
                        var aiResponse = await GenerateAIResponse(message, context);
                        // If AI response is null, return fallback
                        return aiResponse ?? new ChatbotResponseDto
                        {
                            Message = "I'm here to help! You can ask me about your property, rent payments, maintenance issues, or general information.",
                            QuickReplies = GenerateQuickReplies(context, "help")
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response for intent: {Intent}", intent);
                return new ChatbotResponseDto
                {
                    Message = "I'm here to help! You can ask me about your property, rent payments, or maintenance issues.",
                    QuickReplies = GenerateQuickReplies(context, "help")
                };
            }
        }

        private ChatbotResponseDto GenerateGreetingResponse(ChatbotContextDto context)
        {
            var userTypeText = context.UserType == "tenant" ? "tenant" : "landlord";
            var name = context.UserType == "tenant"
                ? context.TenantInfo?.Name
                : "there";

            return new ChatbotResponseDto
            {
                Message = $"Hello {name}! 👋 I'm your AI assistant for {userTypeText} services. How can I help you today?",
                FollowUpQuestions = new List<string>
                {
                    $"What would you like to know about your {(context.UserType == "tenant" ? "tenancy" : "properties")}?",
                    "Is there anything specific I can help you with?"
                }
            };
        }

        private async Task<ChatbotResponseDto> GenerateTenancyInfoResponse(ChatbotContextDto context)
        {
            if (context.UserType != "tenant" || !context.TenantId.HasValue)
            {
                return new ChatbotResponseDto
                {
                    Message = "I can only provide tenancy information to tenants. Please make sure you're logged in as a tenant."
                };
            }

            var tenantInfo = await GetPropertyInformationAsync(context.TenantId.Value);

            return new ChatbotResponseDto
            {
                Message = tenantInfo,
                FollowUpQuestions = new List<string>
                {
                    "Would you like to know about your payment schedule?",
                    "Do you need help with anything else regarding your tenancy?"
                }
            };
        }

        private async Task<ChatbotResponseDto> GeneratePropertyInfoResponse(ChatbotContextDto context)
        {
            if (context.UserType == "tenant" && context.TenantId.HasValue)
            {
                var propertyInfo = await GetPropertyInformationAsync(context.TenantId.Value);
                return new ChatbotResponseDto
                {
                    Message = propertyInfo
                };
            }
            else if (context.UserType == "landlord" && context.LandlordId.HasValue)
            {
                var insights = await GetLandlordInsightsAsync(context.LandlordId.Value);
                return new ChatbotResponseDto
                {
                    Message = insights
                };
            }

            return new ChatbotResponseDto
            {
                Message = "I need more information to help you with property details. Please make sure you're logged in correctly."
            };
        }

        private async Task<ChatbotResponseDto> GeneratePaymentInfoResponse(ChatbotContextDto context)
        {
            if (context.UserType != "tenant" || !context.TenantId.HasValue)
            {
                return new ChatbotResponseDto
                {
                    Message = "I can only provide payment information to tenants."
                };
            }

            var paymentInfo = await GetPaymentInformationAsync(context.TenantId.Value);

            return new ChatbotResponseDto
            {
                Message = paymentInfo
            };
        }

        private ChatbotResponseDto GenerateIssueCreationResponse(ChatbotContextDto context)
        {
            if (context.UserType != "tenant")
            {
                return new ChatbotResponseDto
                {
                    Message = "Only tenants can create maintenance issues. If you're a landlord, you can view existing issues."
                };
            }

            return new ChatbotResponseDto
            {
                Message = "I'd be happy to help you create a maintenance issue! 🔧\n\nWhat type of problem are you experiencing?",
                FollowUpQuestions = new List<string>
                {
                    "Can you describe the issue in detail?",
                    "Where in the property is the problem located?",
                    "How urgent is this issue?"
                }
            };
        }

        private ChatbotResponseDto GenerateHelpResponse(ChatbotContextDto context)
        {
            var capabilities = context.UserType == "tenant" ? new List<string>
            {
                "📋 **Tenancy Information** - Get details about your rent, lease terms, and agreement",
                "🏠 **Property Details** - View your property address and information",
                "💳 **Payment Information** - Check payment history and upcoming due dates",
                "🔧 **Maintenance Issues** - Create and track repair requests",
                "📄 **Documents** - Access your tenancy agreement and other documents",
                "📞 **Contact Information** - Get landlord and support contact details"
            } : new List<string>
            {
                "🏘️ **Property Portfolio** - Overview of all your properties",
                "👥 **Tenant Management** - View tenant information and status",
                "💰 **Rent Collection** - Track payments and outstanding amounts",
                "🔧 **Maintenance Requests** - Monitor and manage repair requests",
                "📊 **Insights & Reports** - Property performance and analytics",
                "📋 **Property Management** - Add, edit, and manage properties"
            };

            return new ChatbotResponseDto
            {
                Message = $"Here's what I can help you with:\n\n{string.Join("\n", capabilities)}\n\n💬 Just ask me anything in natural language, and I'll do my best to help!",
                FollowUpQuestions = new List<string>
                {
                    "What would you like to know more about?",
                    "Is there a specific task I can help you with?"
                }
            };
        }

        private ChatbotResponseDto GenerateGoodbyeResponse(ChatbotContextDto context)
        {
            return new ChatbotResponseDto
            {
                Message = "Thank you for using the AI assistant! 👋 Feel free to come back anytime if you need help. Have a great day!"
            };
        }

        private async Task<ChatbotResponseDto> GenerateAIResponse(string message, ChatbotContextDto context)
        {
            try
            {
                // Call external AI service (OpenAI, Azure AI, etc.)
                var aiResponse = await CallAIService(message, context);
                if (aiResponse != null)
                {
                    return aiResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI service call failed, using fallback response");
            }

            // Fallback response
            return new ChatbotResponseDto
            {
                Message = "I understand you're asking about something specific, but I need a bit more context to help you better. Could you please rephrase your question or choose from the quick options below?",
                FollowUpQuestions = new List<string>
                {
                    "Could you be more specific about what you need?",
                    "Which area would you like help with?"
                }
            };
        }

        public List<QuickReplyDto> GenerateQuickReplies(ChatbotContextDto context, string? intent = null)
        {
            if (context.UserType == "tenant")
            {
                return intent switch
                {
                    "issue_creation" => new List<QuickReplyDto>
                    {
                        new() { Id = "plumbing", Text = "🚰 Plumbing", Payload = "create_issue_plumbing" },
                        new() { Id = "electrical", Text = "⚡ Electrical", Payload = "create_issue_electrical" },
                        new() { Id = "ac_heating", Text = "🌡️ AC/Heating", Payload = "create_issue_ac" },
                        new() { Id = "general", Text = "🔧 General", Payload = "create_issue_general" }
                    },
                    _ => new List<QuickReplyDto>
                    {
                        new() { Id = "property_info", Text = "🏠 Property Info", Payload = "property_info" },
                        new() { Id = "payment_info", Text = "💳 Payments", Payload = "payment_info" },
                        new() { Id = "create_issue", Text = "🔧 Report Issue", Payload = "create_issue" },
                        new() { Id = "help", Text = "❓ Help", Payload = "help" }
                    }
                };
            }
            else
            {
                return new List<QuickReplyDto>
                {
                    new() { Id = "properties", Text = "🏘️ Properties", Payload = "properties" },
                    new() { Id = "tenants", Text = "👥 Tenants", Payload = "tenants" },
                    new() { Id = "maintenance", Text = "🔧 Maintenance", Payload = "maintenance" },
                    new() { Id = "reports", Text = "📊 Reports", Payload = "reports" }
                };
            }
        }

        public List<ChatActionDto> GenerateActions(ChatbotContextDto context, string? intent = null)
        {
            if (context.UserType == "tenant")
            {
                return new List<ChatActionDto>
                {
                    new() { Id = "view_property", Text = "View Property Details", Action = "view_property" },
                    new() { Id = "view_payments", Text = "View Payment History", Action = "view_payments" },
                    new() { Id = "download_agreement", Text = "Download Agreement", Action = "download_agreement" }
                };
            }
            else
            {
                return new List<ChatActionDto>
                {
                    new() { Id = "property_dashboard", Text = "Property Dashboard", Action = "view_dashboard" },
                    new() { Id = "tenant_overview", Text = "Tenant Overview", Action = "view_tenants" },
                    new() { Id = "maintenance_requests", Text = "Maintenance Requests", Action = "view_maintenance" }
                };
            }
        }

        public async Task<IssueCreationDataDto?> ExtractIssueDataAsync(string message, ChatbotContextDto context)
        {
            // Simple keyword-based extraction (can be enhanced with NLP)
            var lowerMessage = message.ToLower();

            string category = ExtractCategory(lowerMessage);
            string priority = ExtractPriority(lowerMessage);
            string title = ExtractTitle(message);

            return new IssueCreationDataDto
            {
                SuggestedTitle = title,
                SuggestedDescription = message,
                SuggestedCategory = category,
                SuggestedPriority = priority
            };
        }

        public async Task<string> GetPropertyInformationAsync(long tenantId)
        {
            try
            {
                var tenant = await _tenantService.GetTenantById(tenantId);
                if (tenant.Status == Models.Enums.ResultStatusType.Success && tenant.Entity != null)
                {
                    var info = new StringBuilder();
                    info.AppendLine($"🏠 **Property Information**\n");
                    info.AppendLine($"📍 **Property**: {tenant.Entity.PropertyName ?? "N/A"}");
                    info.AppendLine($"💰 **Monthly Rent**: ₹{tenant.Entity.RentAmount:N0}");
                    info.AppendLine($"📅 **Rent Due Date**: {tenant.Entity.RentDueDate?.ToString() ?? "1"} of each month");
                    info.AppendLine($"🏁 **Tenancy Start**: {tenant.Entity.TenancyStartDate:MMM dd, yyyy}");

                    if (tenant.Entity.TenancyEndDate.HasValue)
                    {
                        info.AppendLine($"🏁 **Tenancy End**: {tenant.Entity.TenancyEndDate:MMM dd, yyyy}");
                    }

                    info.AppendLine($"✅ **Agreement Status**: {(tenant.Entity.AgreementAccepted == true ? "Accepted" : "Pending")}");

                    return info.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property information for tenant {TenantId}", tenantId);
            }

            return "I couldn't retrieve your property information at the moment. Please try again later.";
        }

        public async Task<string> GetPaymentInformationAsync(long tenantId)
        {
            try
            {
                var tenant = await _tenantService.GetTenantById(tenantId);
                if (tenant.Status == Models.Enums.ResultStatusType.Success && tenant.Entity != null)
                {
                    var info = new StringBuilder();
                    info.AppendLine($"💳 **Payment Information**\n");
                    info.AppendLine($"💰 **Monthly Rent**: ₹{tenant.Entity.RentAmount:N0}");
                    info.AppendLine($"📅 **Due Date**: {tenant.Entity.RentDueDate?.ToString() ?? "1"} of each month");

                    if (tenant.Entity.SecurityDeposit.HasValue)
                    {
                        info.AppendLine($"🛡️ **Security Deposit**: ₹{tenant.Entity.SecurityDeposit:N0}");
                    }

                    if (tenant.Entity.MaintenanceCharges.HasValue)
                    {
                        info.AppendLine($"🔧 **Maintenance Charges**: ₹{tenant.Entity.MaintenanceCharges:N0}");
                    }

                    // TODO: Add actual payment history when payment service is implemented
                    info.AppendLine($"\n📊 For detailed payment history, please visit the Payments section.");

                    return info.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment information for tenant {TenantId}", tenantId);
            }

            return "I couldn't retrieve your payment information at the moment. Please try again later.";
        }

        public async Task<string> GetLandlordInsightsAsync(long landlordId)
        {
            try
            {
                var properties = await _propertyService.GetPropertyList(landlordId);
                var tenantStats = await _tenantService.GetTenantStatistics(landlordId);

                var info = new StringBuilder();
                info.AppendLine($"📊 **Landlord Dashboard**\n");
                info.AppendLine($"🏘️ **Total Properties**: {properties.Entity?.Count() ?? 0}");
                info.AppendLine($"👥 **Total Tenants**: {tenantStats.Entity?.Total ?? 0}");
                info.AppendLine($"🏠 **Occupied Properties**: {properties.Entity?.Count(p => p.Tenants?.Count > 0) ?? 0}");

                // TODO: Add more insights when payment and maintenance services are integrated
                info.AppendLine($"\n📈 For detailed analytics, please visit the Dashboard section.");

                return info.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting landlord insights for {LandlordId}", landlordId);
            }

            return "I couldn't retrieve your insights at the moment. Please try again later.";
        }

        // Placeholder methods for AI service integration
        public async Task<bool> TrainChatbotAsync(List<ChatbotTrainingDataDto> trainingData)
        {
            // TODO: Implement AI model training
            await Task.Delay(100);
            return true;
        }

        public async Task<ChatbotAnalyticsDto> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            // TODO: Implement analytics collection
            await Task.Delay(100);
            return new ChatbotAnalyticsDto();
        }

        #region Private Helper Methods

        private (string intent, double confidence) AnalyzeContextualIntent(string message, ChatbotContextDto context)
        {
            // Contextual analysis based on user type and conversation history
            if (context.UserType == "tenant")
            {
                if (message.ToLower().Contains("rent") || message.ToLower().Contains("payment"))
                    return ("payment_info", 0.7);
                if (message.ToLower().Contains("property") || message.ToLower().Contains("address"))
                    return ("property_info", 0.7);
            }

            return ("help", 0.5);
        }

        private async Task<(string Intent, double Confidence)?> CallAIServiceForIntent(string message, ChatbotContextDto context)
        {
            if (!_openAiEnabled || string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                _logger.LogDebug("OpenAI is disabled or API key is not configured");
                return null;
            }

            try
            {
                var systemPrompt = "You are an intent classifier for a property rental management system. Classify the user's intent into one of these categories: greeting, tenancy_info, property_info, payment_info, issue_creation, help, goodbye. Respond with only the intent name and a confidence score (0-1) in this format: intent|confidence";

                var response = await CallOpenAI(systemPrompt, message, 50);

                if (!string.IsNullOrEmpty(response))
                {
                    var parts = response.Split('|');
                    if (parts.Length == 2 && double.TryParse(parts[1], out var confidence))
                    {
                        return (parts[0].Trim(), confidence);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call OpenAI for intent detection");
            }

            return null;
        }

        private async Task<ChatbotResponseDto?> CallAIService(string message, ChatbotContextDto context)
        {
            if (!_openAiEnabled || string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                _logger.LogDebug("OpenAI is disabled or API key is not configured");
                return null;
            }

            try
            {
                var systemPrompt = BuildContextualSystemPrompt(context);
                var userContext = BuildUserContext(context);
                var fullPrompt = $"{userContext}\n\nUser message: {message}";

                var response = await CallOpenAI(systemPrompt, fullPrompt, _maxTokens);

                if (!string.IsNullOrEmpty(response))
                {
                    return new ChatbotResponseDto
                    {
                        Message = response,
                        QuickReplies = GenerateQuickReplies(context),
                        Actions = GenerateActions(context)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call OpenAI for response generation");
            }

            return null;
        }

        private async Task<string?> CallOpenAI(string systemPrompt, string userMessage, int maxTokens)
        {
            try
            {
                var request = new
                {
                    model = _openAiModel,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    max_tokens = maxTokens,
                    temperature = _temperature
                };

                var requestJson = JsonSerializer.Serialize(request);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseJson);

                    if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var messageObj) &&
                            messageObj.TryGetProperty("content", out var contentProp))
                        {
                            return contentProp.GetString();
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OpenAI API call failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
            }

            return null;
        }

        private string BuildContextualSystemPrompt(ChatbotContextDto context)
        {
            var basePrompt = "You are a helpful AI assistant for a property rental management system called RentConnect.";

            if (context.UserType == "tenant")
            {
                basePrompt += " You are assisting a tenant with their tenancy-related queries. You can help with information about their property, rent payments, maintenance issues, and tenancy agreement. Be friendly, concise, and helpful. If asked to create a maintenance issue, acknowledge it but explain they can use the quick actions to do so.";

                if (context.TenantInfo != null)
                {
                    basePrompt += $" The tenant's name is {context.TenantInfo.Name}. Their monthly rent is ₹{context.TenantInfo.RentAmount:N0} and rent is due on day {context.TenantInfo.RentDueDate} of each month. Property: {context.TenantInfo.PropertyName}.";
                }
            }
            else
            {
                basePrompt += " You are assisting a landlord with their property management queries. You can help with information about their properties, tenants, rent collection, and maintenance requests. Be professional, concise, and provide actionable insights.";

                if (context.LandlordInfo != null)
                {
                    basePrompt += $" The landlord manages {context.LandlordInfo.PropertyCount} properties.";
                }
            }

            basePrompt += " Keep responses concise (under 200 words) and friendly. Use emojis sparingly for visual appeal.";

            return basePrompt;
        }

        private string BuildUserContext(ChatbotContextDto context)
        {
            var contextInfo = $"User Type: {context.UserType}\n";

            if (context.ConversationHistory != null && context.ConversationHistory.Any())
            {
                contextInfo += "Recent conversation:\n";
                var recentMessages = context.ConversationHistory.TakeLast(5);
                foreach (var msg in recentMessages)
                {
                    contextInfo += $"- {msg.Content}\n";
                }
            }

            return contextInfo;
        }

        private string ExtractCategory(string message)
        {
            if (message.Contains("plumb") || message.Contains("water") || message.Contains("leak"))
                return "plumbing";
            if (message.Contains("electric") || message.Contains("power") || message.Contains("light"))
                return "electrical";
            if (message.Contains("ac") || message.Contains("heat") || message.Contains("temperature"))
                return "ac_heating";
            if (message.Contains("clean"))
                return "cleaning";
            if (message.Contains("security") || message.Contains("lock") || message.Contains("key"))
                return "security";

            return "general";
        }

        private string ExtractPriority(string message)
        {
            if (message.Contains("urgent") || message.Contains("emergency") || message.Contains("immediately"))
                return "urgent";
            if (message.Contains("important") || message.Contains("soon") || message.Contains("asap"))
                return "high";
            if (message.Contains("whenever") || message.Contains("not urgent") || message.Contains("low"))
                return "low";

            return "medium";
        }

        private string ExtractTitle(string message)
        {
            // Simple title extraction - take first 50 characters
            var title = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
            return char.ToUpper(title[0]) + title.Substring(1);
        }

        #endregion
    }
}
