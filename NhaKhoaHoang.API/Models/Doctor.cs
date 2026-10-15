using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Specialty { get; set; }

    public string? Biography { get; set; }

    public string? PhotoUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
