using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper;

public interface IDispatchTransportMessageObserver : IPipelineObserver<DispatchTransportMessage>;

public class DispatchTransportMessageObserver(IBusConfiguration busConfiguration, ITransportService transportService)
    : IDispatchTransportMessageObserver
{
    private readonly IBusConfiguration _busConfiguration = Guard.AgainstNull(busConfiguration);
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    public async Task ExecuteAsync(IPipelineContext<DispatchTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        Guard.AgainstEmpty(transportMessage.RecipientInboxWorkTransportUri);

        var transport = !_busConfiguration.HasOutbox()
            ? await _transportService.GetAsync(transportMessage.RecipientInboxWorkTransportUri, cancellationToken)
            : Guard.AgainstNull(_busConfiguration.Outbox!.WorkTransport);

        await using var stream = await Guard.AgainstNull(state.GetTransportMessageStream()).CopyAsync().ConfigureAwait(false);

        await transport.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
    }
}