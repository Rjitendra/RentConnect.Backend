namespace RentConnect.Services.Utility
{
    using RentConnect.Models.Enums;
    using RentConnect.Models.Utility;
    using System.Security.Claims;

    public class CurrentUser
    {
        public CurrentUser(ClaimsPrincipal user)
        {
            // set application user id
            this.ApplicationUserId = Convert.ToInt32(user.FindFirstValue(IdentityServerClaims.Subject));
            this.Email = user.FindFirstValue(IdentityServerClaims.Email);
            this.FirstName = user.FindFirstValue(IdentityServerClaims.FirstName);
            this.LastName = user.FindFirstValue(IdentityServerClaims.LastName);
            this.FullName = user.FindFirstValue(IdentityServerClaims.FullName);

            // set user role
            var role = user.FindAll(ApplicationClaims.RoleId);
            if (role != null)
            {
                this.Role = new List<ApplicationUserRole>();
                foreach (Claim rol in role)
                {
                    this.Role.Add((ApplicationUserRole)Enum.Parse(typeof(ApplicationUserRole), rol.Value));
                }
            }
        }

        /// <summary>
        /// User subject claim (e.g. ApplicationUser PK). Relates to <see cref="Entity.ApplicationUser"/>.
        /// </summary>
        public int ApplicationUserId { get; }

        /// <summary>
        /// User's role.
        /// </summary>
        public List<ApplicationUserRole> Role { get; }

        public string Email { get; }

        public string FirstName { get; }
        public string LastName { get; }

        public string FullName { get; }
    }
}