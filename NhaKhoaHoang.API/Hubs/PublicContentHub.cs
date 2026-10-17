using Microsoft.AspNetCore.SignalR;

namespace NhaKhoaHoang.API.Hubs;

// Hub công khai CHỈ THÔNG BÁO nhóm nội dung đã thay đổi.
// Không có phương thức gọi từ client và không gửi dữ liệu cá nhân/quản trị.
public sealed class PublicContentHub : Hub
{
}
