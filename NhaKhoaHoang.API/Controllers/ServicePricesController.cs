
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

    // ==========================================
    // GET /api/prices?serviceId=1
    // BẢNG GIÁ CÔNG KHAI
    // ==========================================

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
        {
            query = query.Where(
                p => p.ServiceId == serviceId.Value
            );
        }

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

                // Thông tin giá mới.
                p.PriceType,
                p.Amount,
                p.MinAmount,
                p.MaxAmount,

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

    // ==========================================
    // GET /api/admin/prices
    // BẢNG GIÁ TRONG ADMIN
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/prices")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        var query = _db.ServicePrices
            .AsNoTracking();

        if (serviceId.HasValue)
        {
            query = query.Where(
                p => p.ServiceId == serviceId.Value
            );
        }

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

                // Thông tin giá mới.
                p.PriceType,
                p.Amount,
                p.MinAmount,
                p.MaxAmount,

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

    // ==========================================
    // POST /api/admin/prices
    // THÊM HẠNG MỤC & BẢNG GIÁ
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/prices")]
    public async Task<IActionResult> Create(
        [FromBody] ServicePriceRequest request,
        CancellationToken ct)
    {
        var serviceExists = await _db.Services
            .AnyAsync(
                s => s.ServiceId == request.ServiceId,
                ct
            );

        if (!serviceExists)
        {
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ chính không tồn tại."
            });
        }

        if (string.IsNullOrWhiteSpace(request.PriceName))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tên hạng mục điều trị không hợp lệ."
            });
        }

        if (request.IsDemo && request.IsPublished)
        {
            return BadRequest(new
            {
                success = false,
                message = "Giá demo không thể được xuất bản."
            });
        }

        // IValidatableObject trong DTO đã kiểm tra
        // tính hợp lệ của ba loại giá.
        // Tại đây chuẩn hóa giá trị trước khi lưu.
        var priceType = request.PriceType;

        var price = new ServicePrice
        {
            ServiceId = request.ServiceId,

            PriceCode = request.PriceCode?.Trim(),

            PriceName = request.PriceName.Trim(),

            PriceType = priceType,

            Amount = priceType == "fixed"
                ? request.Amount
                : null,

            MinAmount = priceType == "range"
                ? request.MinAmount
                : null,

            MaxAmount = priceType == "range"
                ? request.MaxAmount
                : null,

            Unit = request.Unit?.Trim(),

            Note = request.Note?.Trim(),

            IsDemo = request.IsDemo,

            IsPublished = request.IsPublished,

            DisplayOrder = request.DisplayOrder,

            CreatedAt = DateTime.UtcNow
        };

        _db.ServicePrices.Add(price);

        await _db.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                success = true,
                message = "Đã thêm hạng mục bảng giá.",
                data = new
                {
                    price.PriceId,
                    price.ServiceId,
                    price.PriceName,

                    price.PriceType,
                    price.Amount,
                    price.MinAmount,
                    price.MaxAmount,

                    price.Unit,
                    price.Note,
                    price.IsPublished
                }
            });
    }

    // ==========================================
    // PUT /api/admin/prices/{id}
    // CẬP NHẬT HẠNG MỤC & BẢNG GIÁ
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPut("admin/prices/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ServicePriceRequest request,
        CancellationToken ct)
    {
        var price = await _db.ServicePrices
            .FirstOrDefaultAsync(
                p => p.PriceId == id,
                ct
            );

        if (price == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy hạng mục bảng giá."
            });
        }

        var serviceExists = await _db.Services
            .AnyAsync(
                s => s.ServiceId == request.ServiceId,
                ct
            );

        if (!serviceExists)
        {
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ chính không tồn tại."
            });
        }

        if (string.IsNullOrWhiteSpace(request.PriceName))
        {
            return BadRequest(new
            {
                success = false,
                message = "Tên hạng mục điều trị không hợp lệ."
            });
        }

        if (request.IsDemo && request.IsPublished)
        {
            return BadRequest(new
            {
                success = false,
                message = "Giá demo không thể được xuất bản."
            });
        }

        var priceType = request.PriceType;

        price.ServiceId = request.ServiceId;

        price.PriceCode = request.PriceCode?.Trim();

        price.PriceName = request.PriceName.Trim();

        price.PriceType = priceType;

        // Chỉ lưu Amount cho giá cố định.
        price.Amount = priceType == "fixed"
            ? request.Amount
            : null;

        // Chỉ lưu MinAmount / MaxAmount cho giá khoảng.
        price.MinAmount = priceType == "range"
            ? request.MinAmount
            : null;

        price.MaxAmount = priceType == "range"
            ? request.MaxAmount
            : null;

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
            message = "Đã cập nhật hạng mục bảng giá.",
            data = new
            {
                price.PriceId,
                price.ServiceId,
                price.PriceName,

                price.PriceType,
                price.Amount,
                price.MinAmount,
                price.MaxAmount,

                price.Unit,
                price.Note,
                price.IsPublished
            }
        });
    }

    // ==========================================
    // DELETE /api/admin/prices/{id}
    // XÓA HẠNG MỤC BẢNG GIÁ
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/prices/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var price = await _db.ServicePrices
            .FirstOrDefaultAsync(
                p => p.PriceId == id,
                ct
            );

        if (price == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy hạng mục bảng giá."
            });
        }

        _db.ServicePrices.Remove(price);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa hạng mục bảng giá."
        });
    }
}
