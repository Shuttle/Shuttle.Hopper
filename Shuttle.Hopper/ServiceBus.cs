using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class ServiceBus(IOptions<ServiceBusOptions> serviceBusOptions, ITransportService transportService, IPipelineFactory pipelineFactory, IMessageSender messageSender)
    : IServiceBus
{
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private readonly IMessageSender _messageSender = Guard.AgainstNull(messageSender);
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);
    private CancellationTokenSource _cancellationTokenSource = new();

    private IProcessorThreadPool? _controlInboxThreadPool;
    private IProcessorThreadPool? _deferredMessageThreadPool;

    private bool _disposed;
    private IProcessorThreadPool? _inboxThreadPool;
    private IProcessorThreadPool? _outboxThreadPool;

    public async Task<IServiceBus> StartAsync(CancellationToken cancellationToken = default)
    {
        if (Started)
        {
            throw new ApplicationException(Resources.ServiceBusInstanceAlreadyStarted);
        }

        _cancellationTokenSource = new();

        if (!string.IsNullOrWhiteSpace(_serviceBusOptions.Inbox.WorkTransportUri))
        {
            Inbox = new InboxConfiguration
            {
                WorkTransport = await _transportService.GetAsync(_serviceBusOptions.Inbox.WorkTransportUri, cancellationToken: cancellationToken),
                DeferredTransport =
                    string.IsNullOrWhiteSpace(_serviceBusOptions.Inbox.DeferredTransportUri)
                        ? null
                        : await _transportService.GetAsync(_serviceBusOptions.Inbox.DeferredTransportUri, cancellationToken: cancellationToken),
                ErrorTransport =
                    string.IsNullOrWhiteSpace(_serviceBusOptions.Inbox.ErrorTransportUri)
                        ? null
                        : await _transportService.GetAsync(_serviceBusOptions.Inbox.ErrorTransportUri, cancellationToken: cancellationToken)
            };
        }

        if (!string.IsNullOrWhiteSpace(_serviceBusOptions.Outbox.WorkTransportUri))
        {
            Outbox = new OutboxConfiguration
            {
                WorkTransport = await _transportService.GetAsync(_serviceBusOptions.Outbox.WorkTransportUri, cancellationToken: cancellationToken),
                ErrorTransport =
                    string.IsNullOrWhiteSpace(_serviceBusOptions.Outbox.ErrorTransportUri)
                        ? null
                        : await _transportService.GetAsync(_serviceBusOptions.Outbox.ErrorTransportUri, cancellationToken: cancellationToken)
            };
        }

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
            var shutdownPipeline = await _pipelineFactory.GetPipelineAsync<ShutdownPipeline>(CancellationToken.None);

            await shutdownPipeline.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        _pipelineFactory.Flush();

        Started = false;
    }

    public bool Started { get; private set; }
    public IInboxConfiguration? Inbox { get; private set; }
    public IOutboxConfiguration? Outbox { get; private set; }

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