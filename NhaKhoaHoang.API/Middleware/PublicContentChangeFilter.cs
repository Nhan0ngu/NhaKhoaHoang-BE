using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using NhaKhoaHoang.API.Hubs;

namespace NhaKhoaHoang.API.Middleware;

/// <summary>
/// Phát event sau khi action Admin CRUD thực thi thành công.
/// Không gọi SignalR từ FE Admin vì client không đáng tin.
/// Không phát event đối với API tư vấn / tài khoản / thông báo Admin.
/// </summary>
public sealed class PublicContentChangeFilter : IAsyncResultFilter
{
    private readonly IHubContext<PublicContentHub> _hub;
    private readonly ILogger<PublicContentChangeFilter> _logger;

    public PublicContentChangeFilter(
        IHubContext<PublicContentHub> hub,
        ILogger<PublicContentChangeFilter> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        // Luôn cho controller chạy như trước; không sửa response hoặc dữ liệu.
        var executed = await next();
        if (executed.Canceled || executed.Exception != null) return;

        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;

        if (!HttpMethods.IsPost(request.Method) &&
            !HttpMethods.IsPut(request.Method) &&
            !HttpMethods.IsPatch(request.Method) &&
            !HttpMethods.IsDelete(request.Method)) return;

        // Chỉ xử lý API admin, và user đã qua JWT authorization của BE.
        if (!request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase)
            || !context.HttpContext.User.IsInRole("Admin")) return;

        if (response.StatusCode < 200 || response.StatusCode >= 300) return;

        if (context.ActionDescriptor is not ControllerActionDescriptor action) return;

        // Danh sách whitelist: không phát các thay đổi private.
        var category = action.ControllerName switch
        {
            "AdminServices" or "ServiceCategories" or "ServiceFaqs" => "services",
            "ServicePrices" => "prices",
            "Articles" or "ArticleCategories" => "articles",
            "Branches" => "branches",
            "Doctors" => "doctors",
            "SitePages" => "pages",
            "SiteSettings" => "settings",
            _ => null
        };

        if (category is null) return;

        // Payload không có ID bệnh nhân, JWT, giá chưa công khai hoặc nội dung CMS.
        // Client tự GET lại dữ liệu qua các endpoint public được lọc IsPublished.
        try
        {
            await _hub.Clients.All.SendAsync(
                "PublicContentChanged",
                new { category },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Lưu DB đã thành công; SignalR gặp lỗi không được biến API thành 500.
            _logger.LogWarning(ex,
                "SignalR broadcast failed for category {Category}", category);
        }
    }
}
