using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs;

public sealed class ChatRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    // Hội thoại ngắn do trình duyệt gửi lên. Không lưu vào Neon.
    public List<ChatHistoryMessage> History { get; set; } = new();
}

public sealed class ChatHistoryMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}