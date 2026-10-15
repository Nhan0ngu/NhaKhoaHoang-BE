using System;
using System.Collections.Generic;

namespace NhaKhoaHoang.API.Models;

public partial class MediaFile
{
    public long MediaId { get; set; }

    public string FileName { get; set; } = null!;

    public string StorageProvider { get; set; } = null!;

    public string StorageKey { get; set; } = null!;

    public string? PublicUrl { get; set; }

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AdminUser? UploadedByNavigation { get; set; }
}
