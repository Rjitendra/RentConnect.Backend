namespace RentConnect.STS.Config
{
    using Duende.IdentityServer;
    using Duende.IdentityServer.Configuration;
    using Duende.IdentityServer.EntityFramework.Options;
    using Duende.IdentityServer.Models;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using RentConnect.Models;
    using RentConnect.Models.Utility;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public static class IdentityServerConfigurations
    {
        public static IEnumerable<ApiScope> ApiScopes(ClientApplicationSettings settings)
        {
            return new List<ApiScope>
        {
            new ApiScope(name:IdentityServerConfigConstants.RentConnectApi, displayName:"RentConnect API")
        };
        }

        /*
         By defining an ApiResource, you are registering your API with Duende Identity Server. This allows clients to request access tokens to authenticate and authorize access to the protected API endpoints. The scopes, secrets, and user claims defined in the ApiResource determine the level of access and the information available to the clients.
         */

        public static IEnumerable<ApiResource> GetApiResources(ClientApplicationSettings settings)
        {
            return new List<ApiResource>
            {
                new ApiResource(IdentityServerConfigConstants.RentConnectApi, "LordhoodAPI")
                {
                    Scopes = { IdentityServerConfigConstants.RentConnectApi },
                    ApiSecrets={ new Secret(settings.ApiSecret.Sha256()) },
                    UserClaims = new []
                    {
                        ClaimTypes.Name,
                        ClaimTypes.GivenName,
                        ClaimTypes.Surname,
                        ApplicationClaims.RoleId
                    }
                }
            };
        }

        public static IEnumerable<Client> GetClients(ClientApplicationSettings settings)
        {
            return new[]
            {
                new Client
                {
                    ClientId = "rentconnect-angular",
                    ClientName = "RentConnect",
                    ClientUri = settings.AngularBaseUrl,
                    AllowedGrantTypes = GrantTypes.Code, // We are using Code with PKCE (Proof Key for Code Exchange). NOTE: This is recommended strategy over implicit.
                    RequirePkce = true, // Forcing requiring PKCE
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    AccessTokenType = AccessTokenType.Jwt,
                    RedirectUris =
                    {
                        $"{settings.AngularBaseUrl}",
                        $"{settings.AngularBaseUrl}assets/silent-callback.html",
                        $"{settings.AngularBaseUrl}signin-callback"
                    },

                     PostLogoutRedirectUris = { settings.AngularBaseUrl+"signout-callback" },
                     AllowedCorsOrigins = { settings.AngularBaseUrl.TrimEnd('/') },
                     AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConfigConstants.RentConnectApi,
                        IdentityServerConfigConstants.RentConnectProfile
                    },
                    AlwaysSendClientClaims = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    IdentityTokenLifetime =1800, // 28800 8 hours
                    AccessTokenLifetime = 1800,  //28800 8 hours
                    RequireClientSecret = false,
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource(IdentityServerConfigConstants.RentConnectProfile, new[] { ApplicationClaims.RoleId })
            };
        }

        internal static Action<IdentityOptions> GetConfigureIdentityOptions()
        {
            return options =>
            {
                // password requirements
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // user validation settings
                options.User.RequireUniqueEmail = true;

                // sign-in settings
                options.SignIn.RequireConfirmedEmail = true;
            };
        }

        /// <summary>
        /// Creates identity server configuration.
        /// </summary>
        /// <returns></returns>
        internal static Action<IdentityServerOptions> GetIdentityServerOptions()
        {
            return options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            };
        }

        /// <summary>
		/// Creates operational store configuration.
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		internal static Action<OperationalStoreOptions> GetOperationalStoreOptions(string connectionString)
        {
            return options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);
                options.EnableTokenCleanup = true;
                options.TokenCleanupInterval = 30;
            };
        }

        /// <summary>
        /// Gets signing certificate embedded resource from assembly and returns as X509 certificate.
        /// </summary>
        /// <returns>Signing certificate.</returns>
        internal static X509Certificate2 GetCertificate()
        {
            var assembly = typeof(IdentityServerConfigurations).GetTypeInfo().Assembly;
            var resourceName = $"{assembly.GetName().Name}.IdentityServer4Auth.pfx";
            var resource = assembly.GetManifestResourceStream(resourceName);

            try
            {
                using (var ms = new MemoryStream())
                {
                    resource?.CopyTo(ms);

                    return new X509Certificate2(ms.ToArray(), "abcd1234");
                }
            }
            catch (CryptographicException ex)
            {
                //Log.Logger.Error(
                //    "Failure loading embedded signing certificate having resource name {0}. Available resources are as follows: {1}.",
                //    resourceName,
                //    string.Join(',', assembly.GetManifestResourceNames()));

                throw ex;
            }
        }
    }
}