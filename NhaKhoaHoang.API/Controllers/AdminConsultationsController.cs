
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/consultations")]
public class AdminConsultationsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public AdminConsultationsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/admin/consultations
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Tham số phân trang không hợp lệ."
            });
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "pending" &&
            status != "contacted" &&
            status != "closed")
        {
            return BadRequest(new
            {
                success = false,
                message = "Trạng thái không hợp lệ."
            });
        }

        var query = _db.ConsultationRequests
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.ConsultationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConsultationResponse
            {
                ConsultationId = c.ConsultationId,
                FullName = c.FullName,
                Age = c.Age,
                Gender = c.Gender,
                Phone = c.Phone,
                ServiceId = c.ServiceId,
                ServiceName = c.Service.Name,
                Status = c.Status,
                Note = c.Note,
                CreatedAt = c.CreatedAt,
                ContactedAt = c.ContactedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            data
        });
    }

    // GET /api/admin/consultations/{id}
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var data = await _db.ConsultationRequests
            .AsNoTracking()
            .Where(c => c.ConsultationId == id)
            .Select(c => new ConsultationResponse
            {
                ConsultationId = c.ConsultationId,
                FullName = c.FullName,
                Age = c.Age,
                Gender = c.Gender,
                Phone = c.Phone,
                ServiceId = c.ServiceId,
                ServiceName = c.Service.Name,
                Status = c.Status,
                Note = c.Note,
                CreatedAt = c.CreatedAt,
                ContactedAt = c.ContactedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy yêu cầu tư vấn."
            });
        }

        return Ok(new { success = true, data });
    }

    // PUT /api/admin/consultations/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] ConsultationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var consultation = await _db.ConsultationRequests
            .FirstOrDefaultAsync(
                c => c.ConsultationId == id,
                cancellationToken);

        if (consultation == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy yêu cầu tư vấn."
            });
        }

        var oldStatus = consultation.Status;

        consultation.Status = request.Status;
        consultation.Note = request.Note?.Trim();
        consultation.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "contacted" &&
            consultation.ContactedAt == null)
        {
            consultation.ContactedAt = DateTime.UtcNow;
        }

        // Chỉ xóa ContactedAt khi chuyển ngược về pending.
        if (request.Status == "pending" &&
            oldStatus != "pending")
        {
            consultation.ContactedAt = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật yêu cầu tư vấn.",
            data = new
            {
                consultationId = consultation.ConsultationId,
                status = consultation.Status,
                contactedAt = consultation.ContactedAt,
                note = consultation.Note
            }
        });
    }
}
