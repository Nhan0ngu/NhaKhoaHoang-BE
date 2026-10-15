
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NhaKhoaHoang.API.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException ex)
            when (FindPostgresException(ex)?.SqlState == "23505")
        {
            _logger.LogWarning(
                ex,
                "Database unique constraint violation. Path: {Path}",
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Dữ liệu đã tồn tại",
                "Giá trị bạn nhập đã tồn tại trong hệ thống.");
        }
        catch (DbUpdateException ex)
            when (FindPostgresException(ex)?.SqlState is
                  "23503" or "23502" or "23514")
        {
            _logger.LogWarning(
                ex,
                "Database constraint violation. Path: {Path}",
                context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Dữ liệu có liên kết",
                "Không thể thực hiện thao tác vì dữ liệu đang được sử dụng hoặc không thỏa mãn ràng buộc.");
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException ||
                  !context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Unhandled API exception. Path: {Path}",
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Lỗi hệ thống",
                    "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.");
            }
            else
            {
                throw;
            }
        }
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        Exception? current = exception;

        while (current != null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException(
                "Cannot write error after response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType =
            "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] =
            context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: CancellationToken.None);
    }
}
