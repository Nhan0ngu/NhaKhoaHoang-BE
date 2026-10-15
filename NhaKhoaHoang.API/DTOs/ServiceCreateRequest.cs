
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

public class ServiceCreateRequest
{
    [Required(ErrorMessage = "Vui lòng chọn danh mục dịch vụ.")]
    [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug dịch vụ.")]
    [StringLength(220)]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ được chứa chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ShortDescription { get; set; }

    public string? Content { get; set; }

    [StringLength(1000)]
    public string? ThumbnailUrl { get; set; }

    [StringLength(100)]
    public string? IconClass { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; } = 0;

    public bool IsPublished { get; set; } = false;
}
