
namespace NhaKhoaHoang.API.DTOs
{
    public class AdminLoginResponse
    {
        public bool Success { get; set; }

        public string Token { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public int ExpiresIn { get; set; }

        public int AdminId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
    }
}
