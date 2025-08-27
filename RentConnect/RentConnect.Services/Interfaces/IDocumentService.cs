namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos.Document;
    using RentConnect.Services.Utility;
    public interface IDocumentService
    {
        Task<Result> UploadDocuments(DocumentUploadRequestDto request);
        Task<Result<byte[]>> DownloadDocument(long documentId);
        Task<Result> DeleteDocument(long documentId);
        Task<Result<IEnumerable<DocumentDto>>> GetDocumentsByOwner(long ownerId, string ownerType);


    }
}
