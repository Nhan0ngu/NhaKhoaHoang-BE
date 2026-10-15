
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class ServiceFaqsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public ServiceFaqsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/service-faqs?serviceId=1
    [AllowAnonymous]
    [HttpGet("service-faqs")]
    public async Task<IActionResult> GetPublic(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        var query = _db.ServiceFaqs
            .AsNoTracking()
            .Where(f =>
                f.IsPublished &&
                f.Service.IsPublished &&
                f.Service.Category.IsActive);

        if (serviceId.HasValue)
            query = query.Where(
                f => f.ServiceId == serviceId.Value);

        var data = await query
            .OrderBy(f => f.ServiceId)
            .ThenBy(f => f.DisplayOrder)
            .ThenBy(f => f.Faqid)
            .Select(f => new
            {
                FaqId = f.Faqid,
                f.ServiceId,
                f.Question,
                f.Answer,
                f.DisplayOrder
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/service-faqs
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/service-faqs")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] int? serviceId,
        CancellationToken ct)
    {
        var query = _db.ServiceFaqs.AsNoTracking();

        if (serviceId.HasValue)
            query = query.Where(
                f => f.ServiceId == serviceId.Value);

        var data = await query
            .OrderBy(f => f.ServiceId)
            .ThenBy(f => f.DisplayOrder)
            .ThenBy(f => f.Faqid)
            .Select(f => new
            {
                FaqId = f.Faqid,
                f.ServiceId,
                ServiceName = f.Service.Name,
                f.Question,
                f.Answer,
                f.DisplayOrder,
                f.IsPublished
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // POST /api/admin/service-faqs
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/service-faqs")]
    public async Task<IActionResult> Create(
        [FromBody] ServiceFaqRequest request,
        CancellationToken ct)
    {
        if (!await _db.Services.AnyAsync(
            s => s.ServiceId == request.ServiceId, ct))
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ không tồn tại."
            });

        if (string.IsNullOrWhiteSpace(request.Question) ||
            string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new
            {
                success = false,
                message = "Câu hỏi và câu trả lời không được để trống."
            });

        var faq = new ServiceFaq
        {
            ServiceId = request.ServiceId,
            Question = request.Question.Trim(),
            Answer = request.Answer.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsPublished = request.IsPublished
        };

        _db.ServiceFaqs.Add(faq);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm câu hỏi.",
            data = new
            {
                FaqId = faq.Faqid,
                faq.ServiceId,
                faq.Question,
                faq.IsPublished
            }
        });
    }

    // PUT /api/admin/service-faqs/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/service-faqs/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ServiceFaqRequest request,
        CancellationToken ct)
    {
        var faq = await _db.ServiceFaqs
            .FirstOrDefaultAsync(f => f.Faqid == id, ct);

        if (faq == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy câu hỏi."
            });

        if (!await _db.Services.AnyAsync(
            s => s.ServiceId == request.ServiceId, ct))
            return BadRequest(new
            {
                success = false,
                message = "Dịch vụ không tồn tại."
            });

        if (string.IsNullOrWhiteSpace(request.Question) ||
            string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new
            {
                success = false,
                message = "Câu hỏi và câu trả lời không được để trống."
            });

        faq.ServiceId = request.ServiceId;
        faq.Question = request.Question.Trim();
        faq.Answer = request.Answer.Trim();
        faq.DisplayOrder = request.DisplayOrder;
        faq.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật câu hỏi."
        });
    }

    // DELETE /api/admin/service-faqs/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/service-faqs/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var faq = await _db.ServiceFaqs
            .FirstOrDefaultAsync(f => f.Faqid == id, ct);

        if (faq == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy câu hỏi."
            });

        _db.ServiceFaqs.Remove(faq);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa câu hỏi."
        });
    }
}
