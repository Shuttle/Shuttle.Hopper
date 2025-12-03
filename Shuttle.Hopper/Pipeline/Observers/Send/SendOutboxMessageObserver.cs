using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper;

public interface ISendOutboxMessageObserver : IPipelineObserver<OnDispatchTransportMessage>;

public class SendOutboxMessageObserver(ITransportService transportService) : ISendOutboxMessageObserver
{
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    public async Task ExecuteAsync(IPipelineContext<OnDispatchTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());

        Guard.AgainstEmpty(transportMessage.RecipientInboxWorkTransportUri);

        var transport = await _transportService.GetAsync(transportMessage.RecipientInboxWorkTransportUri, cancellationToken);

        await using var stream = await receivedMessage.Stream.CopyAsync().ConfigureAwait(false);
        await transport.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
    }
}