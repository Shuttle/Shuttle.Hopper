using Microsoft.Extensions.DependencyInjection;
using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Threading;

namespace Shuttle.Hopper;

public class BusControl(IServiceScopeFactory serviceScopeFactory) : IBusControl
{
    private CancellationTokenSource _cancellationTokenSource = new();

    private List<IProcessorThreadPool> _additionalInboxThreadPools = [];
    private IProcessorThreadPool? _deferredMessageThreadPool;

    private bool _disposed;
    private IProcessorThreadPool? _inboxThreadPool;
    private IProcessorThreadPool? _outboxThreadPool;

    public async Task<IBusControl> StartAsync(CancellationToken cancellationToken = default)
    {
        if (Started)
        {
            throw new ApplicationException(Resources.BusInstanceAlreadyStarted);
        }

        _cancellationTokenSource = new();

        using var serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();

        var startupPipeline = serviceScope.ServiceProvider.GetRequiredService<IStartupPipeline>();

        Started = true; // required for using Bus in OnStarted event

        try
        {
            await startupPipeline.ExecuteAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            SetThreadPools(startupPipeline.State);

            var busConfiguration = serviceScope.ServiceProvider.GetRequiredService<IBusConfiguration>();

            Inbox = busConfiguration.Inbox;
            Outbox = busConfiguration.Outbox;
        }
        catch
        {
            // The thread pools that were started before the failure would otherwise keep processing.
            await _cancellationTokenSource.CancelAsync();

            SetThreadPools(startupPipeline.State);
            DisposeThreadPools();

            Started = false;

            throw;
        }

        return this;
    }

    private void SetThreadPools(IState state)
    {
        _inboxThreadPool = state.Get<IProcessorThreadPool>("InboxThreadPool");
        _additionalInboxThreadPools = state.Get<List<IProcessorThreadPool>>("AdditionalInboxThreadPools") ?? [];
        _outboxThreadPool = state.Get<IProcessorThreadPool>("OutboxThreadPool");
        _deferredMessageThreadPool = state.Get<IProcessorThreadPool>("DeferredMessageThreadPool");
    }

    private void DisposeThreadPools()
    {
        _deferredMessageThreadPool?.Dispose();
        _inboxThreadPool?.Dispose();

        foreach (var threadPool in _additionalInboxThreadPools)
        {
            threadPool.Dispose();
        }

        _outboxThreadPool?.Dispose();

        _deferredMessageThreadPool = null;
        _inboxThreadPool = null;
        _additionalInboxThreadPools = [];
        _outboxThreadPool = null;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!Started)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();

        DisposeThreadPools();

        try
        {
            using var serviceScope = Guard.AgainstNull(serviceScopeFactory).CreateScope();

            var shutdownPipeline = serviceScope.ServiceProvider.GetRequiredService<IShutdownPipeline>();

            await shutdownPipeline.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        Inbox = null;
        Outbox = null;

        Started = false;
    }

    public bool Started { get; private set; }

    public IInboxConfiguration? Inbox
    {
        get => Started ? field : throw new ApplicationException(Resources.BusInstanceNotStarted);
        private set;
    }

    public IOutboxConfiguration? Outbox
    {
        get => Started ? field : throw new ApplicationException(Resources.BusInstanceNotStarted);
        private set;
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
}