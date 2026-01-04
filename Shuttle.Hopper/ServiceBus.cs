using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class ServiceBus(IServiceScopeFactory serviceScopeFactory) : IServiceBus
{
    private IMessageSender? _messageSender;
    private IPipelineFactory? _pipelineFactory;
    private CancellationTokenSource _cancellationTokenSource = new();

    private IProcessorThreadPool? _controlInboxThreadPool;
    private IProcessorThreadPool? _deferredMessageThreadPool;

    private bool _disposed;
    private IProcessorThreadPool? _inboxThreadPool;
    private IProcessorThreadPool? _outboxThreadPool;
    private IServiceScope? _serviceScope;

    public async Task<IServiceBus> StartAsync(CancellationToken cancellationToken = default)
    {
        if (Started)
        {
            throw new ApplicationException(Resources.ServiceBusInstanceAlreadyStarted);
        }

        _serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();

        _messageSender = _serviceScope.ServiceProvider.GetRequiredService<IMessageSender>();
        _pipelineFactory = _serviceScope.ServiceProvider.GetRequiredService<IPipelineFactory>();

        _cancellationTokenSource = new();

        var startupPipeline = await _pipelineFactory.GetPipelineAsync<StartupPipeline>(cancellationToken);

        Started = true; // required for using ServiceBus in OnStarted event

        try
        {
            await startupPipeline.ExecuteAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!Started)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();

        _deferredMessageThreadPool?.Dispose();
        _inboxThreadPool?.Dispose();
        _controlInboxThreadPool?.Dispose();
        _outboxThreadPool?.Dispose();

        try
        {
            var shutdownPipeline = await _pipelineFactory!.GetPipelineAsync<ShutdownPipeline>(CancellationToken.None);

            await shutdownPipeline.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        Inbox = null;
        Outbox = null;

        Started = false;
        
        _serviceScope?.Dispose();
    }

    public bool Started { get; private set; }

    public IInboxConfiguration? Inbox
    {
        get => Started ? field : throw new ApplicationException(Resources.ServiceBusInstanceNotStarted);
        private set;
    }

    public IOutboxConfiguration? Outbox
    {
        get => Started ? field : throw new ApplicationException(Resources.ServiceBusInstanceNotStarted);
        private set;
    }

    public async Task<TransportMessage> SendAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await _messageSender!.SendAsync(message, null, builder, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        StartedGuard();

        return await _messageSender!.PublishAsync(message, null, builder, cancellationToken);
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

        _disposed = true;
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