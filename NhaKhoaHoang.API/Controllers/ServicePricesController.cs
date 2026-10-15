
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class ServicePricesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ServicePricesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/prices?serviceId=1
    [AllowAnonymous]
    [HttpGet("prices")]
    public async Task<IActionResult> GetPublic(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        var query = _db.ServicePrices
            .AsNoTracking()
            .Where(p =>
                p.IsPublished &&
                !p.IsDemo &&
                p.Service.IsPublished &&
                p.Service.Category.IsActive);

        if (serviceId.HasValue)
            query = query.Where(p => p.ServiceId == serviceId.Value);

        var data = await query
            .OrderBy(p => p.Service.DisplayOrder)
            .ThenBy(p => p.DisplayOrder)
            .ThenBy(p => p.PriceId)
            .Select(p => new
            {
                p.PriceId,
                p.ServiceId,
                ServiceName = p.Service.Name,
                p.PriceCode,
                p.PriceName,
                p.Amount,
                p.Unit,
                p.Note,
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

    // GET /api/admin/prices
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/prices")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        var query = _db.ServicePrices.AsNoTracking();

        if (serviceId.HasValue)
            query = query.Where(p => p.ServiceId == serviceId.Value);

        var data = await query
            .OrderBy(p => p.ServiceId)
            .ThenBy(p => p.DisplayOrder)
            .ThenBy(p => p.PriceId)
            .Select(p => new
            {
                p.PriceId,
                p.ServiceId,
                ServiceName = p.Service.Name,
                p.PriceCode,
                p.PriceName,
                p.Amount,
                p.Unit,
                p.Note,
                p.IsDemo,
                p.IsPublished,
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

    // POST /api/admin/prices
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/prices")]
    public async Task<IActionResult> Create(
        [FromBody] ServicePriceRequest request,
        CancellationToken ct)
    {
        if (!await _db.Services.AnyAsync(
            s => s.ServiceId == request.ServiceId, ct))
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ không tồn tại."
            });

        if (string.IsNullOrWhiteSpace(request.PriceName))
            return BadRequest(new
            {
                success = false,
                message = "Tên mức giá không hợp lệ."
            });

        if (request.IsDemo && request.IsPublished)
            return BadRequest(new
            {
                success = false,
                message = "Giá demo không thể được xuất bản."
            });

        var price = new ServicePrice
        {
            ServiceId = request.ServiceId,
            PriceCode = request.PriceCode?.Trim(),
            PriceName = request.PriceName.Trim(),
            Amount = request.Amount,
            Unit = request.Unit?.Trim(),
            Note = request.Note?.Trim(),
            IsDemo = request.IsDemo,
            IsPublished = request.IsPublished,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServicePrices.Add(price);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm mức giá.",
            data = new
            {
                price.PriceId,
                price.ServiceId,
                price.PriceName,
                price.Amount,
                price.Unit,
                price.IsPublished
            }
        });
    }

    // PUT /api/admin/prices/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/prices/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ServicePriceRequest request,
        CancellationToken ct)
    {
        var price = await _db.ServicePrices
            .FirstOrDefaultAsync(p => p.PriceId == id, ct);

        if (price == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy mức giá."
            });

        if (!await _db.Services.AnyAsync(
            s => s.ServiceId == request.ServiceId, ct))
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ không tồn tại."
            });

        if (string.IsNullOrWhiteSpace(request.PriceName))
            return BadRequest(new
            {
                success = false,
                message = "Tên mức giá không hợp lệ."
            });

        if (request.IsDemo && request.IsPublished)
            return BadRequest(new
            {
                success = false,
                message = "Giá demo không thể được xuất bản."
            });

        price.ServiceId = request.ServiceId;
        price.PriceCode = request.PriceCode?.Trim();
        price.PriceName = request.PriceName.Trim();
        price.Amount = request.Amount;
        price.Unit = request.Unit?.Trim();
        price.Note = request.Note?.Trim();
        price.IsDemo = request.IsDemo;
        price.IsPublished = request.IsPublished;
        price.DisplayOrder = request.DisplayOrder;
        price.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật mức giá."
        });
    }

    // DELETE /api/admin/prices/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/prices/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var price = await _db.ServicePrices
            .FirstOrDefaultAsync(p => p.PriceId == id, ct);

        if (price == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy mức giá."
            });

        _db.ServicePrices.Remove(price);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa mức giá."
        });
    }
}
