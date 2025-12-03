using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class DeferredMessageProcessor(IOptions<ServiceBusOptions> serviceBusOptions, IPipelineFactory pipelineFactory)
    : IDeferredMessageProcessor
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private Guid _checkpointMessageId = Guid.Empty;
    private DateTime _ignoreTillDate = DateTime.MaxValue.ToUniversalTime();
    private DateTime _nextProcessingDateTime = DateTime.MinValue.ToUniversalTime();

    public async Task ExecuteAsync(IProcessorThreadContext _, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serviceBusOptions.Inbox.DeferredTransportUri))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (DateTime.UtcNow < _nextProcessingDateTime)
            {
                try
                {
                    await Task.Delay(_serviceBusOptions.Inbox.DeferredMessageProcessorWaitInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }

                return;
            }

            var pipeline = await _pipelineFactory.GetPipelineAsync<DeferredMessagePipeline>(cancellationToken);

            try
            {
                pipeline.State.ResetWorking();
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

                    return;
                }

                if (pipeline.State.GetWorking() && transportMessage != null)
                {
                    if (transportMessage.IgnoreTillDate.ToUniversalTime() < _ignoreTillDate)
                    {
                        _ignoreTillDate = transportMessage.IgnoreTillDate.ToUniversalTime();
                    }

                    if (!_checkpointMessageId.Equals(transportMessage.MessageId))
                    {
                        if (!_checkpointMessageId.Equals(Guid.Empty))
                        {
                            return;
                        }

                        _checkpointMessageId = transportMessage.MessageId;

                        return;
                    }
                }

                _checkpointMessageId = Guid.Empty;

                if (_nextProcessingDateTime > DateTime.UtcNow)
                {
                    return;
                }

                var nextProcessingDateTime = DateTime.UtcNow.Add(_serviceBusOptions.Inbox.DeferredMessageProcessorResetInterval);

                await AdjustNextProcessingDateTimeAsync(_ignoreTillDate < nextProcessingDateTime
                    ? _ignoreTillDate
                    : nextProcessingDateTime, cancellationToken);

                _ignoreTillDate = DateTime.MaxValue.ToUniversalTime();

                await _serviceBusOptions.DeferredMessageProcessingHalted.InvokeAsync(new(_nextProcessingDateTime), cancellationToken);
            }
            finally
            {
                await _pipelineFactory.ReleasePipelineAsync(pipeline, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MessageDeferredAsync(DateTime ignoreTillDate, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (ignoreTillDate.ToUniversalTime() < _nextProcessingDateTime)
            {
                await AdjustNextProcessingDateTimeAsync(ignoreTillDate.ToUniversalTime(), cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AdjustNextProcessingDateTimeAsync(DateTime dateTime, CancellationToken cancellationToken = default)
    {
        _nextProcessingDateTime = dateTime;

        await _serviceBusOptions.DeferredMessageProcessingAdjusted.InvokeAsync(new(_nextProcessingDateTime), cancellationToken);
    }
}