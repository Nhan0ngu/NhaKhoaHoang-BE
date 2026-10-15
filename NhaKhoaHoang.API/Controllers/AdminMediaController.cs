
using System.Security.Claims;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.Models;
using NhaKhoaHoang.API.Services;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/media")]
public class AdminMediaController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;
    private readonly MediaStorageService _storage;
    private readonly ILogger<AdminMediaController> _logger;

    public AdminMediaController(
        NhaKhoaDbContext db,
        MediaStorageService storage,
        ILogger<AdminMediaController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    // =======================================
    // GET /api/admin/media
    // =======================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new
            {
                success = false,
                message = "Phân trang không hợp lệ."
            });

        var query = _db.MediaFiles.AsNoTracking();
        var total = await query.CountAsync(ct);

        var data = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MediaId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.MediaId,
                m.FileName,
                m.PublicUrl,
                m.ContentType,
                m.FileSizeBytes,
                m.CreatedAt,
                m.UploadedBy
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            data
        });
    }

    // =======================================
    // GET /api/admin/media/{id}
    // =======================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken ct)
    {
        var data = await _db.MediaFiles
            .AsNoTracking()
            .Where(m => m.MediaId == id)
            .Select(m => new
            {
                m.MediaId,
                m.FileName,
                m.StorageProvider,
                m.StorageKey,
                m.PublicUrl,
                m.ContentType,
                m.FileSizeBytes,
                m.CreatedAt,
                m.UploadedBy
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy hình ảnh."
            });

        return Ok(new { success = true, data });
    }

    // =======================================
    // POST /api/admin/media/upload
    // =======================================

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new
            {
                success = false,
                message = "Vui lòng chọn hình ảnh."
            });

        if (file.Length > _storage.MaxFileSizeBytes)
            return BadRequest(new
            {
                success = false,
                message = "Ảnh vượt quá dung lượng cho phép."
            });

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (contentType == null)
            return BadRequest(new
            {
                success = false,
                message = "Chỉ hỗ trợ JPG, PNG và WebP."
            });

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, ct);

        var bytes = memory.ToArray();

        if (!HasValidImageSignature(bytes, extension))
            return BadRequest(new
            {
                success = false,
                message = "Nội dung file không khớp định dạng ảnh."
            });

        // Prevent detached or disabled admins from uploading.
        var idClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(idClaim, out var adminId))
            return Unauthorized();

        var isActiveAdmin = await _db.AdminUsers
            .AsNoTracking()
            .AnyAsync(
                a => a.AdminId == adminId && a.IsActive,
                ct);

        if (!isActiveAdmin)
            return Unauthorized();

        memory.Position = 0;

        MediaStorageService.UploadedBlob uploaded;

        try
        {
            uploaded = await _storage.UploadAsync(
                memory,
                extension,
                contentType,
                ct);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob upload failed.");

            return StatusCode(503, new
            {
                success = false,
                message = "Dịch vụ lưu trữ ảnh chưa khả dụng."
            });
        }

        var originalName = Path.GetFileName(file.FileName);

        // Limit the original file name to fit the DB column.
        if (originalName.Length > 255)
            originalName = originalName[^255..];

        var media = new MediaFile
        {
            FileName = originalName,
            StorageProvider = "azure",
            StorageKey = uploaded.StorageKey,
            PublicUrl = uploaded.PublicUrl,
            ContentType = contentType,
            FileSizeBytes = file.Length,
            UploadedBy = adminId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _db.MediaFiles.Add(media);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // If SQL save fails, remove the newly uploaded blob.
            try
            {
                await _storage.DeleteAsync(
                    uploaded.StorageKey,
                    CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                _logger.LogError(
                    cleanupError,
                    "Could not clean up orphan blob {Key}",
                    uploaded.StorageKey);
            }

            throw;
        }

        return StatusCode(201, new
        {
            success = true,
            message = "Upload hình ảnh thành công.",
            data = new
            {
                media.MediaId,
                media.FileName,
                media.PublicUrl,
                media.ContentType,
                media.FileSizeBytes,
                media.CreatedAt
            }
        });
    }

    // =======================================
    // DELETE /api/admin/media/{id}
    // =======================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken ct)
    {
        var media = await _db.MediaFiles
            .FirstOrDefaultAsync(m => m.MediaId == id, ct);

        if (media == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy hình ảnh."
            });

        // Only remove media managed by this storage service.
        if (media.StorageProvider != "azure")
            return Conflict(new
            {
                success = false,
                message = "Không hỗ trợ xóa ảnh từ storage khác."
            });

        if (await IsReferencedAsync(media.PublicUrl, ct))
            return Conflict(new
            {
                success = false,
                message =
                    "Ảnh đang được sử dụng trên website. " +
                    "Hãy thay ảnh trong nội dung trước khi xóa."
            });

        // Delete blob first. If SQL delete fails afterward,
        // reconcile the dangling metadata during maintenance.
        try
        {
            await _storage.DeleteAsync(media.StorageKey, ct);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob delete failed.");

            return StatusCode(503, new
            {
                success = false,
                message = "Không thể xóa file khỏi storage."
            });
        }

        _db.MediaFiles.Remove(media);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa hình ảnh."
        });
    }

    // =======================================
    // REFERENCE CHECK
    // =======================================

    private async Task<bool> IsReferencedAsync(
        string? url,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return
            await _db.Services.AnyAsync(
                x => x.ThumbnailUrl == url, ct) ||
            await _db.Articles.AnyAsync(
                x => x.CoverImageUrl == url, ct) ||
            await _db.Doctors.AnyAsync(
                x => x.PhotoUrl == url, ct) ||
            await _db.SitePages.AnyAsync(
                x => x.BannerUrl == url, ct) ||
            await _db.SiteSettings.AnyAsync(
                x => x.SettingValue == url, ct);
    }

    // =======================================
    // FILE SIGNATURE VALIDATION
    // =======================================

    private static bool HasValidImageSignature(
        byte[] bytes,
        string extension)
    {
        if (extension is ".jpg" or ".jpeg")
        {
            return bytes.Length >= 4 &&
                   bytes[0] == 0xFF &&
                   bytes[1] == 0xD8 &&
                   bytes[2] == 0xFF;
        }

        if (extension == ".png")
        {
            byte[] signature =
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A
            };

            return bytes.Length >= signature.Length &&
                   bytes.Take(signature.Length)
                       .SequenceEqual(signature);
        }

        if (extension == ".webp")
        {
            return bytes.Length >= 12 &&
                   bytes[0] == (byte)'R' &&
                   bytes[1] == (byte)'I' &&
                   bytes[2] == (byte)'F' &&
                   bytes[3] == (byte)'F' &&
                   bytes[8] == (byte)'W' &&
                   bytes[9] == (byte)'E' &&
                   bytes[10] == (byte)'B' &&
                   bytes[11] == (byte)'P';
        }

        return false;
    }
}
