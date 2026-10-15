
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class SitePagesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public SitePagesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/pages
    [AllowAnonymous]
    [HttpGet("pages")]
    public async Task<IActionResult> GetPublic(
        CancellationToken ct)
    {
        var data = await _db.SitePages
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.PageId)
            .Select(p => new
            {
                p.PageId,
                p.Title,
                p.Slug,
                p.Summary,
                p.BannerUrl,
                p.DisplayOrder
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/pages/by-slug/{slug}
    [AllowAnonymous]
    [HttpGet("pages/by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken ct)
    {
        var data = await _db.SitePages
            .AsNoTracking()
            .Where(p =>
                p.Slug == slug &&
                p.IsPublished)
            .Select(p => new
            {
                p.PageId,
                p.Title,
                p.Slug,
                p.Summary,
                p.Content,
                p.BannerUrl,
                p.DisplayOrder
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy trang công khai."
            });

        return Ok(new
        {
            success = true,
            data
        });
    }

    // GET /api/admin/pages
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/pages")]
    public async Task<IActionResult> GetAdmin(
        CancellationToken ct)
    {
        var data = await _db.SitePages
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.PageId)
            .Select(p => new
            {
                p.PageId,
                p.Title,
                p.Slug,
                p.Summary,
                p.BannerUrl,
                p.IsPublished,
                p.DisplayOrder,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/pages/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/pages/{id:int}")]
    public async Task<IActionResult> GetAdminById(
        int id,
        CancellationToken ct)
    {
        var data = await _db.SitePages
            .AsNoTracking()
            .Where(p => p.PageId == id)
            .Select(p => new
            {
                p.PageId,
                p.Title,
                p.Slug,
                p.Summary,
                p.Content,
                p.BannerUrl,
                p.IsPublished,
                p.DisplayOrder,
                p.CreatedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy trang."
            });

        return Ok(new
        {
            success = true,
            data
        });
    }

    // POST /api/admin/pages
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/pages")]
    public async Task<IActionResult> Create(
        [FromBody] SitePageRequest request,
        CancellationToken ct)
    {
        var title = request.Title.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new
            {
                success = false,
                message = "Tiêu đề trang không hợp lệ."
            });

        if (await _db.SitePages.AnyAsync(
            p => p.Slug == slug, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug trang đã tồn tại."
            });

        var page = new SitePage
        {
            Title = title,
            Slug = slug,
            Summary = request.Summary?.Trim(),
            Content = request.Content,
            BannerUrl = request.BannerUrl?.Trim(),
            IsPublished = request.IsPublished,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.SitePages.Add(page);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã tạo trang.",
            data = new
            {
                page.PageId,
                page.Title,
                page.Slug,
                page.IsPublished
            }
        });
    }

    // PUT /api/admin/pages/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/pages/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SitePageRequest request,
        CancellationToken ct)
    {
        var page = await _db.SitePages
            .FirstOrDefaultAsync(p => p.PageId == id, ct);

        if (page == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy trang."
            });

        var title = request.Title.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new
            {
                success = false,
                message = "Tiêu đề trang không hợp lệ."
            });

        if (await _db.SitePages.AnyAsync(
            p => p.Slug == slug && p.PageId != id, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug trang đã tồn tại."
            });

        page.Title = title;
        page.Slug = slug;
        page.Summary = request.Summary?.Trim();
        page.Content = request.Content;
        page.BannerUrl = request.BannerUrl?.Trim();
        page.IsPublished = request.IsPublished;
        page.DisplayOrder = request.DisplayOrder;
        page.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật trang.",
            data = new
            {
                page.PageId,
                page.Title,
                page.Slug,
                page.IsPublished
            }
        });
    }

    // DELETE /api/admin/pages/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/pages/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var page = await _db.SitePages
            .FirstOrDefaultAsync(p => p.PageId == id, ct);

        if (page == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy trang."
            });

        _db.SitePages.Remove(page);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa trang."
        });
    }
}
