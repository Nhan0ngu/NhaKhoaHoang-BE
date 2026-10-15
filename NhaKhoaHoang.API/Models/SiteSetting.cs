using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class SiteSetting
{
    public int SettingId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }

    public string? Description { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AdminUser? UpdatedByNavigation { get; set; }
}
