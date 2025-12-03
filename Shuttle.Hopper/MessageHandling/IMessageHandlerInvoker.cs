using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IMessageHandlerInvoker
{
    ValueTask<bool> InvokeAsync(IPipelineContext<OnHandleMessage> pipelineContext, CancellationToken cancellationToken = default);
}