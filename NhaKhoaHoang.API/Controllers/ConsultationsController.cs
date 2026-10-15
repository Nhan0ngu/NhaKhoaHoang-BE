
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api/consultations")]
public class ConsultationsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ConsultationsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // POST /api/consultations
    [AllowAnonymous]
    [EnableRateLimiting("consultations")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ConsultationCreateRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var phone = request.Phone.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new
            {
                success = false,
                message = "Họ tên không hợp lệ."
            });
        }

        var serviceExists = await _db.Services
            .AsNoTracking()
            .AnyAsync(
                s => s.ServiceId == request.ServiceId
                     && s.IsPublished
                     && s.Category.IsActive,
                cancellationToken);

        if (!serviceExists)
        {
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ đã chọn không khả dụng."
            });
        }

        var consultation = new ConsultationRequest
        {
            FullName = fullName,
            Age = checked((byte)request.Age),
            Gender = request.Gender,
            Phone = phone,
            ServiceId = request.ServiceId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.ConsultationRequests.Add(consultation);

        await _db.SaveChangesAsync(cancellationToken);

        // Không trả lại số điện thoại và dữ liệu cá nhân.
        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Yêu cầu tư vấn đã được ghi nhận.",
            data = new
            {
                consultationId = consultation.ConsultationId,
                status = consultation.Status
            }
        });
    }
}
