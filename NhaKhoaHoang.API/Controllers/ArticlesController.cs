
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
public class ArticlesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ArticlesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/articles?page=1&pageSize=12
    [AllowAnonymous]
    [HttpGet("articles")]
    public async Task<IActionResult> GetPublic(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new
            {
                success = false,
                message = "Phân trang không hợp lệ."
            });

        var query = _db.Articles
            .AsNoTracking()
            .Where(a =>
                a.Status == "published" &&
                a.Category.IsActive &&
                a.PublishedAt != null &&
                a.PublishedAt <= DateTime.UtcNow);

        var total = await query.CountAsync(ct);

        var data = await query
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.ArticleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.ArticleId,
                a.CategoryId,
                CategoryName = a.Category.Name,
                a.Title,
                a.Slug,
                a.Summary,
                a.CoverImageUrl,
                a.PublishedAt,
                AuthorName = a.Author.FullName
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

    // GET /api/articles/by-slug/{slug}
    [AllowAnonymous]
    [HttpGet("articles/by-slug/{slug}")]
    public async Task<IActionResult> GetPublicBySlug(
        string slug,
        CancellationToken ct)
    {
        var data = await _db.Articles
            .AsNoTracking()
            .Where(a =>
                a.Slug == slug &&
                a.Status == "published" &&
                a.Category.IsActive &&
                a.PublishedAt != null &&
                a.PublishedAt <= DateTime.UtcNow)
            .Select(a => new
            {
                a.ArticleId,
                a.CategoryId,
                CategoryName = a.Category.Name,
                a.Title,
                a.Slug,
                a.Summary,
                a.Content,
                a.CoverImageUrl,
                a.PublishedAt,
                AuthorName = a.Author.FullName
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bài viết công khai."
            });

        return Ok(new { success = true, data });
    }

    // GET /api/admin/articles
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/articles")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] string? status,
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

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "draft" &&
            status != "published" &&
            status != "archived")
            return BadRequest(new
            {
                success = false,
                message = "Trạng thái không hợp lệ."
            });

        var query = _db.Articles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync(ct);

        var data = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.ArticleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.ArticleId,
                a.CategoryId,
                CategoryName = a.Category.Name,
                a.AuthorId,
                AuthorName = a.Author.FullName,
                a.Title,
                a.Slug,
                a.Summary,
                a.CoverImageUrl,
                a.Status,
                a.PublishedAt,
                a.CreatedAt,
                a.UpdatedAt
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

    // GET /api/admin/articles/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/articles/{id:int}")]
    public async Task<IActionResult> GetAdminById(
        int id,
        CancellationToken ct)
    {
        var data = await _db.Articles
            .AsNoTracking()
            .Where(a => a.ArticleId == id)
            .Select(a => new
            {
                a.ArticleId,
                a.CategoryId,
                a.AuthorId,
                a.Title,
                a.Slug,
                a.Summary,
                a.Content,
                a.CoverImageUrl,
                a.Status,
                a.PublishedAt,
                a.CreatedAt,
                a.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bài viết."
            });

        return Ok(new { success = true, data });
    }

    // POST /api/admin/articles
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/articles")]
    public async Task<IActionResult> Create(
        [FromBody] ArticleRequest request,
        CancellationToken ct)
    {
        if (!TryGetAdminId(out int adminId))
            return Unauthorized();

        var title = request.Title.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new
            {
                success = false,
                message = "Tiêu đề không hợp lệ."
            });

        var categoryExists = await _db.ArticleCategories
            .AnyAsync(c =>
                c.CategoryId == request.CategoryId,
                ct);

        if (!categoryExists)
            return BadRequest(new
            {
                success = false,
                message = "Danh mục không tồn tại."
            });

        if (await _db.Articles.AnyAsync(a => a.Slug == slug, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug bài viết đã tồn tại."
            });

        var article = new Article
        {
            CategoryId = request.CategoryId,
            AuthorId = adminId,
            Title = title,
            Slug = slug,
            Summary = request.Summary?.Trim(),
            Content = request.Content,
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            Status = request.Status,
            PublishedAt = request.Status == "published"
                ? DateTime.UtcNow
                : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã tạo bài viết.",
            data = new
            {
                article.ArticleId,
                article.Title,
                article.Slug,
                article.Status,
                article.PublishedAt
            }
        });
    }

    // PUT /api/admin/articles/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/articles/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ArticleRequest request,
        CancellationToken ct)
    {
        var article = await _db.Articles
            .FirstOrDefaultAsync(a => a.ArticleId == id, ct);

        if (article == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bài viết."
            });

        var title = request.Title.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new
            {
                success = false,
                message = "Tiêu đề không hợp lệ."
            });

        if (!await _db.ArticleCategories.AnyAsync(
            c => c.CategoryId == request.CategoryId, ct))
            return BadRequest(new
            {
                success = false,
                message = "Danh mục không tồn tại."
            });

        if (await _db.Articles.AnyAsync(
            a => a.Slug == slug && a.ArticleId != id, ct))
            return Conflict(new
            {
                success = false,
                message = "Slug bài viết đã tồn tại."
            });

        var wasPublished = article.Status == "published";

        article.CategoryId = request.CategoryId;
        article.Title = title;
        article.Slug = slug;
        article.Summary = request.Summary?.Trim();
        article.Content = request.Content;
        article.CoverImageUrl = request.CoverImageUrl?.Trim();
        article.Status = request.Status;
        article.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "published")
        {
            if (!wasPublished || article.PublishedAt == null)
                article.PublishedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật bài viết.",
            data = new
            {
                article.ArticleId,
                article.Status,
                article.PublishedAt
            }
        });
    }

    // DELETE /api/admin/articles/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/articles/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var article = await _db.Articles
            .FirstOrDefaultAsync(a => a.ArticleId == id, ct);

        if (article == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bài viết."
            });

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa bài viết."
        });
    }

    private bool TryGetAdminId(out int adminId)
    {
        var claim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(claim, out adminId);
    }
}
