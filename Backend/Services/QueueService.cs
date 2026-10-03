namespace Backend.Services;

public sealed class QueueService(
    IBackgroundTaskService service,
    IServiceScopeFactory scopeFactory,
    ILogger<QueueService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await service.DequeueAsync(stoppingToken);

                await using var scope = scopeFactory.CreateAsyncScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar item da fila");
            }
        }
    }
}