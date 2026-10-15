using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public string? Content { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? IconClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<ChatFaq> ChatFaqs { get; set; } = new List<ChatFaq>();

    public virtual ICollection<ConsultationRequest> ConsultationRequests { get; set; } = new List<ConsultationRequest>();

    public virtual ICollection<ServiceFaq> ServiceFaqs { get; set; } = new List<ServiceFaq>();

    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
}
