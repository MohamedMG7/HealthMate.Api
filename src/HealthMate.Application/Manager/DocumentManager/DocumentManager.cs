using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace HealthMate.Application.Manager.DocumentManager
{
    public class DocumentManager : IDocumentManager
    {
        public async Task<FileResult> GetFileAsync(string filePath)
        {
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            
            var fileBytes = await File.ReadAllBytesAsync(filePath);

           
            var contentType = GetContentType(filePath);

            
            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = Path.GetFileName(filePath)
            };
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream" // Default content type
            };
        }
    }
}
