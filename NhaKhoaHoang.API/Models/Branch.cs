using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class Branch
{
    public int BranchId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? OpeningHours { get; set; }

    public string? GoogleMapsUrl { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
