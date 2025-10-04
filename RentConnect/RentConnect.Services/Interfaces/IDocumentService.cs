namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Services.Utility;
    public interface IDocumentService
    {
        Task<Result<IEnumerable<DocumentDto>>> UploadDocuments(DocumentUploadRequestDto request);

        Task<Result<(byte[] fileBytes, string fileName, string contentType)>> DownloadDocument(long documentId);
        Task<Result> DeleteDocument(long documentId);
        Task<Result<IEnumerable<DocumentDto>>> GetDocumentsByOwner(long ownerId, string ownerType);

        Task<Result<IEnumerable<DocumentDto>>> GetPropertyImages(long? landlordId, long propertyId,long? tenantId);

    }
}
