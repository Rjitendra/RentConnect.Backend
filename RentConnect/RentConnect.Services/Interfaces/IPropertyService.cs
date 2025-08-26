namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos.Properties;
    using RentConnect.Models.Enums;
    using RentConnect.Services.Utility;
    public interface IPropertyService
    {
        Task<Result<IEnumerable<PropertyDto>>> GetPropertyList(int landlordId);

        Task<Result<PropertyDto>> GetProperty(long id);

        Task<Result<long>> AddPropertyDetail(PropertyDto filterDto);

        Task<Result<PropertyDto>> UpdatePropertyDetail(PropertyDto filterDto);

        Task<Result<long>> DeleteProperty(long Id);

        Task<Result<byte[]>> DownloadPropertyFiles(DocumentCategory category, long propertyId);
    }
}
