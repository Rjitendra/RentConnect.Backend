namespace RentConnect.STS.Controllers
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ManageViewModel
    {
        public IEnumerable<SelectListItem> AvailableRoles { get; set; }

        public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Roles")]
        public IEnumerable<string> UserRoles { get; set; }
    }
}