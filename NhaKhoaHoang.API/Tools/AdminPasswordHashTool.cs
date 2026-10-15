
using Microsoft.AspNetCore.Identity;
using NhaKhoaHoang.API.Models;

namespace NhaKhoaHoang.API.Tools;

public static class AdminPasswordHashTool
{
    public static string GenerateHash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));
        }

        var admin = new AdminUser();

        var hasher = new PasswordHasher<AdminUser>();

        return hasher.HashPassword(admin, password);
    }
}
