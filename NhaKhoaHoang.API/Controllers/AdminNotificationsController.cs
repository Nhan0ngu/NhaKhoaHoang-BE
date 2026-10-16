
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/notifications")]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public AdminNotificationsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // ==========================================
    // GET /api/admin/notifications
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Phiên đăng nhập không hợp lệ."
            });
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Tham số phân trang không hợp lệ."
            });
        }

        var query = _db.AdminNotifications
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var unreadCount = await query.CountAsync(
            notification =>
                !notification.Reads.Any(
                    read => read.AdminId == adminId),
            cancellationToken);

        var data = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.NotificationId,
                n.ConsultationId,
                n.Type,
                n.CreatedAt,

                FullName = n.Consultation.FullName,
                ServiceId = n.Consultation.ServiceId,
                ServiceName =
                    n.Consultation.Service.Name,

                ConsultationStatus =
                    n.Consultation.Status,

                IsRead = n.Reads.Any(
                    read => read.AdminId == adminId),

                ReadAt = n.Reads
                    .Where(read => read.AdminId == adminId)
                    .Select(read => (DateTime?)read.ReadAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            total,
            unreadCount,
            page,
            pageSize,
            data
        });
    }

    // ==========================================
    // GET /api/admin/notifications/unread-count
    // ==========================================

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Phiên đăng nhập không hợp lệ."
            });
        }

        var unreadCount = await _db.AdminNotifications
            .AsNoTracking()
            .CountAsync(
                notification =>
                    !notification.Reads.Any(
                        read => read.AdminId == adminId),
                cancellationToken);

        return Ok(new
        {
            success = true,
            unreadCount
        });
    }

    // ==========================================
    // PUT /api/admin/notifications/{id}/read
    // ==========================================

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(
        long id,
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Phiên đăng nhập không hợp lệ."
            });
        }

        var exists = await _db.AdminNotifications
            .AsNoTracking()
            .AnyAsync(
                notification =>
                    notification.NotificationId == id,
                cancellationToken);

        if (!exists)
        {
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy thông báo."
            });
        }

        // PostgreSQL ON CONFLICT đảm bảo gọi lại API
        // hoặc hai request đồng thời không tạo
        // trùng bản ghi đã đọc.

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "AdminNotificationReads"
                ("NotificationId", "AdminId", "ReadAt")
            VALUES
                ({id}, {adminId},
                 (NOW() AT TIME ZONE 'UTC'))
            ON CONFLICT ("NotificationId", "AdminId")
            DO NOTHING
            """,
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Đã đánh dấu thông báo là đã đọc.",
            notificationId = id,
            isRead = true
        });
    }

    // ==========================================
    // AUTHENTICATED ADMIN IDENTIFIER
    // ==========================================

    private bool TryGetAdminId(out int adminId)
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out adminId)
            && adminId > 0;
    }
}
