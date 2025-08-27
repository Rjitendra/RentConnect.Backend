


namespace RentConnect.Models.Dtos.Properties
{
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Models.Entities.Landlords;
    using RentConnect.Models.Entities.Tenants;
    using RentConnect.Models.Enums;
    public class PropertyDto
    {
        public long Id { get; set; }
        // ✅ Relationship to Landlord(1 Landlord → Many Properties)
        public long LandlordId { get; set; }
        // ✅ Relationship to Tenants (1 Property → Many Tenants)
        public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

        // Basic Info
        public string? Title { get; set; } // "2 BHK Apartment in Pune"
        public string? Description { get; set; }
        public PropertyType? PropertyType { get; set; } // Enum: Apartment, Villa, etc.
        public string? BhkConfiguration { get; set; } // "1 BHK", "2 BHK"
        public int? FloorNumber { get; set; }
        public int? TotalFloors { get; set; }
        public double? CarpetAreaSqFt { get; set; }
        public double? BuiltUpAreaSqFt { get; set; }
        public bool? IsFurnished { get; set; }
        public FurnishingType? FurnishingType { get; set; } // Enum: Unfurnished, SemiFurnished, FullyFurnished
        public int? NumberOfBathrooms { get; set; }
        public int? NumberOfBalconies { get; set; }

        // Location
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string? Locality { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Rent Details
        public decimal? MonthlyRent { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public bool? IsNegotiable { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public LeaseType? LeaseType { get; set; } // Enum: ShortTerm, LongTerm

        // Amenities
        public bool? HasLift { get; set; }
        public bool? HasParking { get; set; }
        public bool? HasPowerBackup { get; set; }
        public bool? HasWaterSupply { get; set; }
        public bool? HasGasPipeline { get; set; }
        public bool? HasSecurity { get; set; }
        public bool? HasInternet { get; set; }

        // Status
        public PropertyStatus? Status { get; set; } // Enum: Draft, Listed, Rented, Archived

        // Audit
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation - Generic document link
        public IList<DocumentUploadDto> Documents { get; set; } = new List<DocumentUploadDto>();
    }
}
