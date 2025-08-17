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

    public enum DocumentType
    {
        Aadhaar,
        PAN,
        OwnershipProof,
        UtilityBill,
        NoObjectionCertificate,
        BankProof,
        PropertyPhoto,
        RentalAgreement,
        AddressProof,
        IdProof,
        ProfilePhoto,
        EmploymentProof,
        PersonPhoto,
    }
}