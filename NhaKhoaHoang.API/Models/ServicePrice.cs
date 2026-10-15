using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ServicePrice
{
    public int PriceId { get; set; }

    public int ServiceId { get; set; }

    public string? PriceCode { get; set; }

    public string PriceName { get; set; } = null!;

    public decimal? Amount { get; set; }

    public string? Unit { get; set; }

    public string? Note { get; set; }

    public bool IsDemo { get; set; }

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Service Service { get; set; } = null!;
}
