using RentConnect.API.Controller;
using RentConnect.Services.Implementations;
using RentConnect.Services.Interfaces;

namespace RentConnect.API.Extentions
{
    public static class ServiceExtensions
    {
        public static void ConfigureDIServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IPropertyService, PropertyService>();
            services.AddTransient<IDocumentService, DocumentService>();
            services.AddTransient<ITenantService, TenantService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ITicketService, TicketService>();
            //services.AddTransient<IPaymentService, PaymentService>();
            //services.AddTransient<ITaskService, TaskService>();
        }
    }
}