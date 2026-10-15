
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class SiteSettingsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    // Chỉ các khóa này được phép quản lý qua CMS.
    // Tuyệt đối không lưu API key/mật khẩu ở đây.
    private static readonly HashSet<string> AllowedKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "site_name",
            "hotline",
            "contact_email",
            "logo_url",
            "facebook_url",
            "footer_description"
        };

    public SiteSettingsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/site-settings
    [AllowAnonymous]
    [HttpGet("site-settings")]
    public async Task<IActionResult> GetPublic(
        CancellationToken ct)
    {
        var keys = AllowedKeys.ToArray();

        var rows = await _db.SiteSettings
            .AsNoTracking()
            .Where(s => keys.Contains(s.SettingKey))
            .OrderBy(s => s.SettingKey)
            .Select(s => new
            {
                s.SettingKey,
                s.SettingValue
            })
            .ToListAsync(ct);

        var data = rows.ToDictionary(
            x => x.SettingKey,
            x => x.SettingValue);

        return Ok(new
        {
            success = true,
            data
        });
    }

    // GET /api/admin/site-settings
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/site-settings")]
    public async Task<IActionResult> GetAdmin(
        CancellationToken ct)
    {
        var keys = AllowedKeys.ToArray();

        var data = await _db.SiteSettings
            .AsNoTracking()
            .Where(s => keys.Contains(s.SettingKey))
            .OrderBy(s => s.SettingKey)
            .Select(s => new
            {
                s.SettingId,
                s.SettingKey,
                s.SettingValue,
                s.Description,
                s.UpdatedAt,
                s.UpdatedBy
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // PUT /api/admin/site-settings/{key}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/site-settings/{key}")]
    public async Task<IActionResult> Update(
        string key,
        [FromBody] SiteSettingUpdateRequest request,
        CancellationToken ct)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();

        if (!AllowedKeys.Contains(normalizedKey))
            return BadRequest(new
            {
                success = false,
                message = "Thiết lập không được hỗ trợ."
            });

        var value = request.SettingValue?.Trim();

        if (value != null && value.Length > 5000)
            return BadRequest(new
            {
                success = false,
                message = "Giá trị quá dài."
            });

        // Kiểm tra cơ bản URL, tránh javascript: và
        // các giao thức không phù hợp.
        if (normalizedKey is "logo_url" or "facebook_url")
        {
            if (!string.IsNullOrEmpty(value) &&
                (!Uri.TryCreate(
                    value,
                    UriKind.Absolute,
                    out var uri) ||
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "URL phải là địa chỉ HTTPS hợp lệ."
                });
            }
        }

        var adminClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        int? adminId = int.TryParse(adminClaim, out var id)
            ? id
            : null;

        var setting = await _db.SiteSettings
            .FirstOrDefaultAsync(
                s => s.SettingKey == normalizedKey, ct);

        if (setting == null)
        {
            setting = new SiteSetting
            {
                SettingKey = normalizedKey,
                SettingValue = value,
                Description = null,
                UpdatedBy = adminId,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SiteSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = value;
            setting.UpdatedBy = adminId;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật thiết lập website.",
            data = new
            {
                setting.SettingKey,
                setting.SettingValue,
                setting.UpdatedAt
            }
        });
    }
}
