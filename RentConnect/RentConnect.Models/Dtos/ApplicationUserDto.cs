namespace RentConnect.Models.Dtos
{
    public class ApplicationUserDto
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long ApplicationUserId { get; set; }

        /// <summary>
        /// Email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Property for User is Enbaled or not
        /// </summary>
        public bool IsEnabled { get; set; }

        public string UserName { get; set; }

        public List<ApplicationUserRoleDto> UserRoles { get; set; }

        public string Password { get; set; }

        public bool IsResetPassword { get; set; }
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string PhoneNumber { get; set; }

        public DateTime? DateCreated { get; set; }
    }
}