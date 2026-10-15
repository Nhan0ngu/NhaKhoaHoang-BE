using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class ServiceFaq
{
    public int Faqid { get; set; }

    public int ServiceId { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public virtual Service Service { get; set; } = null!;
}
