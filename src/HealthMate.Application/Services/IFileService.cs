using Microsoft.AspNetCore.Http;
public interface IFileService
{
    /// <summary>
    /// Saves a file to the specified folder path
    /// </summary>
    /// <param name="file">The file to save</param>
    /// <param name="folderPath">The folder path where the file should be saved</param>
    /// <returns>The full path of the saved file</returns>
    Task<string> SaveFileAsync(IFormFile file, string folderPath,string savedFileName);

    /// <summary>
    /// Retrieves a file as a byte array
    /// </summary>
    /// <param name="filePath">The path of the file to retrieve</param>
    /// <returns>The file content as a byte array</returns>
    Task<byte[]> GetFileAsync(string filePath); // this should be implemented and used to send the profile picture

    /// <summary>
    /// Deletes a file from the specified path
    /// </summary>
    /// <param name="filePath">The path of the file to delete</param>
    /// <returns>True if the file was deleted, false if the file didn't exist</returns>
    bool DeleteFile(string filePath);

    // i should create function to check if specif file is still on the server or not
}
