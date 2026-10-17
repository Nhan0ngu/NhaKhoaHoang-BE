
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class BranchesController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public BranchesController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/branches
    [AllowAnonymous]
    [HttpGet("branches")]
    public async Task<IActionResult> GetPublic(CancellationToken ct)
    {
        var data = await _db.Branches
            .AsNoTracking()
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BranchId)
            .Select(b => new
            {
                b.BranchId,
                b.Name,
                b.Address,
                b.Phone,
                b.OpeningHours,
                b.GoogleMapsUrl,
                b.IsPrimary
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/branches
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/branches")]
    public async Task<IActionResult> GetAdmin(CancellationToken ct)
    {
        var data = await _db.Branches
            .AsNoTracking()
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BranchId)
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/branches/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/branches/{id:int}")]
    public async Task<IActionResult> GetAdminById(
        int id,
        CancellationToken ct)
    {
        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == id, ct);

        if (branch == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy cơ sở."
            });

        return Ok(new { success = true, data = branch });
    }

    // POST /api/admin/branches
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/branches")]
    public async Task<IActionResult> Create(
        [FromBody] BranchRequest request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (name.Length < 2)
            return BadRequest(new
            {
                success = false,
                message = "Tên cơ sở không hợp lệ."
            });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

        if (request.IsPrimary)
        {
            // ExecuteUpdateAsync cần giá trị thời gian được tính trước,
            // không dùng trực tiếp DateTime.UtcNow trong biểu thức SQL.
            // DbContext ánh xạ thời gian sang timestamp without time zone.
            var updatedAtUtc = DateTime.SpecifyKind(
                DateTime.UtcNow, DateTimeKind.Unspecified);

            await _db.Branches
                .Where(b => b.IsPrimary)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.IsPrimary, false)
                        .SetProperty(b => b.UpdatedAt,
                            (DateTime?)updatedAtUtc),
                    ct);
        }

        var branch = new Branch
        {
            Name = name,
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            OpeningHours = request.OpeningHours?.Trim(),
            GoogleMapsUrl = request.GoogleMapsUrl?.Trim(),
            IsPrimary = request.IsPrimary,
            IsPublished = request.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm cơ sở.",
            data = branch
        });
    }

    // PUT /api/admin/branches/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/branches/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BranchRequest request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (name.Length < 2)
            return BadRequest(new
            {
                success = false,
                message = "Tên cơ sở không hợp lệ."
            });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.BranchId == id, ct);

        if (branch == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy cơ sở."
            });

        if (request.IsPrimary)
        {
            // ExecuteUpdateAsync cần giá trị thời gian được tính trước,
            // không dùng trực tiếp DateTime.UtcNow trong biểu thức SQL.
            // DbContext ánh xạ thời gian sang timestamp without time zone.
            var updatedAtUtc = DateTime.SpecifyKind(
                DateTime.UtcNow, DateTimeKind.Unspecified);

            await _db.Branches
                .Where(b => b.BranchId != id && b.IsPrimary)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.IsPrimary, false)
                        .SetProperty(b => b.UpdatedAt,
                            (DateTime?)updatedAtUtc),
                    ct);
        }

        branch.Name = name;
        branch.Address = request.Address?.Trim();
        branch.Phone = request.Phone?.Trim();
        branch.OpeningHours = request.OpeningHours?.Trim();
        branch.GoogleMapsUrl = request.GoogleMapsUrl?.Trim();
        branch.IsPrimary = request.IsPrimary;
        branch.IsPublished = request.IsPublished;
        branch.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật cơ sở.",
            data = branch
        });
    }

    // DELETE /api/admin/branches/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/branches/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.BranchId == id, ct);

        if (branch == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy cơ sở."
            });

        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa cơ sở."
        });
    }
}
