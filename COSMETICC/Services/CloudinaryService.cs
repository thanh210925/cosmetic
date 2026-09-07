using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COSMETICC.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IOptions<CloudinarySettings> config, ILogger<CloudinaryService> logger)
        {
            _logger = logger;
            var settings = config.Value;

            if (!string.IsNullOrWhiteSpace(settings.CloudName) &&
                !string.IsNullOrWhiteSpace(settings.ApiKey) &&
                !string.IsNullOrWhiteSpace(settings.ApiSecret) &&
                settings.CloudName != "YOUR_CLOUD_NAME")
            {
                var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Secure = true;
            }
            else
            {
                _logger.LogWarning("Cloudinary credentials are not configured in appsettings.json.");
            }
        }

        public async Task<string?> UploadImageAsync(IFormFile? file, string folderName = "cosmetic_uploads")
        {
            if (file == null || file.Length == 0) return null;

            if (_cloudinary == null)
            {
                _logger.LogError("Cannot upload to Cloudinary: Cloudinary settings are missing or unconfigured.");
                return null;
            }

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return null;
            }

            return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        }

        public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string folderName = "cosmetic_uploads")
        {
            if (fileStream == null || fileStream.Length == 0) return null;

            if (_cloudinary == null)
            {
                _logger.LogError("Cannot upload to Cloudinary: Cloudinary settings are missing or unconfigured.");
                return null;
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folderName,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return null;
            }

            return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicIdOrUrl)
        {
            if (string.IsNullOrWhiteSpace(publicIdOrUrl) || _cloudinary == null) return false;

            var publicId = GetPublicIdFromUrl(publicIdOrUrl);
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        private static string GetPublicIdFromUrl(string url)
        {
            if (!url.Contains("cloudinary.com")) return url;

            try
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
                {
                    var startIndex = uploadIndex + 1;
                    if (segments[startIndex].StartsWith("v") && long.TryParse(segments[startIndex].Substring(1), out _))
                    {
                        startIndex++;
                    }
                    var pathParts = segments.Skip(startIndex);
                    var fullPath = string.Join("/", pathParts);
                    var dotIndex = fullPath.LastIndexOf('.');
                    return dotIndex > 0 ? fullPath.Substring(0, dotIndex) : fullPath;
                }
            }
            catch
            {
                // Fallback to input string
            }
            return url;
        }
    }
}
