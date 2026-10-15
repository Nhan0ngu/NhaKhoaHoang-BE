
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ServiceCategoriesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/service-categories
    [AllowAnonymous]
    [HttpGet("service-categories")]
    public async Task<IActionResult> GetPublic(CancellationToken ct)
    {
        var data = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.CategoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.Slug,
                c.Description,
                c.DisplayOrder
            })
            .ToListAsync(ct);

        return Ok(new { success = true, count = data.Count, data });
    }

    // GET /api/admin/service-categories
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/service-categories")]
    public async Task<IActionResult> GetAdmin(CancellationToken ct)
    {
        var data = await _db.ServiceCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.Slug,
                c.Description,
                c.DisplayOrder,
                c.IsActive
            })
            .ToListAsync(ct);

        return Ok(new { success = true, count = data.Count, data });
    }

    // POST /api/admin/service-categories
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/service-categories")]
    public async Task<IActionResult> Create(
        [FromBody] ServiceCategoryRequest request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new
            {
                success = false,
                message = "Tên danh mục không hợp lệ."
            });

        if (await _db.ServiceCategories
            .AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug danh mục đã tồn tại."
            });

        var category = new ServiceCategory
        {
            Name = name,
            Slug = slug,
            Description = request.Description?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServiceCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm danh mục.",
            data = new
            {
                category.CategoryId,
                category.Name,
                category.Slug,
                category.IsActive
            }
        });
    }

    // PUT /api/admin/service-categories/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/service-categories/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ServiceCategoryRequest request,
        CancellationToken ct)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id, ct);

        if (category == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy danh mục."
            });

        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new
            {
                success = false,
                message = "Tên danh mục không hợp lệ."
            });

        if (await _db.ServiceCategories.AnyAsync(
            c => c.Slug == slug && c.CategoryId != id, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug danh mục đã tồn tại."
            });

        category.Name = name;
        category.Slug = slug;
        category.Description = request.Description?.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật danh mục."
        });
    }

    // DELETE /api/admin/service-categories/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/service-categories/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id, ct);

        if (category == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy danh mục."
            });

        var inUse = await _db.Services
            .AnyAsync(s => s.CategoryId == id, ct);

        if (inUse)
            return Conflict(new
            {
                success = false,
                message = "Danh mục đang có dịch vụ. Hãy ẩn danh mục thay vì xóa."
            });

        _db.ServiceCategories.Remove(category);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa danh mục."
        });
    }
}
