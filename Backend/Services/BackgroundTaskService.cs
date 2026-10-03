using System.Threading.Channels;

namespace Backend.Services;

public interface IBackgroundTaskService
{
    ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem);
    ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct);
}

public sealed class BackgroundTaskService : IBackgroundTaskService
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, ValueTask>> _channel;

    public BackgroundTaskService(int capacity = 1024)
    {
        _channel = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, ValueTask>>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
        => _channel.Writer.WriteAsync(workItem);

    public ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}