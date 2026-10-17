using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Services;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly OpenAiChatService _chat;

    public ChatController(OpenAiChatService chat)
    {
        _chat = chat;
    }

    // GET /api/chat/status — Không tiết lộ API key.
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { success = true, enabled = _chat.IsConfigured });
    }

    // POST /api/chat — Khách không cần tài khoản.
    [HttpPost]
    [EnableRateLimiting("chat")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Send(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
        {
            return BadRequest(new
            {
                success = false,
                message = "Câu hỏi phải có từ 1 đến 1.000 ký tự."
            });
        }

        if (request.History is { Count: > 12 })
        {
            return BadRequest(new
            {
                success = false,
                message = "Hội thoại quá dài. Vui lòng bắt đầu cuộc trò chuyện mới."
            });
        }

        if (!_chat.IsConfigured)
        {
            return StatusCode(503, new
            {
                success = false,
                message = "Trợ lý AI chưa được kích hoạt. Phòng khám đang cập nhật hệ thống."
            });
        }

        try
        {
            var reply = await _chat.GetReplyAsync(
                message, request.History ?? new(), ct);

            return Ok(new { success = true, reply });
        }
        catch (OpenAiChatUnavailableException)
        {
            return StatusCode(503, new
            {
                success = false,
                message = "Trợ lý AI đang bận. Vui lòng thử lại sau hoặc đăng ký tư vấn."
            });
        }
    }
}