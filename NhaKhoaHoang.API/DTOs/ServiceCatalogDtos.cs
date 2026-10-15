
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

// =====================================
// SERVICE CATEGORIES
// =====================================

public class ServiceCategoryRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

// =====================================
// SERVICE PRICES
// =====================================

public class ServicePriceRequest
{
    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }

    [StringLength(30)]
    public string? PriceCode { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string PriceName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public bool IsDemo { get; set; }

    public bool IsPublished { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

// =====================================
// SERVICE FAQs
// =====================================

public class ServiceFaqRequest
{
    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }
}
