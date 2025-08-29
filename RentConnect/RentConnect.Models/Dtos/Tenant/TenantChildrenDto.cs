namespace RentConnect.Models.Dtos.Tenants
{
    public class TenantChildrenDto
    {
        public long Id { get; set; }
        public int TenantGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime DOB { get; set; }
        public string Occupation { get; set; } = string.Empty;

        // Calculated property - Age is computed from DOB
        public int Age => CalculateAge(DOB);

        // Helper method for age calculation
        private static int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
