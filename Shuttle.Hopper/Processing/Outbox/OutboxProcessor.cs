using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class OutboxProcessor(IPipelineFactory pipelineFactory) : TransportProcessor<OutboxPipeline>(pipelineFactory);