namespace RentConnect.STS.Controllers
{
    using System;

    public class AccountOptions
    {
        /// <summary>
        /// Specify the Windows authentication scheme being used.
        /// </summary>
        public static readonly string WindowsAuthenticationSchemeName = Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme;

        public static bool AllowLocalLogin = true;

        public static bool AllowRememberLogin = true;

        public static bool AutomaticRedirectAfterSignOut = true;

        /// <summary>
        /// If user uses windows auth, should we load the groups from windows.
        /// </summary>
        public static bool IncludeWindowsGroups = false;

        public static string InvalidCredentialsErrorMessage = "Invalid username or password";
        public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);

        public static bool ShowLogoutPrompt = true;
    }
}