using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ChatFaq
{
    public int ChatFaqid { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public string? Keywords { get; set; }

    public int? ServiceId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Service? Service { get; set; }
}
