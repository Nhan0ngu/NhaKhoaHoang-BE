
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

public class ArticleCategoryRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug không hợp lệ.")]
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class ArticleRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(320)]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Summary { get; set; }

    public string? Content { get; set; }

    [StringLength(1000)]
    public string? CoverImageUrl { get; set; }

    [Required]
    [RegularExpression("^(draft|published|archived)$")]
    public string Status { get; set; } = "draft";
}

public class SitePageRequest
{
    [Required]
    [StringLength(250, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1500)]
    public string? Summary { get; set; }

    public string? Content { get; set; }

    [StringLength(1000)]
    public string? BannerUrl { get; set; }

    public bool IsPublished { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
