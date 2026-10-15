
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ServicesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/services
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var data = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsPublished && s.Category.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ServiceId)
            .Select(s => new ServiceResponse
            {
                ServiceId = s.ServiceId,
                CategoryId = s.CategoryId,
                Name = s.Name,
                Slug = s.Slug,
                ShortDescription = s.ShortDescription,
                Content = s.Content,
                ThumbnailUrl = s.ThumbnailUrl,
                IconClass = s.IconClass,
                DisplayOrder = s.DisplayOrder,
                IsPublished = s.IsPublished
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/services/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var data = await _db.Services
            .AsNoTracking()
            .Where(s =>
                s.ServiceId == id &&
                s.IsPublished &&
                s.Category.IsActive)
            .Select(s => new ServiceResponse
            {
                ServiceId = s.ServiceId,
                CategoryId = s.CategoryId,
                Name = s.Name,
                Slug = s.Slug,
                ShortDescription = s.ShortDescription,
                Content = s.Content,
                ThumbnailUrl = s.ThumbnailUrl,
                IconClass = s.IconClass,
                DisplayOrder = s.DisplayOrder,
                IsPublished = s.IsPublished
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy dịch vụ công khai."
            });

        return Ok(new { success = true, data });
    }

    // GET /api/services/by-slug/trong-rang-implant
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken ct)
    {
        var data = await _db.Services
            .AsNoTracking()
            .Where(s =>
                s.Slug == slug &&
                s.IsPublished &&
                s.Category.IsActive)
            .Select(s => new ServiceResponse
            {
                ServiceId = s.ServiceId,
                CategoryId = s.CategoryId,
                Name = s.Name,
                Slug = s.Slug,
                ShortDescription = s.ShortDescription,
                Content = s.Content,
                ThumbnailUrl = s.ThumbnailUrl,
                IconClass = s.IconClass,
                DisplayOrder = s.DisplayOrder,
                IsPublished = s.IsPublished
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy dịch vụ công khai."
            });

        return Ok(new { success = true, data });
    }
}
