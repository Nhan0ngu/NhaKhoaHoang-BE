
namespace NhaKhoaHoang.API.DTOs;

public class ServiceResponse
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Content { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? IconClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }
}
