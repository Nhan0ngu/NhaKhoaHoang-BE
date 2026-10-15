
namespace NhaKhoaHoang.API.Configuration;

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "website-images";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
