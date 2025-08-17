namespace RentConnect.STS.Controllers
{
    using System.ComponentModel.DataAnnotations;

    public class RegistrationViewModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [StringLength(
                    100,
                    ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                    MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        public string? CountryCode { get; set; }

        [Required]
        [Display(Name = "Mobile Number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string? Address { get; set; }

        [Required]
        public string? Postcode { get; set; }

        [Display(Name = "Upload Address Proof")]
        [Required]
        public IFormFile? UploadAddressProof { get; set; }

        [Display(Name = "Upload Id Proof")]
        [Required]
        public IFormFile? UploadIdProof { get; set; }

        [MustBeTrue(ErrorMessage = "Please Accept the Terms & Conditions")]
        public bool AcceptTerms { get; set; }
    }

    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }

            bool boolValue;
            if (!bool.TryParse(value.ToString(), out boolValue))
            {
                return false;
            }

            return boolValue == true;
        }
    }
}