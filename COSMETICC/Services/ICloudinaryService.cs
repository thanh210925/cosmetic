using Microsoft.AspNetCore.Http;

namespace COSMETICC.Services
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads an image file to Cloudinary and returns the secure HTTPS URL.
        /// </summary>
        Task<string?> UploadImageAsync(IFormFile? file, string folderName = "cosmetic_uploads");

        /// <summary>
        /// Uploads an image stream to Cloudinary and returns the secure HTTPS URL.
        /// </summary>
        Task<string?> UploadImageAsync(Stream fileStream, string fileName, string folderName = "cosmetic_uploads");

        /// <summary>
        /// Deletes an image from Cloudinary given its URL or public ID.
        /// </summary>
        Task<bool> DeleteImageAsync(string publicIdOrUrl);
    }
}
