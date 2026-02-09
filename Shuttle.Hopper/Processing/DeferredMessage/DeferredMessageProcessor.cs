using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IOptions<HopperOptions> hopperOptions, IPipelineFactory pipelineFactory)
    : IDeferredMessageProcessor
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private Guid _checkpointMessageId = Guid.Empty;
    private DateTimeOffset _ignoreUntil = DateTimeOffset.MaxValue;
    private DateTimeOffset _nextProcessingAt = DateTimeOffset.MinValue;

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_hopperOptions.Inbox.DeferredTransportUri == null)
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (DateTimeOffset.UtcNow < _nextProcessingAt)
            {
                return false;
            }

            var pipeline = await _pipelineFactory.GetPipelineAsync<DeferredMessagePipeline>(cancellationToken);

            pipeline.State.ResetWorkPerformed();
            pipeline.State.SetDeferredMessageReturned(false);
            pipeline.State.SetTransportMessage(null);

            await pipeline.ExecuteAsync(cancellationToken).ConfigureAwait(false);

            var transportMessage = pipeline.State.GetTransportMessage();

            if (pipeline.State.GetDeferredMessageReturned())
            {
                if (transportMessage != null &&
                    transportMessage.MessageId.Equals(_checkpointMessageId))
                {
                    _checkpointMessageId = Guid.Empty;
                }

                return false;
            }

            if (pipeline.State.GetWorkPerformed() && transportMessage != null)
            {
                if (transportMessage.IgnoreUntil.ToUniversalTime() < _ignoreUntil)
                {
                    _ignoreUntil = transportMessage.IgnoreUntil.ToUniversalTime();
                }

                if (!_checkpointMessageId.Equals(transportMessage.MessageId))
                {
                    if (!_checkpointMessageId.Equals(Guid.Empty))
                    {
                        return false;
                    }

                    _checkpointMessageId = transportMessage.MessageId;

                    return false;
                }
            }

            _checkpointMessageId = Guid.Empty;

            if (_nextProcessingAt > DateTimeOffset.UtcNow)
            {
                return false;
            }

            var nextProcessingDateTime = DateTimeOffset.UtcNow.Add(_hopperOptions.Inbox.DeferredMessageProcessorResetInterval);

            await AdjustNextProcessingDateTimeAsync(_ignoreUntil < nextProcessingDateTime
                ? _ignoreUntil
                : nextProcessingDateTime, cancellationToken);

            _ignoreUntil = DateTimeOffset.MaxValue.ToUniversalTime();

            await _hopperOptions.DeferredMessageProcessingHalted.InvokeAsync(new(_nextProcessingAt), cancellationToken);

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
            if (ignoreUntil.ToUniversalTime() < _nextProcessingAt)
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
        _nextProcessingAt = nextProcessingAt;

        await _hopperOptions.DeferredMessageProcessingAdjusted.InvokeAsync(new(_nextProcessingAt), cancellationToken);
    }
}