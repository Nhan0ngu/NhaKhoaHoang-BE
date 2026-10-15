
using System.ComponentModel.DataAnnotations;

namespace NhaKhoaHoang.API.DTOs
{
    public class AdminLoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; } = string.Empty;
    }
}
