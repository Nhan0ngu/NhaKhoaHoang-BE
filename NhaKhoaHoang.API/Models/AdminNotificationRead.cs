
using System;

namespace NhaKhoaHoang.API.Models;

public class AdminNotificationRead
{
    public long NotificationId { get; set; }

    public int AdminId { get; set; }

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public AdminNotification Notification { get; set; } = null!;

    public AdminUser Admin { get; set; } = null!;
}
