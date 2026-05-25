using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Application.Manager.DocumentManager{
    public interface IDocumentManager{
        Task<FileResult> GetFileAsync(string filePath);
    }
}