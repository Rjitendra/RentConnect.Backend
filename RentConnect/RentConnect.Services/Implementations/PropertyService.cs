using Microsoft.EntityFrameworkCore;
using RentConnect.Models.Context;
using RentConnect.Models.Dtos.Properties;
using RentConnect.Models.Entities.Landlords;
using RentConnect.Models.Entities.Properties;
using RentConnect.Models.Enums;
using RentConnect.Services.Interfaces;
using RentConnect.Services.Utility;

namespace RentConnect.Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        private readonly ApiContext _context;

        public PropertyService(ApiContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<PropertyDto>>> GetPropertyList(long landlordId)
        {
            try
            {
                var properties = await _context.Property
                    .Where(p => p.LandlordId == landlordId)
                    .Include(p => p.Tenants)
                    .ToListAsync();
                var documents = await this._context.Document.Where(x => x.OwnerId == landlordId).ToArrayAsync();

                foreach (var property in properties)
                {
                    property.Documents = documents.Where(d => d.PropertyId == property.Id).ToList(); ;
                }

                var propertyDtos = properties.Select(MapToDto).ToList();
                return Result<IEnumerable<PropertyDto>>.Success(propertyDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<PropertyDto>>.Failure($"Failed to get property list: {ex.Message}");
            }
        }

        public async Task<Result<PropertyDto>> GetProperty(long id)
        {
            try
            {
                var property = await _context.Property
                    .Include(p => p.Tenants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                    return Result<PropertyDto>.Failure("Property not found");

                var documents = await this._context.Document.Where(x => x.PropertyId == property.Id).ToArrayAsync();
                property.Documents = documents;
                var propertyDto = MapToDto(property);
                return Result<PropertyDto>.Success(propertyDto);
            }
            catch (Exception ex)
            {
                return Result<PropertyDto>.Failure($"Failed to get property: {ex.Message}");
            }
        }

        public async Task<Result<long>> AddPropertyDetail(PropertyDto propertyDto)
        {
            try
            {
                var property = MapToEntity(propertyDto);
                property.CreatedOn = DateTime.UtcNow;
                property.UpdatedOn = DateTime.UtcNow;
                property.Status = PropertyStatus.Draft;

                _context.Property.Add(property);
                await _context.SaveChangesAsync();

                return Result<long>.Success(property.Id);
            }
            catch (Exception ex)
            {
                return Result<long>.Failure($"Failed to add property: {ex.Message}");
            }
        }

        public async Task<Result<PropertyDto>> UpdatePropertyDetail(PropertyDto propertyDto)
        {
            try
            {
                var existingProperty = await _context.Property
                   .Include(p => p.Tenants)
                    .FirstOrDefaultAsync(p => p.Id == propertyDto.Id);

                if (existingProperty == null)
                    return Result<PropertyDto>.Failure("Property not found");

                // Update property details
                UpdateEntityFromDto(existingProperty, propertyDto);
                existingProperty.UpdatedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedDto = MapToDto(existingProperty);
                return Result<PropertyDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                return Result<PropertyDto>.Failure($"Failed to update property: {ex.Message}");
            }
        }

        public async Task<Result<long>> DeleteProperty(long id)
        {
            try
            {
                var property = await _context.Property.FindAsync(id);
                if (property == null)
                    return Result<long>.Failure("Property not found");

                // Soft delete - you might want to implement a IsDeleted flag instead
                _context.Property.Remove(property);
                await _context.SaveChangesAsync();

                return Result<long>.Success(id);
            }
            catch (Exception ex)
            {
                return Result<long>.Failure($"Failed to delete property: {ex.Message}");
            }
        }

        public async Task<Result<byte[]>> DownloadPropertyFiles(DocumentCategory category, long propertyId)
        {
            try
            {
                var documents = await _context.Document
                    .Where(d => d.OwnerId == propertyId &&
                               d.OwnerType == "Property" &&
                               d.Category == category)
                    .ToListAsync();

                if (!documents.Any())
                    return Result<byte[]>.Failure("No documents found for the specified criteria");

                // For now, return the first document as bytes
                // You might want to implement a ZIP creation logic here for multiple files
                var firstDoc = documents.First();
                var filePath = Path.Combine("wwwroot", firstDoc.Url?.TrimStart('/') ?? "");

                if (!File.Exists(filePath))
                    return Result<byte[]>.Failure("File not found on disk");

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                return Result<byte[]>.Success(fileBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Failed to download files: {ex.Message}");
            }
        }

        private PropertyDto MapToDto(Property property)
        {
            return new PropertyDto
            {
                Id = property.Id,
                LandlordId = property.LandlordId,
                Title = property.Title,
                Description = property.Description,
                PropertyType = property.PropertyType,
                BhkConfiguration = property.BhkConfiguration,
                FloorNumber = property.FloorNumber,
                TotalFloors = property.TotalFloors,
                CarpetAreaSqFt = property.CarpetAreaSqFt,
                BuiltUpAreaSqFt = property.BuiltUpAreaSqFt,
                IsFurnished = property.IsFurnished,
                FurnishingType = property.FurnishingType,
                NumberOfBathrooms = property.NumberOfBathrooms,
                NumberOfBalconies = property.NumberOfBalconies,
                AddressLine1 = property.AddressLine1,
                AddressLine2 = property.AddressLine2,
                Landmark = property.Landmark,
                Locality = property.Locality,
                City = property.City,
                State = property.State,
                PinCode = property.PinCode,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                MonthlyRent = property.MonthlyRent,
                SecurityDeposit = property.SecurityDeposit,
                IsNegotiable = property.IsNegotiable,
                AvailableFrom = property.AvailableFrom,
                LeaseType = property.LeaseType,
                HasLift = property.HasLift,
                HasParking = property.HasParking,
                HasPowerBackup = property.HasPowerBackup,
                HasWaterSupply = property.HasWaterSupply,
                HasGasPipeline = property.HasGasPipeline,
                HasSecurity = property.HasSecurity,
                HasInternet = property.HasInternet,
                Status = property.Status,
                CreatedOn = property.CreatedOn,
                UpdatedOn = property.UpdatedOn,
                Tenants = property.Tenants,
                Documents = property.Documents?.Select(d => new Models.Dtos.Document.DocumentUploadDto
                {
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    Category = d.Category,
                    Url = d.Url,
                    Name = Path.GetFileName(d.Url),
                    DocumentIdentifier = d.Id.ToString(),
                    Size=d.Size,
                    Type=d.Type
                  
                }).ToList() ?? new List<Models.Dtos.Document.DocumentUploadDto>()
            };
        }

        private Property MapToEntity(PropertyDto dto)
        {
            return new Property
            {
                LandlordId = dto.LandlordId,
                Title = dto.Title,
                Description = dto.Description,
                PropertyType = dto.PropertyType,
                BhkConfiguration = dto.BhkConfiguration,
                FloorNumber = dto.FloorNumber,
                TotalFloors = dto.TotalFloors,
                CarpetAreaSqFt = dto.CarpetAreaSqFt,
                BuiltUpAreaSqFt = dto.BuiltUpAreaSqFt,
                IsFurnished = dto.IsFurnished,
                FurnishingType = dto.FurnishingType,
                NumberOfBathrooms = dto.NumberOfBathrooms,
                NumberOfBalconies = dto.NumberOfBalconies,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                Landmark = dto.Landmark,
                Locality = dto.Locality,
                City = dto.City,
                State = dto.State,
                PinCode = dto.PinCode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                MonthlyRent = dto.MonthlyRent,
                SecurityDeposit = dto.SecurityDeposit,
                IsNegotiable = dto.IsNegotiable,
                AvailableFrom = dto.AvailableFrom,
                LeaseType = dto.LeaseType,
                HasLift = dto.HasLift,
                HasParking = dto.HasParking,
                HasPowerBackup = dto.HasPowerBackup,
                HasWaterSupply = dto.HasWaterSupply,
                HasGasPipeline = dto.HasGasPipeline,
                HasSecurity = dto.HasSecurity,
                HasInternet = dto.HasInternet,
                Status = dto.Status
            };
        }

        private void UpdateEntityFromDto(Property entity, PropertyDto dto)
        {
            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.PropertyType = dto.PropertyType;
            entity.BhkConfiguration = dto.BhkConfiguration;
            entity.FloorNumber = dto.FloorNumber;
            entity.TotalFloors = dto.TotalFloors;
            entity.CarpetAreaSqFt = dto.CarpetAreaSqFt;
            entity.BuiltUpAreaSqFt = dto.BuiltUpAreaSqFt;
            entity.IsFurnished = dto.IsFurnished;
            entity.FurnishingType = dto.FurnishingType;
            entity.NumberOfBathrooms = dto.NumberOfBathrooms;
            entity.NumberOfBalconies = dto.NumberOfBalconies;
            entity.AddressLine1 = dto.AddressLine1;
            entity.AddressLine2 = dto.AddressLine2;
            entity.Landmark = dto.Landmark;
            entity.Locality = dto.Locality;
            entity.City = dto.City;
            entity.State = dto.State;
            entity.PinCode = dto.PinCode;
            entity.Latitude = dto.Latitude;
            entity.Longitude = dto.Longitude;
            entity.MonthlyRent = dto.MonthlyRent;
            entity.SecurityDeposit = dto.SecurityDeposit;
            entity.IsNegotiable = dto.IsNegotiable;
            entity.AvailableFrom = dto.AvailableFrom;
            entity.LeaseType = dto.LeaseType;
            entity.HasLift = dto.HasLift;
            entity.HasParking = dto.HasParking;
            entity.HasPowerBackup = dto.HasPowerBackup;
            entity.HasWaterSupply = dto.HasWaterSupply;
            entity.HasGasPipeline = dto.HasGasPipeline;
            entity.HasSecurity = dto.HasSecurity;
            entity.HasInternet = dto.HasInternet;
            entity.Status = dto.Status;
        }
    }
}
