
using System.Collections.Generic;
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

public class ServicePriceRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ServiceId { get; set; }

    [StringLength(30)]
    public string? PriceCode { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string PriceName { get; set; } = string.Empty;

    // fixed        = Giá cố định
    // range        = Giá từ - đến
    // consultation = Tùy trường hợp
    [Required]
    [RegularExpression(
        @"^(fixed|range|consultation)$",
        ErrorMessage = "Loại giá không hợp lệ."
    )]
    public string PriceType { get; set; } = "fixed";

    // Giá cố định.
    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? Amount { get; set; }

    // Giá thấp nhất và cao nhất.
    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? MinAmount { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? MaxAmount { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public bool IsDemo { get; set; }

    public bool IsPublished { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    // =====================================
    // KIỂM TRA LOGIC BA LOẠI GIÁ
    // =====================================

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (PriceType == "fixed")
        {
            // Giữ tương thích với API cũ: Amount null
            // vẫn được phép trong thời gian chuyển đổi.
            // Admin mới sẽ yêu cầu nhập số tiền.
            if (MinAmount.HasValue || MaxAmount.HasValue)
            {
                yield return new ValidationResult(
                    "Giá cố định không được có giá từ/đến.",
                    new[]
                    {
                        nameof(MinAmount),
                        nameof(MaxAmount)
                    }
                );
            }
        }
        else if (PriceType == "range")
        {
            if (!MinAmount.HasValue || !MaxAmount.HasValue)
            {
                yield return new ValidationResult(
                    "Giá khoảng phải có giá thấp nhất và cao nhất.",
                    new[]
                    {
                        nameof(MinAmount),
                        nameof(MaxAmount)
                    }
                );
            }
            else if (MinAmount.Value > MaxAmount.Value)
            {
                yield return new ValidationResult(
                    "Giá thấp nhất không được lớn hơn giá cao nhất.",
                    new[]
                    {
                        nameof(MinAmount),
                        nameof(MaxAmount)
                    }
                );
            }

            if (Amount.HasValue)
            {
                yield return new ValidationResult(
                    "Giá khoảng không được nhập giá cố định.",
                    new[] { nameof(Amount) }
                );
            }
        }
        else if (PriceType == "consultation")
        {
            if (
                Amount.HasValue ||
                MinAmount.HasValue ||
                MaxAmount.HasValue
            )
            {
                yield return new ValidationResult(
                    "Giá tùy trường hợp không được có số tiền cố định hoặc khoảng giá.",
                    new[]
                    {
                        nameof(Amount),
                        nameof(MinAmount),
                        nameof(MaxAmount)
                    }
                );
            }
        }
    }
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
