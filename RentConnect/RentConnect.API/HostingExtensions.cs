namespace RentConnect.API
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Versioning;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json;
    using RentConnect.API.Configs;
    using RentConnect.API.Extentions;
    using RentConnect.API.Policies;
    using RentConnect.Models.Configs;
    using RentConnect.Models.Context;
    using RentConnect.Services.Implementations;
    using RentConnect.Services.Interfaces;
    using System;
    using System.Reflection;

    public static class HostingExtensions
    {
        private const string CorsPolicy = "_MyAllowSubdomainPolicy";
        private const string IdentityServerSettings = "IdentityServerSettings";

        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            // builder.Services.AddApplicationInsightsTelemetry();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            var assembly = typeof(ApiContext).GetTypeInfo().Assembly.GetName().Name;
            builder.Services.AddDbContext<ApiContext>(builder =>
            builder.UseSqlServer(connectionString, b => b.MigrationsAssembly("RentConnect.API")));
            // load mail settings from appsettings.json
            var mailSettings = builder.Configuration.GetSection("MailSettings").Get<MailSetting>();

            // Add mail service to DI
            builder.Services.AddTransient<IMailService, MailService>(x => new MailService(mailSettings));

            builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection(IdentityServerSettings));

            builder.Services.AddHttpContextAccessor(); // Needed for HttpContextHelper

            builder.Services.ConfigureDIServices();

            // Add MVC support
            builder.Services.AddMvc(options => { options.EnableEndpointRouting = true; });

            // configure authentication to use IdentityServer4 (e.g. STS)
            var identityServerSettings = builder.Configuration.GetSection("IdentityServerSettings").Get<IdentityServerSettings>();

            // load the configuration settings
            var ServerSettings = builder.Configuration.GetSection("IdentityServerSettings").Get<ServerSettings>();

            // add the configuration settings to the dependency injection container
            builder.Services.AddSingleton(ServerSettings);

            var StripeConfiguration = builder.Configuration.GetSection("StripeConfiguration").Get<StripeSetting>();
            builder.Services.AddSingleton(StripeConfiguration);

            builder.Services.AddCors(options =>
              options.AddPolicy(CorsPolicy, builder => builder.WithOrigins("http://localhost:4200",
            "https://localhost:5000",
            "http://localhost:5001").AllowAnyHeader().AllowAnyMethod()));
            // Retrieve the IWebHostEnvironment service

            // Add Authorization Policies
            builder.Services.ConfigureAuthorizationPolicies();

            builder.Services.AddAuthentication("Bearer").AddIdentityServerAuthentication(identityServerSettings.GetOptions());

            // configure services for injection
            builder.Services
                .AddControllers()
                .AddNewtonsoftJson();

            var defaultApiVersion = new ApiVersion(1, 0);
            var apiVersionReader = new HeaderApiVersionReader("api-version");
            builder.Services
                .AddMvcCore();

            // Add support for versioning in the API
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = defaultApiVersion;
                options.ApiVersionReader = apiVersionReader;
                options.ReportApiVersions = true;
            });

            // This is to have Swagger support multiple versions.

            // configure Global Application Settings
            PopulateGlobalSettings(builder.Services);
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddControllers();
            var environment = builder.Services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();

            // Check if the current environment is production
            if (!environment.IsProduction())
            {
                // Add Swagger services
                RegisterDocumentationGenerators(builder.Services);
            }

            //  builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
        });

            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment() || !app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                var virDir = app.Configuration.GetSection("VirtualDirectory");
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint(virDir.Value + "/swagger/v1/swagger.json", "v1");
                });
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseRouting();
            app.UseStaticFiles();
            app.ConfigureExceptionHandler();
            app.UseHttpsRedirection();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.ConfigureRedundantStatusCodePages(); // Provide JSON responses for standard response codes such as HTTP 401.
                                                     //  app.UseHttpContextHelper(); // Helper to get Base URL anywhere in application
            app.MapControllers();
            return app;
        }

        private static void PopulateGlobalSettings(IServiceCollection services)
        {
            services.AddSingleton<GlobalAppSettings>(new GlobalAppSettings()
            {
                DefaultMinutesPerTimeSlot = 30, // Hard coded for now, till WI#22005 makes this data driven and populated from the populate method.
                SlotsToDisplayPerStore = 5 // will probably always stay hard coded but this can be moved to a global setting that's data driven.
            });
        }

        private static void RegisterDocumentationGenerators(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Lordhood.API",
                    Description = "An ASP.NET Core Web API for managing Lordhood.APi items"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });
        }
    }
}