using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
