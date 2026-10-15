using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class Article
{
    public int ArticleId { get; set; }

    public int CategoryId { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? CoverImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AdminUser Author { get; set; } = null!;

    public virtual ArticleCategory Category { get; set; } = null!;
}
