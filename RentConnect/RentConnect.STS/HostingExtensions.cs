namespace RentConnect.STS
{
    using Duende.IdentityServer.Services;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models.Configs;
    using RentConnect.Models.Context;
    using RentConnect.Models.Entities;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Implementations;
    using RentConnect.Services.Interfaces;
    using RentConnect.STS.Config;

    public static class HostingExtensions
    {
        private const string CorsPolicy = "DefaultCorsPolicy";
        private const string ClientApplicationSettings = "ClientApplicationSettings";

        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllersWithViews();

            // Add DB Contexts
            builder.Services.AddDbContext<ApiContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("RentConnect.STS")));

            // configure reading the client application settings from appsettings.json file
            var clientSettings = builder.Configuration.GetSection(ClientApplicationSettings).Get<ClientApplicationSettings>();
            builder.Services.Configure<ClientApplicationSettings>(builder.Configuration.GetSection(ClientApplicationSettings));

            // Add ASP.NET Core Identity
            builder.Services.AddIdentity<ApplicationUser, ApplicationUserIdentityRole>(IdentityServerConfigurations.GetConfigureIdentityOptions())
                .AddEntityFrameworkStores<ApiContext>()
                .AddDefaultTokenProviders();

            var StripeConfiguration = builder.Configuration.GetSection("StripeConfiguration").Get<StripeSetting>();
            builder.Services.AddSingleton(StripeConfiguration);
            builder.Services.AddSingleton(clientSettings);

            // load mail settings from appsettings.json
            var mailSettings = builder.Configuration.GetSection("MailSettings").Get<MailSetting>();

            // Add mail service to DI
            builder.Services.AddTransient<IMailService, MailService>(x => new MailService(mailSettings));

            builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            var logFactory = (ILoggerFactory)new LoggerFactory();

            // We need to specify the IdentityServer CORS settings, which is separate from the Identity Server CORS
            var idsrvCors = new DefaultCorsPolicyService(logFactory.CreateLogger<DefaultCorsPolicyService>())
            {
                AllowedOrigins = { clientSettings.AngularBaseUrl.TrimEnd('/') }
            };
            builder.Services.AddSingleton<ICorsPolicyService>(idsrvCors);

            // configure IIS hosting
            builder.Services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            // General MVC project services
            builder.Services.AddRazorPages();

            // configure cookies
            builder.Services.ConfigureApplicationCookie(
                options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

            builder.Services.AddTransient<IProfileService, ProfileService>();

            // add IdentityServer6

            //var clientAppSettings = builder.Configuration.GetSection(ClientApplicationSettings).Get<ClientApplicationSettings>();

            builder.Services.AddIdentityServer(IdentityServerConfigurations.GetIdentityServerOptions())
                 .AddInMemoryApiResources(IdentityServerConfigurations.GetApiResources(clientSettings))
            .AddInMemoryApiScopes(IdentityServerConfigurations.ApiScopes(clientSettings))
            .AddInMemoryIdentityResources(IdentityServerConfigurations.GetIdentityResources())
                   .AddInMemoryClients(IdentityServerConfigurations.GetClients(clientSettings))
                 .AddSigningCredential(IdentityServerConfigurations.GetCertificate())
                // order matters very much here; AspNetIdentity has its own IProfileService implementation
                // by adding our own profile service after, we are overriding their implementation of IProfileService
                .AddAspNetIdentity<ApplicationUser>()
            .AddProfileService<ProfileService>();
            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax
            });
            // app.UseHttpsRedirection();
            app.UseCors(CorsPolicy);
            app.UseStaticFiles();
            app.UseRouting();
            app.UseIdentityServer();
            // call the InitializeRoles method here to initialize the roles during application startup
            InitializeRoles(app.Services).Wait();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            return app;
        }

        private static async Task InitializeRoles(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationUserIdentityRole>>();

            foreach (var roleName in Constants.RoleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationUserIdentityRole();
                    role.Name = roleName;
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }

    public static class Constants
    {
        public static string[] RoleNames = {
            ApplicationUserRole.None.ToString(),
            ApplicationUserRole.SuperAdmin.ToString(),
            ApplicationUserRole.Landlord.ToString(),
            ApplicationUserRole.Tenant.ToString()
        };
    }
}