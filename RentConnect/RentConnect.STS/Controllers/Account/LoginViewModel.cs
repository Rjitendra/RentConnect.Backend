namespace RentConnect.STS.Controllers
{
    public class LoginViewModel : LoginInputModel
    {
        public bool AllowRememberLogin { get; set; }

        public bool EnableLocalLogin { get; set; }
    }
}