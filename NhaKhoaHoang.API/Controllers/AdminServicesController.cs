
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/services")]
public class AdminServicesController : ControllerBase
{
    private readonly NhaKhoaDbContext _context;

    public AdminServicesController(NhaKhoaDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // GET /api/admin/services
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _context.Services
            .AsNoTracking()
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
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = services.Count,
            data = services
        });
    }

    // ==========================================
    // GET /api/admin/services/{id}
    // ==========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Where(s => s.ServiceId == id)
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
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy dịch vụ."
            });
        }

        return Ok(new
        {
            success = true,
            data = service
        });
    }

    // ==========================================
    // POST /api/admin/services
    // ==========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ServiceCreateRequest request)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tên dịch vụ không được để trống."
            });
        }

        var categoryExists = await _context.ServiceCategories
            .AnyAsync(c =>
                c.CategoryId == request.CategoryId &&
                c.IsActive);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                success = false,
                message = "Danh mục dịch vụ không tồn tại hoặc đã bị vô hiệu hóa."
            });
        }

        var slugExists = await _context.Services
            .AnyAsync(s => s.Slug == slug);

        if (slugExists)
        {
            return Conflict(new
            {
                success = false,
                message = "Slug dịch vụ đã tồn tại."
            });
        }

        var service = new Service
        {
            CategoryId = request.CategoryId,
            Name = name,
            Slug = slug,
            ShortDescription = request.ShortDescription,
            Content = request.Content,
            ThumbnailUrl = request.ThumbnailUrl,
            IconClass = request.IconClass,
            DisplayOrder = request.DisplayOrder,
            IsPublished = request.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = service.ServiceId },
            new
            {
                success = true,
                message = "Thêm dịch vụ thành công.",
                data = ToResponse(service)
            });
    }

    // ==========================================
    // PUT /api/admin/services/{id}
    // ==========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ServiceUpdateRequest request)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceId == id);

        if (service == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy dịch vụ."
            });
        }

        var name = request.Name.Trim();
        var slug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tên dịch vụ không được để trống."
            });
        }

        var categoryExists = await _context.ServiceCategories
            .AnyAsync(c =>
                c.CategoryId == request.CategoryId &&
                c.IsActive);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                success = false,
                message = "Danh mục dịch vụ không tồn tại hoặc đã bị vô hiệu hóa."
            });
        }

        var slugExists = await _context.Services
            .AnyAsync(s =>
                s.Slug == slug &&
                s.ServiceId != id);

        if (slugExists)
        {
            return Conflict(new
            {
                success = false,
                message = "Slug dịch vụ đã được sử dụng."
            });
        }

        service.CategoryId = request.CategoryId;
        service.Name = name;
        service.Slug = slug;
        service.ShortDescription = request.ShortDescription;
        service.Content = request.Content;
        service.ThumbnailUrl = request.ThumbnailUrl;
        service.IconClass = request.IconClass;
        service.DisplayOrder = request.DisplayOrder;
        service.IsPublished = request.IsPublished;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật dịch vụ thành công.",
            data = ToResponse(service)
        });
    }

    // ==========================================
    // DELETE /api/admin/services/{id}
    // ==========================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.ServiceId == id);

        if (service == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy dịch vụ."
            });
        }

        var hasPrices = await _context.ServicePrices
            .AnyAsync(p => p.ServiceId == id);

        var hasFaqs = await _context.ServiceFaqs
            .AnyAsync(f => f.ServiceId == id);

        var hasConsultations = await _context.ConsultationRequests
            .AnyAsync(c => c.ServiceId == id);

        var hasChatFaqs = await _context.ChatFaqs
            .AnyAsync(f => f.ServiceId == id);

        if (hasPrices || hasFaqs ||
            hasConsultations || hasChatFaqs)
        {
            return Conflict(new
            {
                success = false,
                message = "Dịch vụ đang có dữ liệu liên kết. Hãy ẩn dịch vụ thay vì xóa."
            });
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Xóa dịch vụ thành công."
        });
    }

    // ==========================================
    // RESPONSE MAPPER
    // ==========================================

    private static ServiceResponse ToResponse(Service s)
    {
        return new ServiceResponse
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
        };
    }
}
