using System.ComponentModel.DataAnnotations;

namespace RentConnect.Models.Dtos
{
    public class UserRegistrationDto
    {
        public string ConfirmPassword { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Password { get; set; }

        [Required]
        public string? CountryCode { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public string? Address { get; set; }

        [Required]
        public string? Postcode { get; set; }

        public bool AcceptTerms { get; set; }
    }
}