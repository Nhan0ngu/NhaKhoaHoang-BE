using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class SitePage
{
    public int PageId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? BannerUrl { get; set; }

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
