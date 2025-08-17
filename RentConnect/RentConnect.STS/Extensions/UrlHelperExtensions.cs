namespace RentConnect.STS.Extensions
{
    using Microsoft.AspNetCore.Mvc;
    using RentConnect.STS.Controllers;

    public static class UrlHelperExtensions
    {
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, long userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmEmail),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }

        public static string ResetPasswordCallbackLink(
            this IUrlHelper urlHelper,
            long userId,
            string code,
            string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ResetPassword),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }
    }
}