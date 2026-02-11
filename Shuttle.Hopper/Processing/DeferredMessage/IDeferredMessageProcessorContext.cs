using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IDeferredMessageProcessorContext
{
    Guid CheckpointMessageId { get; }
    DateTimeOffset IgnoreUntil { get; }
    DateTimeOffset NextProcessingAt { get; }
    bool ShouldCheckDeferredMessages { get; }
    ValueTask<bool> GetResultAsync(IState state, CancellationToken cancellationToken = default);
    Task MessageDeferredAsync(DateTimeOffset ignoreUntil, CancellationToken cancellationToken = default);
}