using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public interface IStartupProcessingObserver :
    IPipelineObserver<OnCreatePhysicalTransports>,
    IPipelineObserver<OnConfigureThreadPools>,
    IPipelineObserver<OnStartThreadPools>;

public class StartupProcessingObserver(IOptions<ServiceBusOptions> serviceBusOptions, IServiceBus serviceBus, IDeferredMessageProcessor deferredMessageProcessor, IPipelineFactory pipelineFactory, IProcessorThreadPoolFactory processorThreadPoolFactory)
    : IStartupProcessingObserver
{
    private readonly IDeferredMessageProcessor _deferredMessageProcessor = Guard.AgainstNull(deferredMessageProcessor);
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly IProcessorThreadPoolFactory _processorThreadPoolFactory = Guard.AgainstNull(processorThreadPoolFactory);
    private readonly IServiceBus _serviceBus = Guard.AgainstNull(serviceBus);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<OnCreatePhysicalTransports> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (!_serviceBusOptions.CreatePhysicalTransports)
        {
            return;
        }

        Guard.Against<InvalidOperationException>(_serviceBus.HasInbox() && _serviceBus.Inbox!.WorkTransport == null && string.IsNullOrEmpty(_serviceBusOptions.Inbox.WorkTransportUri), string.Format(Resources.RequiredTransportUriMissingException, "Inbox.WorkTransportUri"));
        Guard.Against<InvalidOperationException>(_serviceBus.HasOutbox() && _serviceBus.Outbox!.WorkTransport == null && string.IsNullOrEmpty(_serviceBusOptions.Outbox.WorkTransportUri), string.Format(Resources.RequiredTransportUriMissingException, "Outbox.WorkTransportUri"));

        await _serviceBus.CreatePhysicalTransportsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(IPipelineContext<OnConfigureThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_serviceBus.HasInbox() && _serviceBus.Inbox!.HasDeferredTransport())
        {
            pipelineContext.Pipeline.State.Add("DeferredMessageThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "DeferredMessageProcessor",
                1,
                new DeferredMessageProcessorFactory(_deferredMessageProcessor), cancellationToken));
        }

        if (_serviceBus.HasInbox())
        {
            pipelineContext.Pipeline.State.Add("InboxThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "InboxProcessor",
                _serviceBusOptions.Inbox.ThreadCount,
                new InboxProcessorFactory(_serviceBusOptions, _pipelineFactory), cancellationToken));
        }

        if (_serviceBus.HasOutbox())
        {
            pipelineContext.Pipeline.State.Add("OutboxThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "OutboxProcessor",
                _serviceBusOptions.Outbox.ThreadCount,
                new OutboxProcessorFactory(_serviceBusOptions, _pipelineFactory), cancellationToken));
        }

        await Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<OnStartThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext.Pipeline.State);

        var inboxThreadPool = state.Get<IProcessorThreadPool>("InboxThreadPool");
        var controlInboxThreadPool = state.Get<IProcessorThreadPool>("ControlInboxThreadPool");
        var outboxThreadPool = state.Get<IProcessorThreadPool>("OutboxThreadPool");
        var deferredMessageThreadPool = state.Get<IProcessorThreadPool>("DeferredMessageThreadPool");

        if (inboxThreadPool != null)
        {
            await inboxThreadPool.StartAsync(cancellationToken);
        }

        if (controlInboxThreadPool != null)
        {
            await controlInboxThreadPool.StartAsync(cancellationToken);
        }

        if (outboxThreadPool != null)
        {
            await outboxThreadPool.StartAsync(cancellationToken);
        }

        if (deferredMessageThreadPool != null)
        {
            await deferredMessageThreadPool.StartAsync(cancellationToken);
        }
    }
}