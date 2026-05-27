using HealthMate.Application.Abstractions.Storage;
using Microsoft.AspNetCore.Hosting;

namespace HealthMate.Infrastructure.Storage;

public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string contentType, string folder, string fileName, CancellationToken ct = default)
    {
        _ = contentType;

        if (content.Length == 0)
        {
            throw new ArgumentException("File content is empty.", nameof(content));
        }

        var safeFileName = Path.GetFileName(fileName);
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "Uploads", folder);
        Directory.CreateDirectory(uploadsRoot);

        var fullPath = Path.Combine(uploadsRoot, safeFileName);
        await using (var destination = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(destination, ct);
        }

        return Path.Combine("Uploads", folder, safeFileName).Replace("\\", "/");
    }

    public Task<byte[]> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        return File.ReadAllBytesAsync(ToFullPath(relativePath), ct);
    }

    public bool Delete(string relativePath)
    {
        var fullPath = ToFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        return true;
    }

    private string ToFullPath(string relativePath)
    {
        return Path.Combine(environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
