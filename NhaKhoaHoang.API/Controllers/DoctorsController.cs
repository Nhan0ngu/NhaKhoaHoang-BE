
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api")]
public class DoctorsController : ControllerBase
{
    private readonly NhaKhoaDbContext _db;

    public DoctorsController(NhaKhoaDbContext db)
    {
        _db = db;
    }

    // GET /api/doctors
    [AllowAnonymous]
    [HttpGet("doctors")]
    public async Task<IActionResult> GetPublic(CancellationToken ct)
    {
        var data = await _db.Doctors
            .AsNoTracking()
            .Where(d => d.IsPublished)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.DoctorId)
            .Select(d => new
            {
                d.DoctorId,
                d.FullName,
                d.Specialty,
                d.Biography,
                d.PhotoUrl,
                d.DisplayOrder
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/doctors
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/doctors")]
    public async Task<IActionResult> GetAdmin(CancellationToken ct)
    {
        var data = await _db.Doctors
            .AsNoTracking()
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.DoctorId)
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            count = data.Count,
            data
        });
    }

    // GET /api/admin/doctors/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/doctors/{id:int}")]
    public async Task<IActionResult> GetAdminById(
        int id,
        CancellationToken ct)
    {
        var doctor = await _db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == id, ct);

        if (doctor == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bác sĩ."
            });

        return Ok(new { success = true, data = doctor });
    }

    // POST /api/admin/doctors
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/doctors")]
    public async Task<IActionResult> Create(
        [FromBody] DoctorRequest request,
        CancellationToken ct)
    {
        var name = request.FullName.Trim();

        if (name.Length < 2)
            return BadRequest(new
            {
                success = false,
                message = "Họ tên bác sĩ không hợp lệ."
            });

        var doctor = new Doctor
        {
            FullName = name,
            Specialty = request.Specialty?.Trim(),
            Biography = request.Biography,
            PhotoUrl = request.PhotoUrl?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsPublished = request.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new
        {
            success = true,
            message = "Đã thêm hồ sơ bác sĩ.",
            data = doctor
        });
    }

    // PUT /api/admin/doctors/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/doctors/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] DoctorRequest request,
        CancellationToken ct)
    {
        var doctor = await _db.Doctors
            .FirstOrDefaultAsync(d => d.DoctorId == id, ct);

        if (doctor == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bác sĩ."
            });

        var name = request.FullName.Trim();

        if (name.Length < 2)
            return BadRequest(new
            {
                success = false,
                message = "Họ tên bác sĩ không hợp lệ."
            });

        doctor.FullName = name;
        doctor.Specialty = request.Specialty?.Trim();
        doctor.Biography = request.Biography;
        doctor.PhotoUrl = request.PhotoUrl?.Trim();
        doctor.DisplayOrder = request.DisplayOrder;
        doctor.IsPublished = request.IsPublished;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật bác sĩ.",
            data = doctor
        });
    }

    // DELETE /api/admin/doctors/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/doctors/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var doctor = await _db.Doctors
            .FirstOrDefaultAsync(d => d.DoctorId == id, ct);

        if (doctor == null)
            return NotFound(new
            {
                success = false,
                message = "Không tìm thấy bác sĩ."
            });

        _db.Doctors.Remove(doctor);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            message = "Đã xóa hồ sơ bác sĩ."
        });
    }
}
