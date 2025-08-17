namespace RentConnect.Models.Entities
{
    using Microsoft.AspNetCore.Identity;
    using RentConnect.Models.Dtos;

    public class ApplicationUser : IdentityUser<long>
    {
        public ApplicationUser(string email)
                    : base(email)
        {
            this.Email = email;
        }

        public ApplicationUser(ApplicationUserDto entity)
        {
            this.Id = entity.ApplicationUserId;
            this.AssignEntityValue(entity);
        }

        public ApplicationUser()
        {
        }

        public bool IsEnabled { get; set; }

        public bool IsResetPassword { get; set; }

        public string? CountryCode { get; set; }
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public DateTime? DateCreated { get; set; }
        public ICollection<IdentityUserRole<long>>? Roles { get; } = new List<IdentityUserRole<long>>();
        public ICollection<IdentityUserClaim<long>>? Claims { get; } = new List<IdentityUserClaim<long>>();

        public void Update(ApplicationUserDto entity)
        {
            this.Id = entity.ApplicationUserId;
            this.AssignEntityValue(entity, true);
        }

        public void UpdateAspNetUser(ApplicationUserDto entity)
        {
            this.Address = entity.Address;
            this.Postcode = entity.Postcode;
            this.PhoneNumber = entity.PhoneNumber;
        }

        public void AssignEntityValue(ApplicationUserDto entity, bool isUpdate = false)
        {
            this.UserName = entity.UserName;
            this.Email = entity.Email;
            this.PhoneNumber = entity.PhoneNumber;
            this.PhoneNumberConfirmed = false;
            this.IsEnabled = entity.IsEnabled;
            this.NormalizedUserName = entity.UserName.ToUpper();
            this.NormalizedEmail = entity.Email.ToUpper();
            this.EmailConfirmed = true;
            this.IsResetPassword = entity.IsResetPassword;
            this.Postcode = entity.Postcode;
            this.Address = entity.Address;
            this.DateCreated = DateTime.Now;

            if (!string.IsNullOrEmpty(entity.Password))
            {
                var options = new PasswordHasherOptions();
                options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
                var hasher = new PasswordHasher<ApplicationUser>();
                this.PasswordHash = hasher.HashPassword(this, entity.Password);
                if (!isUpdate)
                {
                    this.SecurityStamp = Guid.NewGuid().ToString();
                }
            }
        }
    }
}