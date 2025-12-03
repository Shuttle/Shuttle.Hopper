using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Reflection;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class ServiceBus(IPipelineFactory pipelineFactory, IMessageSender messageSender, ICancellationTokenSource? cancellationTokenSource = null)
    : IServiceBus
{
    private readonly ICancellationTokenSource _cancellationTokenSource = cancellationTokenSource ?? new DefaultCancellationTokenSource();
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);

    private IProcessorThreadPool? _controlInboxThreadPool;
    private IProcessorThreadPool? _deferredMessageThreadPool;

    private bool _disposed;
    private IProcessorThreadPool? _inboxThreadPool;
    private IProcessorThreadPool? _outboxThreadPool;

    public bool Asynchronous { get; private set; }

    public async Task<IServiceBus> StartAsync(CancellationToken cancellationToken = default)
    {
        return await StartAsync(false, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!Started)
        {
            return;
        }

        _cancellationTokenSource.Renew();

        _deferredMessageThreadPool?.Dispose();
        _inboxThreadPool?.Dispose();
        _controlInboxThreadPool?.Dispose();
        _outboxThreadPool?.Dispose();

        try
        {
            var shutdownPipeline = await _pipelineFactory.GetPipelineAsync<ShutdownPipeline>(cancellationToken);

            await shutdownPipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        _pipelineFactory.Flush();

        Started = false;
    }

    public bool Started { get; private set; }
    public IInboxConfiguration? Inbox { get; }
    public IOutboxConfiguration? Outbox { get; }

    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await _messageSender.SendAsync(message, null, builder, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await _messageSender.PublishAsync(message, null, builder, cancellationToken);
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);

        await _cancellationTokenSource.TryDisposeAsync();

        _disposed = true;
    }

    private async Task<IServiceBus> StartAsync(bool sync, CancellationToken cancellationToken)
    {
        if (Started)
        {
            throw new ApplicationException(Resources.ServiceBusInstanceAlreadyStarted);
        }

        var startupPipeline = await _pipelineFactory.GetPipelineAsync<StartupPipeline>(cancellationToken);

        Started = true; // required for using ServiceBus in OnStarted event
        Asynchronous = !sync;

        try
        {
            await startupPipeline.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            _inboxThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("InboxThreadPool");
            _controlInboxThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("ControlInboxThreadPool");
            _outboxThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("OutboxThreadPool");
            _deferredMessageThreadPool = startupPipeline.State.Get<IProcessorThreadPool>("DeferredMessageThreadPool");
        }
        catch
        {
            Started = false;
            throw;
        }

        return this;
    }

    private void StartedGuard()
    {
        if (Started)
        {
            return;
        }

        throw new InvalidOperationException(Resources.ServiceBusInstanceNotStarted);
    }
}