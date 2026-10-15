using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ConsultationRequest
{
    public long ConsultationId { get; set; }

    public string FullName { get; set; } = null!;

    public byte Age { get; set; }

    public string Gender { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public int ServiceId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ContactedAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Service Service { get; set; } = null!;
}
