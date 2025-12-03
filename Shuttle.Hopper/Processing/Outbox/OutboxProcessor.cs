using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class OutboxProcessor(ServiceBusOptions serviceBusOptions, IThreadActivity threadActivity, IPipelineFactory pipelineFactory)
    : TransportProcessor<OutboxPipeline>(serviceBusOptions, threadActivity, pipelineFactory);