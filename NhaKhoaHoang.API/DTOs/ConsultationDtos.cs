
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

public class ConsultationCreateRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Range(1, 120)]
    public int Age { get; set; }

    [Required]
    [RegularExpression("^(male|female|other)$")]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[0-9]{9}|\+84[0-9]{9})$")]
    public string Phone { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }
}

public class ConsultationUpdateRequest
{
    [Required]
    [RegularExpression("^(pending|contacted|closed)$")]
    public string Status { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Note { get; set; }
}

public class ConsultationResponse
{
    public long ConsultationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public byte Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
}
