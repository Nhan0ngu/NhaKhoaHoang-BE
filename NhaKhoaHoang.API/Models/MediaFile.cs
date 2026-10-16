
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

    // Nội dung ảnh được lưu trực tiếp trong Neon PostgreSQL.
    // Ánh xạ đến cột "ImageData" kiểu BYTEA.
    // Nullable để tương thích với các bản ghi cũ.
    public byte[]? ImageData { get; set; }

    public virtual AdminUser? UploadedByNavigation { get; set; }
}
