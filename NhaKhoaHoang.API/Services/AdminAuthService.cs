
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Services;

public class AdminAuthService
{
    private readonly NhaKhoaDbContext _context;
    private readonly IConfiguration _configuration;

    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public AdminAuthService(
        NhaKhoaDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AdminLoginResponse?> LoginAsync(
        AdminLoginRequest request)
    {
        var username = request.Username.Trim();

        var admin = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Username == username && a.IsActive);

        if (admin == null)
        {
            return null;
        }

        PasswordVerificationResult verification;

        try
        {
            verification = _passwordHasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                request.Password);
        }
        catch (FormatException)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var jwtKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) ||
            Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing or invalid.");
        }

        var expiresAt = DateTime.UtcNow.AddHours(2);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,
                admin.AdminId.ToString()),

            new(JwtRegisteredClaimNames.UniqueName,
                admin.Username),

            new(ClaimTypes.NameIdentifier,
                admin.AdminId.ToString()),

            new(ClaimTypes.Name, admin.Username),

            new(ClaimTypes.Role, "Admin"),

            new(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AdminLoginResponse
        {
            Success = true,
            Token = tokenString,
            TokenType = "Bearer",
            ExpiresIn = 7200,
            AdminId = admin.AdminId,
            Username = admin.Username,
            FullName = admin.FullName
        };
    }
}
