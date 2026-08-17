using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Streams;

namespace Shuttle.Hopper;

public interface ISendOutboxMessageObserver : IPipelineObserver<DispatchTransportMessage>;

public class SendOutboxMessageObserver(ITransportService transportService) : ISendOutboxMessageObserver
{
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    public async Task ExecuteAsync(IPipelineContext<DispatchTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());

        Guard.AgainstEmpty(transportMessage.RecipientInboxWorkTransportUri);

        var transport = await _transportService.GetAsync(transportMessage.RecipientInboxWorkTransportUri, cancellationToken);

        await using var stream = await receivedMessage.Stream.CopyAsync(cancellationToken).ConfigureAwait(false);
        await transport.SendAsync(stream, pipelineContext.Pipeline, cancellationToken).ConfigureAwait(false);
    }
}