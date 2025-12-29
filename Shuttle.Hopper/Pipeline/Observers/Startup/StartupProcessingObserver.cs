using Microsoft.Extensions.DependencyInjection;
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

public class StartupProcessingObserver(IOptions<ServiceBusOptions> serviceBusOptions, IOptions<ThreadingOptions> threadingOptions, IServiceScopeFactory serviceScopeFactory, IServiceBusConfiguration serviceBusConfiguration, IProcessorIdleStrategy processorIdleStrategy)
    : IStartupProcessingObserver
{
    private readonly ThreadingOptions _threadingOptions = Guard.AgainstNull(Guard.AgainstNull(threadingOptions).Value);
    private readonly IServiceScopeFactory _serviceScopeFactory = Guard.AgainstNull(serviceScopeFactory);
    private readonly IServiceBusConfiguration _serviceBusConfiguration = Guard.AgainstNull(serviceBusConfiguration);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private readonly IProcessorIdleStrategy _processorIdleStrategy = Guard.AgainstNull(processorIdleStrategy);

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

    public Task ExecuteAsync(IPipelineContext<ConfigureThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_serviceBusConfiguration.HasInbox() && _serviceBusConfiguration.Inbox!.HasDeferredTransport())
        {
            pipelineContext.Pipeline.State.Add("DeferredMessageThread", new ProcessorThread("DeferredMessageProcessor", _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
        }

        if (_serviceBusConfiguration.HasInbox())
        {
            pipelineContext.Pipeline.State.Add("InboxThreadPool", new ProcessorThreadPool("InboxProcessor", _serviceBusOptions.Inbox.ThreadCount, _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
        }

        if (_serviceBusConfiguration.HasOutbox())
        {
            pipelineContext.Pipeline.State.Add("OutboxThreadPool", new ProcessorThreadPool("OutboxProcessor", _serviceBusOptions.Outbox.ThreadCount, _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
        }

        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(IPipelineContext<StartThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext.Pipeline.State);

        var inboxThreadPool = state.Get<IProcessorThreadPool>("InboxThreadPool");
        var outboxThreadPool = state.Get<IProcessorThreadPool>("OutboxThreadPool");
        var deferredMessageThread = state.Get<ProcessorThread>("DeferredMessageThread");

        if (inboxThreadPool != null)
        {
            await inboxThreadPool.StartAsync(cancellationToken);
        }

        if (outboxThreadPool != null)
        {
            await outboxThreadPool.StartAsync(cancellationToken);
        }

        if (deferredMessageThread != null)
        {
            await deferredMessageThread.StartAsync(cancellationToken);
        }
    }

    public async Task ExecuteAsync(IPipelineContext<Starting> pipelineContext, CancellationToken cancellationToken = default)
    {
        await _serviceBusConfiguration.ConfigureAsync(cancellationToken);
    }
}