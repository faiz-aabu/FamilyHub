using Microsoft.AspNetCore.Http;

namespace FamilyHub.Models;

public static class ImageUploadValidator
{
    public const long MaxFileSize = 2 * 1024 * 1024;

    public static bool IsValid(IFormFile file, out string error)
    {
        error = string.Empty;
        if (file.Length is <= 0 or > MaxFileSize)
        {
            error = "Uploaded image must be between 1 byte and 2 MB.";
            return false;
        }

        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[8];
        var read = stream.Read(header);
        var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = read == 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

        if (!isJpeg && !isPng)
        {
            error = "Only genuine JPEG and PNG image files are allowed.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if ((!isJpeg || !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            && (!isPng || !string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)))
        {
            error = "The image file extension does not match its content.";
            return false;
        }

        return true;
    }
}
