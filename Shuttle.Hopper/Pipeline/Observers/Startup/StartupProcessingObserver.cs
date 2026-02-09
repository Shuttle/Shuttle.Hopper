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

public class StartupProcessingObserver(IOptions<HopperOptions> hopperOptions, IOptions<ThreadingOptions> threadingOptions, IServiceScopeFactory serviceScopeFactory, IBusConfiguration busConfiguration, IProcessorIdleStrategy processorIdleStrategy)
    : IStartupProcessingObserver
{
    private readonly ThreadingOptions _threadingOptions = Guard.AgainstNull(Guard.AgainstNull(threadingOptions).Value);
    private readonly IServiceScopeFactory _serviceScopeFactory = Guard.AgainstNull(serviceScopeFactory);
    private readonly IBusConfiguration _busConfiguration = Guard.AgainstNull(busConfiguration);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly IProcessorIdleStrategy _processorIdleStrategy = Guard.AgainstNull(processorIdleStrategy);

    public async Task ExecuteAsync(IPipelineContext<CreatePhysicalTransports> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (!_hopperOptions.CreatePhysicalTransports)
        {
            return;
        }

        Guard.Against<InvalidOperationException>(_busConfiguration.HasInbox() && _busConfiguration.Inbox!.WorkTransport == null && _hopperOptions.Inbox.WorkTransportUri == null, string.Format(Resources.RequiredTransportUriMissingException, "Inbox.WorkTransportUri"));
        Guard.Against<InvalidOperationException>(_busConfiguration.HasOutbox() && _busConfiguration.Outbox!.WorkTransport == null && _hopperOptions.Outbox.WorkTransportUri == null, string.Format(Resources.RequiredTransportUriMissingException, "Outbox.WorkTransportUri"));

        await _busConfiguration.CreatePhysicalTransportsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task ExecuteAsync(IPipelineContext<ConfigureThreadPools> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_busConfiguration.HasInbox() && _busConfiguration.Inbox!.HasDeferredTransport())
        {
            pipelineContext.Pipeline.State.Add("DeferredMessageThread", new ProcessorThread("DeferredMessageProcessor", _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
        }

        if (_busConfiguration.HasInbox())
        {
            pipelineContext.Pipeline.State.Add("InboxThreadPool", new ProcessorThreadPool("InboxProcessor", _hopperOptions.Inbox.ThreadCount, _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
        }

        if (_busConfiguration.HasOutbox())
        {
            pipelineContext.Pipeline.State.Add("OutboxThreadPool", new ProcessorThreadPool("OutboxProcessor", _hopperOptions.Outbox.ThreadCount, _serviceScopeFactory, _threadingOptions, _processorIdleStrategy));
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
        await _busConfiguration.ConfigureAsync(cancellationToken);
    }
}