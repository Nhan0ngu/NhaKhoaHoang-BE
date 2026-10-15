using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ArticleCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
