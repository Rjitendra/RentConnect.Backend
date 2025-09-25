namespace RentConnect.Models.Enums
{
    public enum PropertyType
    {
        Apartment,
        Villa,
        IndependentHouse,
        RowHouse,
        Plot,
        Studio
    }

    public enum FurnishingType
    {
        Unfurnished,
        SemiFurnished,
        FullyFurnished
    }

    public enum LeaseType
    {
        ShortTerm,
        LongTerm
    }

    public enum PropertyStatus
    {
        Draft,
        Listed,
        Rented,
        Archived
    }

    public enum DocumentCategory
    {
        Aadhaar,
        PAN,
        OwnershipProof,
        UtilityBill,
        NoObjectionCertificate,
        BankProof,
        PropertyImages,
        RentalAgreement,
        AddressProof,
        IdProof,
        ProfilePhoto,
        EmploymentProof,
        PersonPhoto,
        PropertyCondition,
        Other
    }

    // Ticket related enums
    public enum TicketCategory
    {
        Electricity,
        Plumbing,
        Rent,
        Maintenance,
        Appliances,
        Security,
        Cleaning,
        Pest,
        Noise,
        Parking,
        Internet,
        Heating,
        AirConditioning,
        Other
    }

    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TicketStatusType
    {
        Open,
        InProgress,
        Pending,
        Resolved,
        Closed,
        Cancelled
    }

    public enum CreatedByType
    {
        Landlord,
        Tenant
    }
}