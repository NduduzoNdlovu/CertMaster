namespace CertMaster.Application.Common.Interfaces;

public record StoredFileResult(string StoragePath, string Sha256Hash, long SizeBytes);

/// <summary>
/// Abstraction over where uploaded files are kept. The default implementation stores
/// them on local disk outside any web-servable directory (never under wwwroot), so an
/// uploaded file can never be fetched directly by URL. Swap the registered
/// implementation in Infrastructure's DependencyInjection to move to S3/Azure Blob/etc.
/// without touching the Application or Api layers.
/// </summary>
public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct);
    Task DeleteAsync(string storagePath, CancellationToken ct);
}
