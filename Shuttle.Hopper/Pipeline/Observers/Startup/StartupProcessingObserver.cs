using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public interface IStartupProcessingObserver :
    IPipelineObserver<Starting>,
    IPipelineObserver<CreatePhysicalTransports>,
    IPipelineObserver<ConfigureThreadPools>,
    IPipelineObserver<StartThreadPools>;

public class StartupProcessingObserver(IOptions<ServiceBusOptions> serviceBusOptions, IServiceBusConfiguration serviceBusConfiguration, IDeferredMessageProcessor deferredMessageProcessor, IPipelineFactory pipelineFactory, IProcessorThreadPoolFactory processorThreadPoolFactory)
    : IStartupProcessingObserver
{
    private readonly IDeferredMessageProcessor _deferredMessageProcessor = Guard.AgainstNull(deferredMessageProcessor);
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly IProcessorThreadPoolFactory _processorThreadPoolFactory = Guard.AgainstNull(processorThreadPoolFactory);
    private readonly IServiceBusConfiguration _serviceBusConfiguration = Guard.AgainstNull(serviceBusConfiguration);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<CreatePhysicalTransports> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (!_serviceBusOptions.CreatePhysicalTransports)
        {
            return;
        }

        Guard.Against<InvalidOperationException>(_serviceBusConfiguration.HasInbox() && _serviceBusConfiguration.Inbox!.WorkTransport == null && _serviceBusOptions.Inbox.WorkTransportUri == null, string.Format(Resources.RequiredTransportUriMissingException, "Inbox.WorkTransportUri"));
        Guard.Against<InvalidOperationException>(_serviceBusConfiguration.HasOutbox() && _serviceBusConfiguration.Outbox!.WorkTransport == null && _serviceBusOptions.Outbox.WorkTransportUri == null, string.Format(Resources.RequiredTransportUriMissingException, "Outbox.WorkTransportUri"));

        await _serviceBusConfiguration.CreatePhysicalTransportsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(IPipelineContext<ConfigureThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_serviceBusConfiguration.HasInbox() && _serviceBusConfiguration.Inbox!.HasDeferredTransport())
        {
            pipelineContext.Pipeline.State.Add("DeferredMessageThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "DeferredMessageProcessor",
                1,
                new DeferredMessageProcessorFactory(_deferredMessageProcessor), cancellationToken));
        }

        if (_serviceBusConfiguration.HasInbox())
        {
            pipelineContext.Pipeline.State.Add("InboxThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "InboxProcessor",
                _serviceBusOptions.Inbox.ThreadCount,
                new InboxProcessorFactory(_serviceBusOptions, _pipelineFactory), cancellationToken));
        }

        if (_serviceBusConfiguration.HasOutbox())
        {
            pipelineContext.Pipeline.State.Add("OutboxThreadPool", await _processorThreadPoolFactory.CreateAsync(
                "OutboxProcessor",
                _serviceBusOptions.Outbox.ThreadCount,
                new OutboxProcessorFactory(_serviceBusOptions, _pipelineFactory), cancellationToken));
        }
    }

    public async Task ExecuteAsync(IPipelineContext<StartThreadPools> pipelineContext, CancellationToken cancellationToken = default)
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

    public async Task ExecuteAsync(IPipelineContext<Starting> pipelineContext, CancellationToken cancellationToken = default)
    {
        await _serviceBusConfiguration.ConfigureAsync(cancellationToken);
    }
}