using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IMessageHandlerInvoker
{
    ValueTask<bool> InvokeAsync(IPipelineContext<HandleMessage> pipelineContext, CancellationToken cancellationToken = default);
}