using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IOptions<ServiceBusOptions> serviceBusOptions, IPipelineFactory pipelineFactory)
    : IDeferredMessageProcessor
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private Guid _checkpointMessageId = Guid.Empty;
    private DateTimeOffset _ignoreTillDateTime = DateTimeOffset.MaxValue;
    private DateTimeOffset _nextProcessingDateTime = DateTimeOffset.MinValue;

    public async ValueTask<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_serviceBusOptions.Inbox.DeferredTransportUri == null)
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (DateTimeOffset.UtcNow < _nextProcessingDateTime)
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
                if (transportMessage.IgnoreTillDateTime.ToUniversalTime() < _ignoreTillDateTime)
                {
                    _ignoreTillDateTime = transportMessage.IgnoreTillDateTime.ToUniversalTime();
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

            if (_nextProcessingDateTime > DateTimeOffset.UtcNow)
            {
                return false;
            }

            var nextProcessingDateTime = DateTimeOffset.UtcNow.Add(_serviceBusOptions.Inbox.DeferredMessageProcessorResetInterval);

            await AdjustNextProcessingDateTimeAsync(_ignoreTillDateTime < nextProcessingDateTime
                ? _ignoreTillDateTime
                : nextProcessingDateTime, cancellationToken);

            _ignoreTillDateTime = DateTimeOffset.MaxValue.ToUniversalTime();

            await _serviceBusOptions.DeferredMessageProcessingHalted.InvokeAsync(new(_nextProcessingDateTime), cancellationToken);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MessageDeferredAsync(DateTimeOffset ignoreTillDateTime, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (ignoreTillDateTime.ToUniversalTime() < _nextProcessingDateTime)
            {
                await AdjustNextProcessingDateTimeAsync(ignoreTillDateTime.ToUniversalTime(), cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AdjustNextProcessingDateTimeAsync(DateTimeOffset dateTime, CancellationToken cancellationToken = default)
    {
        _nextProcessingDateTime = dateTime;

        await _serviceBusOptions.DeferredMessageProcessingAdjusted.InvokeAsync(new(_nextProcessingDateTime), cancellationToken);
    }
}