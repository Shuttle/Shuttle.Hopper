using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public class MessageSender(IPipelineFactory pipelineFactory, ISubscriptionService subscriptionService)
    : IMessageSender
{
    private readonly IPipelineFactory _pipelineFactory = Guard.AgainstNull(pipelineFactory);
    private readonly ISubscriptionService _subscriptionService = Guard.AgainstNull(subscriptionService);

    public async Task DispatchAsync(TransportMessage transportMessage, TransportMessage? transportMessageReceived = null, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(transportMessage);

        var messagePipeline = await _pipelineFactory.GetPipelineAsync<DispatchTransportMessagePipeline>(cancellationToken);

        try
        {
            await messagePipeline.ExecuteAsync(transportMessage, transportMessageReceived, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _pipelineFactory.ReleasePipelineAsync(messagePipeline, cancellationToken);
        }
    }

    public async Task<IEnumerable<TransportMessage>> PublishAsync(object message, TransportMessage? transportMessageReceived = null, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(message);

        var subscribers = (await _subscriptionService.GetSubscribedUrisAsync(message).ConfigureAwait(false)).ToList();

        if (subscribers.Count > 0)
        {
            var transportMessage = await GetTransportMessageAsync(message, transportMessageReceived, builder, cancellationToken).ConfigureAwait(false);

            var result = new List<TransportMessage>(subscribers.Count);

            foreach (var subscriber in subscribers)
            {
                transportMessage.RecipientInboxWorkTransportUri = subscriber;

                await DispatchAsync(transportMessage, transportMessageReceived, cancellationToken).ConfigureAwait(false);

                result.Add(transportMessage);
            }

            return result;
        }

        return [];
    }

    public async Task<TransportMessage> SendAsync(object message, TransportMessage? transportMessageReceived = null, Action<TransportMessageBuilder>? builder = null, CancellationToken cancellationToken = default)
    {
        var transportMessage = await GetTransportMessageAsync(message, transportMessageReceived, builder, cancellationToken).ConfigureAwait(false);

        await DispatchAsync(transportMessage, transportMessageReceived, cancellationToken).ConfigureAwait(false);

        return transportMessage;
    }

    private async Task<TransportMessage> GetTransportMessageAsync(object message, TransportMessage? transportMessageReceived, Action<TransportMessageBuilder>? builder, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(message);

        var messagePipeline = await _pipelineFactory.GetPipelineAsync<TransportMessagePipeline>(cancellationToken);

        try
        {
            await messagePipeline.ExecuteAsync(message, transportMessageReceived, builder, cancellationToken).ConfigureAwait(false);

            return messagePipeline.State.GetTransportMessage()!;
        }
        finally
        {
            await _pipelineFactory.ReleasePipelineAsync(messagePipeline, cancellationToken);
        }
    }
}