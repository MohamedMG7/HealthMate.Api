using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;


namespace HealthMate.Application.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _enviroment;
        public FileService(IWebHostEnvironment enviroment)
        {
            _enviroment = enviroment;
        }

		// i will need to change the name of the file stored so i may change the method or i can make overloads that give me the ability to add the file name manually
		public async Task<string> SaveFileAsync(IFormFile file, string folderName, string savedFileName)
		{
			if (file == null || file.Length == 0)
			{
				throw new ArgumentNullException(nameof(file));
			}

			var uploadsRoot = Path.Combine(_enviroment.ContentRootPath, "Uploads", folderName);

			// Ensure the directory exists
			if (!Directory.Exists(uploadsRoot))
			{
				Directory.CreateDirectory(uploadsRoot);
			}

			// Generate the file name with extension
			var fileName = savedFileName + Path.GetExtension(file.FileName);
			var fullPath = Path.Combine(uploadsRoot, fileName);

			// Save the file to disk
			using (var stream = new FileStream(fullPath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			// Return the relative path only (what you'll store in the DB and return to the client)
			var relativePath = Path.Combine("Uploads", folderName, fileName).Replace("\\", "/");

			return relativePath;
		}


		public async Task<byte[]> GetFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        public bool DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        
    }
}
