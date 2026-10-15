
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class ArticleCategoriesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ArticleCategoriesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/article-categories
    [AllowAnonymous]
    [HttpGet("article-categories")]
    public async Task<IActionResult> GetPublic(
        CancellationToken ct)
    {
        var data = await _db.ArticleCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.Slug
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/article-categories
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/article-categories")]
    public async Task<IActionResult> GetAdmin(
        CancellationToken ct)
    {
        var data = await _db.ArticleCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.Name,
                c.Slug,
                c.IsActive
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // POST /api/admin/article-categories
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/article-categories")]
    public async Task<IActionResult> Create(
        [FromBody] ArticleCategoryRequest request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new
            {
                success = false,
                message = "Tên danh mục không hợp lệ."
            });

        if (await _db.ArticleCategories
            .AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug danh mục đã tồn tại."
            });

        var category = new ArticleCategory
        {
            Name = name,
            Slug = slug,
            IsActive = request.IsActive
        };

        _db.ArticleCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm danh mục bài viết.",
            data = new
            {
                category.CategoryId,
                category.Name,
                category.Slug,
                category.IsActive
            }
        });
    }

    // PUT /api/admin/article-categories/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/article-categories/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ArticleCategoryRequest request,
        CancellationToken ct)
    {
        var category = await _db.ArticleCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id, ct);

        if (category == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy danh mục."
            });

        var name = request.Name.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new
            {
                success = false,
                message = "Tên danh mục không hợp lệ."
            });

        if (await _db.ArticleCategories.AnyAsync(
            c => c.Slug == slug && c.CategoryId != id, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug danh mục đã tồn tại."
            });

        category.Name = name;
        category.Slug = slug;
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật danh mục."
        });
    }

    // DELETE /api/admin/article-categories/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/article-categories/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var category = await _db.ArticleCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id, ct);

        if (category == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy danh mục."
            });

        var inUse = await _db.Articles
            .AnyAsync(a => a.CategoryId == id, ct);

        if (inUse)
            return Conflict(new
            {
                success = false,
                message =
                    "Danh mục đang có bài viết. " +
                    "Hãy ẩn danh mục thay vì xóa."
            });

        _db.ArticleCategories.Remove(category);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa danh mục."
        });
    }
}
