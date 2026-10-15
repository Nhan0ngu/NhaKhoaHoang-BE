
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

public class DoctorRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Specialty { get; set; }

    public string? Biography { get; set; }

    [StringLength(1000)]
    public string? PhotoUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }
}

public class BranchRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? OpeningHours { get; set; }

    [StringLength(1000)]
    public string? GoogleMapsUrl { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsPublished { get; set; }
}

public class SiteSettingUpdateRequest
{
    // Có thể là null để xóa giá trị một thiết lập.
    public string? SettingValue { get; set; }
}
