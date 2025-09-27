using Microsoft.EntityFrameworkCore;
using RentConnect.Models.Context;
using RentConnect.Models.Dtos.Document;
using RentConnect.Models.Dtos.Properties;
using RentConnect.Models.Dtos.Tenants;
using RentConnect.Models.Entities.Landlords;
using RentConnect.Models.Entities.Properties;
using RentConnect.Models.Entities.Tenants;
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
                    .Where(p => p.LandlordId == landlordId && p.IsDeleted == false)
                    .Include(p => p.Tenants)
                    .ToListAsync();
                var documents = await this._context.Document.Where(x => x.OwnerId == landlordId).ToArrayAsync();

                foreach (var property in properties)
                {
                    property.Documents = documents.Where(d => d.PropertyId == property.Id && d.LandlordId == property.LandlordId).ToList();
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
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false);

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
                    .FirstOrDefaultAsync(p => p.Id == propertyDto.Id && p.IsDeleted == false);

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
                var property = await _context.Property.Include(x => x.Tenants).FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false);
                if (property == null)
                {
                    return Result<long>.Failure("Property not found");
                }
                bool hasTenants = property.Tenants?.Any() == true;
                if (hasTenants) { return Result<long>.Failure("This property is associated with a tenant. Please delete the tenant first."); }



                // Soft delete - you might want to implement a IsDeleted flag instead
                property.IsDeleted = true;
                _context.Property.Update(property);
                await _context.SaveChangesAsync();

                return Result<long>.Success(id);
            }
            catch (Exception ex)
            {
                return Result<long>.Failure($"Failed to delete property: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<DocumentDto>>> DownloadPropertyFiles(DocumentCategory category, long propertyId)
        {
            try
            {
                var documents = await _context.Document
                    .Where(d => d.PropertyId == propertyId &&
                               d.Category == category)
                    .ToListAsync();

                var documentDtos = documents.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    LandlordId = d.LandlordId,
                    PropertyId = d.PropertyId,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Description = d.Description,
                    DocumentIdentifier = d.Id.ToString(),
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified
                }).ToList();

                return Result<IEnumerable<DocumentDto>>.Success(documentDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<DocumentDto>>.Failure($"Failed to get property images: {ex.Message}");
            }
        }

        private PropertyDto MapToDto(Property property)
        {
            return new PropertyDto
            {
                // Identity & Relationships
                Id = property.Id,
                LandlordId = property.LandlordId,
                // Tenants -> comes from navigation, not direct mapping here

                // Basic Info
                Title = property.Title,
                Description = property.Description,
                PropertyType = property.PropertyType,
                BhkConfiguration = property.BhkConfiguration,
                FloorNumber = property.FloorNumber,
                TotalFloors = property.TotalFloors,
                NumberOfBathrooms = property.NumberOfBathrooms,
                NumberOfBalconies = property.NumberOfBalconies,

                // Area & Furnishing
                CarpetAreaSqFt = property.CarpetAreaSqFt,
                BuiltUpAreaSqFt = property.BuiltUpAreaSqFt,
                FurnishingType = property.FurnishingType,

                // Location
                AddressLine1 = property.AddressLine1,
                AddressLine2 = property.AddressLine2,
                Landmark = property.Landmark,
                Locality = property.Locality,
                City = property.City,
                State = property.State,
                PinCode = property.PinCode,
                Latitude = property.Latitude,
                Longitude = property.Longitude,

                // Rent Details
                MonthlyRent = property.MonthlyRent,
                SecurityDeposit = property.SecurityDeposit,
                IsNegotiable = property.IsNegotiable,
                AvailableFrom = property.AvailableFrom,
                LeaseType = property.LeaseType,

                // Amenities
                HasLift = property.HasLift,
                HasParking = property.HasParking,
                HasPowerBackup = property.HasPowerBackup,
                HasWaterSupply = property.HasWaterSupply,
                HasGasPipeline = property.HasGasPipeline,
                HasSecurity = property.HasSecurity,
                HasInternet = property.HasInternet,

                // Validation
                IsValid = property.IsValid,

                // Status
                Status = property.Status,

                // Audit
                CreatedOn = property.CreatedOn,
                UpdatedOn = property.UpdatedOn,
                Tenants = property.Tenants?
                .Select(x => MapEntityTenantDto(x))
                .ToList() ?? new List<TenantDto>(),

                Documents = property.Documents?.Select(d => new Models.Dtos.Document.DocumentDto
                {
                    Id = d.Id,
                    OwnerId = d.OwnerId,
                    OwnerType = d.OwnerType,
                    LandlordId = d.LandlordId,
                    PropertyId = d.PropertyId,
                    Category = d.Category,
                    Url = d.Url,
                    Name = d.Name,
                    Size = d.Size,
                    Type = d.Type,
                    Description = d.Description,
                    DocumentIdentifier = d.Id.ToString(),
                    UploadedOn = d.UploadedOn,
                    IsVerified = d.IsVerified,
                    File = null

                }).ToList() ?? new List<Models.Dtos.Document.DocumentDto>()
            };
        }

        private Property MapToEntity(PropertyDto dto)
        {
            return new Property
            {
                LandlordId = dto.LandlordId.Value,
                Title = dto.Title,
                Description = dto.Description,
                PropertyType = dto.PropertyType,
                BhkConfiguration = dto.BhkConfiguration,
                FloorNumber = dto.FloorNumber,
                TotalFloors = dto.TotalFloors,
                CarpetAreaSqFt = dto.CarpetAreaSqFt,
                BuiltUpAreaSqFt = dto.BuiltUpAreaSqFt,
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

        private TenantDto MapEntityTenantDto(Models.Entities.Tenants.Tenant tenant)
        {
            if (tenant == null) return null;

            return new TenantDto
            {
                Id = tenant.Id,
                LandlordId = tenant.LandlordId,
                PropertyId = tenant.PropertyId,

                // Personal Info
                Name = tenant.Name ?? string.Empty,
                Email = tenant.Email,
                PhoneNumber = tenant.PhoneNumber ?? string.Empty,
                AlternatePhoneNumber = tenant.AlternatePhoneNumber,
                DOB = tenant.DOB ?? DateTime.MinValue,
                Occupation = tenant.Occupation ?? string.Empty,
                Gender = tenant.Gender,
                MaritalStatus = tenant.MaritalStatus,

                // Addresses
                CurrentAddress = tenant.CurrentAddress,
                PermanentAddress = tenant.PermanentAddress,

                // Emergency Contact
                EmergencyContactName = tenant.EmergencyContactName,
                EmergencyContactPhone = tenant.EmergencyContactPhone,
                EmergencyContactRelation = tenant.EmergencyContactRelation,

                // Government IDs
                AadhaarNumber = tenant.AadhaarNumber,
                PanNumber = tenant.PanNumber, // entity → dto naming difference
                DrivingLicenseNumber = tenant.DrivingLicenseNumber,
                VoterIdNumber = tenant.VoterIdNumber,

                // Employment
                EmployerName = tenant.EmployerName,
                EmployerAddress = tenant.EmployerAddress,
                EmployerPhone = tenant.EmployerPhone,
                MonthlyIncome = tenant.MonthlyIncome,
                WorkExperience = tenant.WorkExperience,

                // Tenancy
                TenancyStartDate = tenant.TenancyStartDate ?? DateTime.MinValue,
                TenancyEndDate = tenant.TenancyEndDate,
                RentDueDate = tenant.RentDueDate ?? DateTime.MinValue,
                RentAmount = tenant.RentAmount ?? 0,
                SecurityDeposit = tenant.SecurityDeposit ?? 0,
                MaintenanceCharges = tenant.MaintenanceCharges,
                LeaseDuration = tenant.LeaseDuration ?? 12,
                NoticePeriod = tenant.NoticePeriod ?? 30,

                // Agreement / Onboarding
                AgreementSigned = tenant.AgreementSigned,
                AgreementDate = tenant.AgreementDate,
                AgreementUrl = tenant.AgreementUrl,
                OnboardingEmailSent = tenant.OnboardingEmailSent,
                OnboardingEmailDate = tenant.OnboardingEmailDate,
                OnboardingCompleted = tenant.OnboardingCompleted,

                // Files
                //BackgroundCheckFileUrl = tenant.BackgroundCheckFileUrl,
                //RentGuideFileUrl = tenant.RentGuideFileUrl,
                //DepositReceiptUrl = tenant.DepositReceiptUrl,

                // Acknowledgement / Verification
                IsAcknowledge = tenant.IsAcknowledge ?? false,
                AcknowledgeDate = tenant.AcknowledgeDate,
                IsVerified = tenant.IsVerified ?? false,
                VerificationNotes = tenant.VerificationNotes,

                // Flags
                IsNewTenant = tenant.IsNewTenant ?? true,
                IsPrimary = tenant.IsPrimary ?? false,
                IsActive = tenant.IsActive ?? true,
                // NeedsOnboarding = tenant.NeedsOnboarding ?? true,

                // Grouping
                TenantGroup = tenant.TenantGroup,

                // Audit
                IpAddress = tenant.IpAddress,
                DateCreated = tenant.DateCreated,
                DateModified = tenant.DateModified,

                // Navigation Collections
                Documents = [],


                // Extra UI props
                PropertyName = tenant.Property.Title,  // assuming `Property.Name` exists
                //TenantCount = tenant.Property?.Tenants?.Count ?? 0,
                //StatusDisplay = tenant.IsActive == true ? "Active" : "Inactive",
                //StatusClass = tenant.IsActive == true ? "status-active" : "status-inactive",
                //StatusIcon = tenant.IsActive == true ? "check_circle" : "cancel"
            };
        }


    }
}
