
using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public class AdminNotification
{
    public long NotificationId { get; set; }

    public long ConsultationId { get; set; }

    public string Type { get; set; } = "new_consultation";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConsultationRequest Consultation { get; set; } = null!;

    public ICollection<AdminNotificationRead> Reads { get; set; }
        = new List<AdminNotificationRead>();
}
