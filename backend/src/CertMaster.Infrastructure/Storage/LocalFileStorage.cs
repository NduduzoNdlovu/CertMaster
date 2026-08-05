using System.Security.Cryptography;
using CertMaster.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CertMaster.Infrastructure.Storage;

public class LocalStorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Absolute or content-root-relative path where uploaded files are kept.
    /// Must NOT be under wwwroot — nothing in this folder is ever served directly.</summary>
    public string UploadRoot { get; set; } = "App_Data/uploads";
}

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<LocalStorageSettings> options)
    {
        _root = Path.GetFullPath(options.Value.UploadRoot);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFileResult> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct)
    {
        // GUID-based filename: never trust the original filename for the path on disk
        // (avoids path traversal and collisions); the original name is kept separately
        // in the database (ImportJob.OriginalFileName) for display and audit purposes.
        var safeExtension = Path.GetExtension(suggestedFileName);
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(_root, storedFileName);

        using var sha256 = SHA256.Create();
        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        await using (var hashingStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write))
        {
            await content.CopyToAsync(hashingStream, ct);
        }

        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        var sizeBytes = new FileInfo(fullPath).Length;

        return new StoredFileResult(storedFileName, hash, sizeBytes);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_root, storagePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_root, storagePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
