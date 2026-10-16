
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
        // ==========================================
        // 1. VALIDATE INPUT
        // ==========================================

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

        // ==========================================
        // 2. CHECK SERVICE
        // ==========================================

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

        // ==========================================
        // 3. PREPARE CONSULTATION
        // ==========================================

        var now = DateTime.UtcNow;

        var consultation = new ConsultationRequest
        {
            FullName = fullName,
            Age = checked((byte)request.Age),
            Gender = request.Gender,
            Phone = phone,
            ServiceId = request.ServiceId,
            Status = "pending",
            CreatedAt = now
        };

        // ==========================================
        // 4. TRANSACTION
        // ==========================================
        // Consultation và Notification phải được
        // lưu thành công cùng nhau.
        //
        // Nếu một thao tác gặp lỗi, transaction
        // không được commit.

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        // ==========================================
        // 5. SAVE CONSULTATION
        // ==========================================

        _db.ConsultationRequests.Add(consultation);

        await _db.SaveChangesAsync(
            cancellationToken);

        // Sau SaveChanges, EF Core nhận được ID
        // từ cột IDENTITY của PostgreSQL.

        // ==========================================
        // 6. CREATE ADMIN NOTIFICATION
        // ==========================================

        var notification = new AdminNotification
        {
            ConsultationId = consultation.ConsultationId,
            Type = "new_consultation",
            CreatedAt = now
        };

        _db.AdminNotifications.Add(notification);

        await _db.SaveChangesAsync(
            cancellationToken);

        // ==========================================
        // 7. COMMIT TRANSACTION
        // ==========================================

        await transaction.CommitAsync(
            cancellationToken);

        // ==========================================
        // 8. RESPONSE
        // ==========================================
        // Không trả số điện thoại hoặc thông tin
        // cá nhân của khách hàng.
        //
        // Giữ nguyên cấu trúc response cũ
        // để Frontend không bị ảnh hưởng.

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                success = true,
                message =
                    "Yêu cầu tư vấn đã được ghi nhận.",
                data = new
                {
                    consultationId =
                        consultation.ConsultationId,
                    status = consultation.Status
                }
            });
    }
}
