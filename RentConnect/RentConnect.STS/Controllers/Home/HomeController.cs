namespace RentConnect.STS.Controllers
{
    using Duende.IdentityServer.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;

        public HomeController(IIdentityServerInteractionService interaction)
        {
            _interaction = interaction;
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
        }

        public IActionResult Index()
        {
            return this.RedirectToAction("Login", "Account");
        }
    }
}