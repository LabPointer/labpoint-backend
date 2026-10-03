using DTOs.Error;
using Microsoft.AspNetCore.Diagnostics;

namespace Backend.Handler;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        ErroResponseDTO body;

        switch (exception)
        {
            case AppException app:
                status = app.StatusCode;
                body = new ErroResponseDTO(app.Message, app.Logout);
                break;

            default:
                // exceções inesperadas: loga e não vaza detalhes
                logger.LogError(exception, "Erro não tratado");
                status = StatusCodes.Status500InternalServerError;
                body = new ErroResponseDTO("Erro interno do servidor");
                break;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body, ct);
        return true;
    }
}