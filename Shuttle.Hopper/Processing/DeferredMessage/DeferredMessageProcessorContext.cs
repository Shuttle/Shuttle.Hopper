using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessageProcessorContext(IOptions<HopperOptions> hopperOptions) : IDeferredMessageProcessorContext
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Guid _checkpointMessageId = Guid.Empty;
    public Guid CheckpointMessageId => _checkpointMessageId;
    public DateTimeOffset IgnoreUntil { get; private set; } = DateTimeOffset.MaxValue;

    public DateTimeOffset NextProcessingAt { get; private set; } = DateTimeOffset.MinValue;

    public bool ShouldCheckDeferredMessages => _hopperOptions.Inbox.DeferredTransportUri != null && DateTimeOffset.UtcNow > NextProcessingAt;

    public async ValueTask<bool> GetResultAsync(IState state, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var transportMessage = state.GetTransportMessage();

            if (state.HasDeferredMessageReturned())
            {
                if (transportMessage != null && transportMessage.MessageId.Equals(_checkpointMessageId))
                {
                    NextProcessingAt = DateTimeOffset.UtcNow;
                    _checkpointMessageId = Guid.Empty;
                }

                return true;
            }

            if (state.HasReceivedMessage() && transportMessage != null)
            {
                if (transportMessage.IgnoreUntil < IgnoreUntil)
                {
                    IgnoreUntil = transportMessage.IgnoreUntil;
                }

                if (!_checkpointMessageId.Equals(transportMessage.MessageId))
                {
                    if (!_checkpointMessageId.Equals(Guid.Empty))
                    {
                        return true;
                    }

                    _checkpointMessageId = transportMessage.MessageId;

                    return true;
                }
            }

            _checkpointMessageId = Guid.Empty;

            if (NextProcessingAt > DateTimeOffset.UtcNow)
            {
                return false;
            }

            var nextProcessingDateTime = DateTimeOffset.UtcNow.Add(_hopperOptions.Inbox.DeferredMessageProcessorResetInterval);

            await AdjustNextProcessingDateTimeAsync(IgnoreUntil < nextProcessingDateTime
                ? IgnoreUntil
                : nextProcessingDateTime, cancellationToken);

            IgnoreUntil = DateTimeOffset.MaxValue.ToUniversalTime();

            await _hopperOptions.DeferredMessageProcessingHalted.InvokeAsync(new(NextProcessingAt), cancellationToken);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MessageDeferredAsync(DateTimeOffset ignoreUntil, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (ignoreUntil.ToUniversalTime() < NextProcessingAt)
            {
                await AdjustNextProcessingDateTimeAsync(ignoreUntil.ToUniversalTime(), cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AdjustNextProcessingDateTimeAsync(DateTimeOffset nextProcessingAt, CancellationToken cancellationToken = default)
    {
        NextProcessingAt = nextProcessingAt;

        await _hopperOptions.DeferredMessageProcessingAdjusted.InvokeAsync(new(NextProcessingAt), cancellationToken);
    }
}