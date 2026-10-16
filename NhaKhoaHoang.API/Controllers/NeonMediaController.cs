
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
public sealed class NeonMediaController : ControllerBase
{
    private const long MaxImageBytes = 2 * 1024 * 1024;

    private readonly NhaKhoaDbContext _db;

    public NeonMediaController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // =============================================
    // UPLOAD ẢNH - CHỈ ADMIN
    // =============================================

    [Authorize(Roles = "Admin")]
    [HttpPost("api/admin/media/neon-upload")]
    [RequestSizeLimit(MaxImageBytes + 65536)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MaxImageBytes + 65536)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Vui lòng chọn ảnh."
            });
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest(new
            {
                success = false,
                message = "Ảnh không được vượt quá 2 MB."
            });
        }

        var adminIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(adminIdValue, out int adminId)
            || adminId <= 0)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Phiên Admin không hợp lệ."
            });
        }

        await using var input = file.OpenReadStream();

        using var memory = new MemoryStream();

        await input.CopyToAsync(memory, ct);

        if (memory.Length > MaxImageBytes)
        {
            return BadRequest(new
            {
                success = false,
                message = "Ảnh vượt quá dung lượng cho phép."
            });
        }

        var bytes = memory.ToArray();

        // Kiểm tra chữ ký file thay vì chỉ tin
        // vào phần mở rộng hoặc Content-Type gửi lên.
        var contentType = DetectImageType(bytes);

        if (contentType == null)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Chỉ hỗ trợ ảnh JPEG, PNG hoặc WebP hợp lệ."
            });
        }

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };

        var now = DateTime.UtcNow;

        var media = new MediaFile
        {
            FileName = Guid.NewGuid().ToString("N")
                + extension,

            StorageProvider = "neon",

            StorageKey = Guid.NewGuid().ToString("N"),

            ContentType = contentType,

            FileSizeBytes = bytes.LongLength,

            UploadedBy = adminId,

            CreatedAt = now,

            ImageData = bytes
        };

        _db.MediaFiles.Add(media);

        await _db.SaveChangesAsync(ct);

        // URL chính thức của Backend Render.
        // Ảnh lưu vào Neon và được phục vụ qua API.
        media.PublicUrl =
            "https://nhakhoahoang-be.onrender.com" +
            $"/api/media/{media.MediaId}";

        await _db.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                success = true,
                message = "Upload ảnh thành công.",
                data = new
                {
                    mediaId = media.MediaId,
                    url = media.PublicUrl,
                    fileName = media.FileName,
                    contentType = media.ContentType,
                    size = media.FileSizeBytes
                }
            });
    }

    // =============================================
    // HIỂN THỊ ẢNH - CÔNG KHAI
    // =============================================

    [AllowAnonymous]
    [HttpGet("api/media/{id:long}")]
    public async Task<IActionResult> GetImage(
        long id,
        CancellationToken ct)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var image = await _db.MediaFiles
            .AsNoTracking()
            .Where(m =>
                m.MediaId == id &&
                m.StorageProvider == "neon")
            .Select(m => new
            {
                m.ImageData,
                m.ContentType
            })
            .FirstOrDefaultAsync(ct);

        if (image?.ImageData == null ||
            image.ImageData.Length == 0)
        {
            return NotFound();
        }

        var contentType = DetectImageType(
            image.ImageData);

        if (contentType == null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] =
            "nosniff";

        Response.Headers["Cache-Control"] =
            "public, max-age=3600";

        return File(
            image.ImageData,
            contentType);
    }

    // =============================================
    // KIỂM TRA ĐỊNH DẠNG ẢNH
    // =============================================

    private static string? DetectImageType(byte[] bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        byte[] png =
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

        if (bytes.Length >= png.Length &&
            bytes.Take(png.Length).SequenceEqual(png))
        {
            return "image/png";
        }

        // WebP: RIFF....WEBP
        if (bytes.Length >= 12 &&
            bytes[0] == 'R' &&
            bytes[1] == 'I' &&
            bytes[2] == 'F' &&
            bytes[3] == 'F' &&
            bytes[8] == 'W' &&
            bytes[9] == 'E' &&
            bytes[10] == 'B' &&
            bytes[11] == 'P')
        {
            return "image/webp";
        }

        return null;
    }
}
