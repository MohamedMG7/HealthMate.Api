namespace HealthMate.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string contentType, string folder, string fileName, CancellationToken ct = default);
    Task<byte[]> ReadAsync(string relativePath, CancellationToken ct = default);
    bool Delete(string relativePath);
}
